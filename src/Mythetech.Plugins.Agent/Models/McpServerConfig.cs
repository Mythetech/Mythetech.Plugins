namespace Mythetech.Plugins.Agent.Models;

/// <summary>
/// Configuration for an MCP server that can be passed to Claude CLI.
/// </summary>
public class McpServerConfig
{
    /// <summary>
    /// Unique name for this MCP server.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Transport type (stdio or http).
    /// </summary>
    public McpTransportType Type { get; set; } = McpTransportType.Stdio;

    /// <summary>
    /// Command to execute for stdio transport.
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Arguments for the command (stdio transport).
    /// </summary>
    public List<string> Args { get; set; } = [];

    /// <summary>
    /// Environment variables for the process (stdio transport).
    /// </summary>
    public Dictionary<string, string> Env { get; set; } = [];

    /// <summary>
    /// URL for http transport.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Whether this server is enabled and should be passed to Claude CLI.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// True if this server represents the host application's MCP server.
    /// Host app servers cannot be removed, only disabled.
    /// </summary>
    public bool IsHostApp { get; set; }
}

/// <summary>
/// MCP transport types supported by Claude CLI.
/// </summary>
public enum McpTransportType
{
    /// <summary>
    /// Standard input/output transport (local process).
    /// </summary>
    Stdio,

    /// <summary>
    /// HTTP transport (remote server).
    /// </summary>
    Http
}
