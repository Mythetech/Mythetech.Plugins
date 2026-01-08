using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Mythetech.Plugins.Agent.Models;

namespace Mythetech.Plugins.Agent.Services;

public class ClaudeCliService : IClaudeCliService, IDisposable
{
    private readonly IClaudeCliDetector _detector;
    private readonly ILogger<ClaudeCliService> _logger;
    private Process? _currentProcess;
    private CancellationTokenSource? _cts;

    public bool IsProcessing => _currentProcess != null && !_currentProcess.HasExited;
    public event EventHandler<bool>? ProcessingStateChanged;

    public ClaudeCliService(IClaudeCliDetector detector, ILogger<ClaudeCliService> logger)
    {
        _detector = detector;
        _logger = logger;
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

        // Build command arguments
        var args = BuildArguments(message, context);

        var psi = new ProcessStartInfo(cliPath, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Run sandboxed - no workspace access
            WorkingDirectory = Path.GetTempPath()
        };

        _logger.LogDebug("Starting Claude CLI: {Path} {Args}", cliPath, args);

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
        }
    }

    private string BuildArguments(string message, AgentContext? context)
    {
        var args = new List<string>();

        // Use print mode for simple streaming output (non-interactive)
        args.Add("--print");

        // Add system prompt from context if provided
        if (!string.IsNullOrEmpty(context?.SystemPrompt))
        {
            args.Add("--system-prompt");
            args.Add(EscapeArgument(context.SystemPrompt));
        }

        // Add the message itself
        args.Add(EscapeArgument(message));

        return string.Join(" ", args);
    }

    private static string EscapeArgument(string arg)
    {
        // Wrap in quotes and escape internal quotes
        var escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
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
        _cts?.Dispose();
        _currentProcess?.Dispose();
    }
}
