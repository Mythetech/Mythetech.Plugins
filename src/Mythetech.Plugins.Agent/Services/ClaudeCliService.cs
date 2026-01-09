using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Mythetech.Plugins.Agent.Models;

namespace Mythetech.Plugins.Agent.Services;

public class ClaudeCliService : IClaudeCliService, IDisposable
{
    private readonly IClaudeCliDetector _detector;
    private readonly IMcpConfigService? _mcpConfig;
    private readonly ILogger<ClaudeCliService> _logger;
    private Process? _currentProcess;
    private CancellationTokenSource? _cts;

    public bool IsProcessing => _currentProcess != null && !_currentProcess.HasExited;
    public event EventHandler<bool>? ProcessingStateChanged;

    public ClaudeCliService(
        IClaudeCliDetector detector,
        IMcpConfigService? mcpConfig,
        ILogger<ClaudeCliService> logger)
    {
        _detector = detector;
        _mcpConfig = mcpConfig;
        _logger = logger;
    }

    public ClaudeCliService(IClaudeCliDetector detector, ILogger<ClaudeCliService> logger)
        : this(detector, null, logger)
    {
    }

    public async IAsyncEnumerable<string> SendMessageAsync(
        string message,
        AgentContext? context = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cliPath = await _detector.GetCliPathAsync();
        if (string.IsNullOrEmpty(cliPath))
        {
            throw new InvalidOperationException("Claude CLI is not installed");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Build command arguments as a list (avoids shell parsing issues with JSON)
        var argsList = BuildArgumentsList(message, context);

        var psi = new ProcessStartInfo(cliPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Run sandboxed - no workspace access
            WorkingDirectory = Path.GetTempPath()
        };

        foreach (var arg in argsList)
        {
            psi.ArgumentList.Add(arg);
        }

        _logger.LogDebug("Starting Claude CLI: {Path} with {ArgCount} args", cliPath, argsList.Count);

        try
        {
            _currentProcess = Process.Start(psi);
            if (_currentProcess == null)
            {
                throw new InvalidOperationException("Failed to start Claude CLI process");
            }

            ProcessingStateChanged?.Invoke(this, true);

            // Close stdin immediately since we're using --print mode
            _currentProcess.StandardInput.Close();

            // Stream stdout character by character for real-time updates
            using var reader = _currentProcess.StandardOutput;
            var buffer = new char[1];
            int charsRead;

            while ((charsRead = await reader.ReadAsync(buffer, _cts.Token)) > 0
                   && !_cts.Token.IsCancellationRequested)
            {
                yield return new string(buffer, 0, charsRead);
            }

            await _currentProcess.WaitForExitAsync(_cts.Token);

            if (_currentProcess.ExitCode != 0)
            {
                var error = await _currentProcess.StandardError.ReadToEndAsync(_cts.Token);
                _logger.LogWarning("Claude CLI exited with code {Code}: {Error}",
                    _currentProcess.ExitCode, error);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    yield return $"\n\n**Error:** {error}";
                }
            }
        }
        finally
        {
            CleanupProcess();
            CleanupMcpConfigFile();
        }
    }

    private string? _mcpConfigFilePath;

    private List<string> BuildArgumentsList(string message, AgentContext? context)
    {
        var args = new List<string>();

        // Use print mode for simple streaming output (non-interactive)
        args.Add("--print");

        // Add system prompt from context if provided
        if (!string.IsNullOrEmpty(context?.SystemPrompt))
        {
            args.Add("--system-prompt");
            args.Add(context.SystemPrompt);
        }

        // The prompt/message must come BEFORE variadic options like --allowedTools and --mcp-config
        args.Add(message);

        // Add allowed tools if configured (enables specific tools without prompting)
        // Must come AFTER the message (variadic option)
        if (context?.AllowedTools?.Count > 0)
        {
            args.Add("--allowedTools");
            args.Add(string.Join(",", context.AllowedTools));
        }

        // Add MCP configuration if servers are configured
        // Write to temp file because inline JSON parsing is unreliable
        // Must come AFTER the message (variadic option)
        var mcpJson = _mcpConfig?.BuildCliConfigJson();
        if (!string.IsNullOrEmpty(mcpJson))
        {
            _mcpConfigFilePath = Path.Combine(Path.GetTempPath(), $"claude-mcp-{Guid.NewGuid():N}.json");
            File.WriteAllText(_mcpConfigFilePath, mcpJson);
            args.Add("--mcp-config");
            args.Add(_mcpConfigFilePath);
            _logger.LogDebug("Wrote MCP config to temp file: {Path}", _mcpConfigFilePath);
        }

        return args;
    }

    private void CleanupMcpConfigFile()
    {
        if (_mcpConfigFilePath != null && File.Exists(_mcpConfigFilePath))
        {
            try
            {
                File.Delete(_mcpConfigFilePath);
                _logger.LogDebug("Deleted MCP config temp file: {Path}", _mcpConfigFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete MCP config temp file: {Path}", _mcpConfigFilePath);
            }
            _mcpConfigFilePath = null;
        }
    }

    public void CancelCurrentRequest()
    {
        _logger.LogDebug("Cancelling current request");
        _cts?.Cancel();

        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            try
            {
                _currentProcess.Kill(entireProcessTree: true);
                _logger.LogDebug("Killed Claude CLI process");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill Claude CLI process");
            }
        }
    }

    private void CleanupProcess()
    {
        _currentProcess?.Dispose();
        _currentProcess = null;
        _cts?.Dispose();
        _cts = null;
        ProcessingStateChanged?.Invoke(this, false);
    }

    public void Dispose()
    {
        CancelCurrentRequest();
        CleanupMcpConfigFile();
        _cts?.Dispose();
        _currentProcess?.Dispose();
    }
}
