using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mythetech.Plugins.Agent.Models;

namespace Mythetech.Plugins.Agent.Services;

/// <summary>
/// Manages MCP servers via Claude CLI's `claude mcp` commands.
/// Servers are persisted in Claude's own configuration.
/// </summary>
public class ClaudeMcpManager
{
    private readonly IClaudeCliDetector _detector;
    private readonly ILogger<ClaudeMcpManager> _logger;

    public ClaudeMcpManager(IClaudeCliDetector detector, ILogger<ClaudeMcpManager> logger)
    {
        _detector = detector;
        _logger = logger;
    }

    /// <summary>
    /// Add an MCP server to Claude's configuration.
    /// </summary>
    public async Task<bool> AddServerAsync(McpServerConfig server, CancellationToken cancellationToken = default)
    {
        var cliPath = await _detector.GetCliPathAsync();
        if (string.IsNullOrEmpty(cliPath))
        {
            _logger.LogWarning("Claude CLI not found, cannot add MCP server");
            return false;
        }

        var args = new List<string> { "mcp", "add" };

        // Use user scope so it persists across projects
        args.Add("--scope");
        args.Add("user");

        // Transport type
        args.Add("--transport");
        args.Add(server.Type == McpTransportType.Stdio ? "stdio" : "http");

        // Environment variables for stdio
        if (server.Type == McpTransportType.Stdio && server.Env.Count > 0)
        {
            foreach (var (key, value) in server.Env)
            {
                args.Add("--env");
                args.Add($"{key}={value}");
            }
        }

        // Server name
        args.Add(server.Name);

        // Command/URL and args
        if (server.Type == McpTransportType.Stdio)
        {
            // Add -- separator before command and args
            args.Add("--");
            if (!string.IsNullOrEmpty(server.Command))
            {
                args.Add(server.Command);
            }
            args.AddRange(server.Args);
        }
        else if (server.Type == McpTransportType.Http)
        {
            if (!string.IsNullOrEmpty(server.Url))
            {
                args.Add(server.Url);
            }
        }

        return await RunClaudeCommandAsync(cliPath, args, cancellationToken);
    }

    /// <summary>
    /// Remove an MCP server from Claude's configuration.
    /// </summary>
    public async Task<bool> RemoveServerAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var cliPath = await _detector.GetCliPathAsync();
        if (string.IsNullOrEmpty(cliPath))
        {
            _logger.LogWarning("Claude CLI not found, cannot remove MCP server");
            return false;
        }

        var args = new List<string> { "mcp", "remove", "--scope", "user", serverName };
        return await RunClaudeCommandAsync(cliPath, args, cancellationToken);
    }

    /// <summary>
    /// List all configured MCP servers.
    /// </summary>
    public async Task<List<string>> ListServersAsync(CancellationToken cancellationToken = default)
    {
        var cliPath = await _detector.GetCliPathAsync();
        if (string.IsNullOrEmpty(cliPath))
        {
            return [];
        }

        var psi = new ProcessStartInfo(cliPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("mcp");
        psi.ArgumentList.Add("list");

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return [];

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Parse output - each line typically contains a server name
            var servers = new List<string>();
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("No MCP"))
                {
                    // Extract server name (first word before any description)
                    var name = trimmed.Split(' ', 2)[0];
                    if (!string.IsNullOrEmpty(name))
                    {
                        servers.Add(name);
                    }
                }
            }
            return servers;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list MCP servers");
            return [];
        }
    }

    /// <summary>
    /// Check if a server is already registered.
    /// </summary>
    public async Task<bool> ServerExistsAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var servers = await ListServersAsync(cancellationToken);
        return servers.Contains(serverName, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> RunClaudeCommandAsync(string cliPath, List<string> args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(cliPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        _logger.LogDebug("Running: claude {Args}", string.Join(" ", args));

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogWarning("Failed to start Claude CLI process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogWarning("Claude CLI command failed with exit code {Code}: {Error}",
                    process.ExitCode, error);
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            _logger.LogDebug("Claude CLI output: {Output}", output);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run Claude CLI command");
            return false;
        }
    }
}
