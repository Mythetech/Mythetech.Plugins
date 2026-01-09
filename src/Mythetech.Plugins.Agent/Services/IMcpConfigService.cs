using Mythetech.Plugins.Agent.Models;

namespace Mythetech.Plugins.Agent.Services;

/// <summary>
/// Service for managing MCP server configurations.
/// Handles persistence, host app detection, and CLI config generation.
/// </summary>
public interface IMcpConfigService
{
    /// <summary>
    /// Get all configured MCP servers (including host app server if detected).
    /// </summary>
    IReadOnlyList<McpServerConfig> GetAllServers();

    /// <summary>
    /// Load server configurations from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Save server configurations to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Add a new MCP server configuration.
    /// </summary>
    void AddServer(McpServerConfig server);

    /// <summary>
    /// Add a new MCP server configuration asynchronously.
    /// </summary>
    Task AddServerAsync(McpServerConfig server);

    /// <summary>
    /// Remove an MCP server by name.
    /// Host app servers cannot be removed.
    /// </summary>
    bool RemoveServer(string name);

    /// <summary>
    /// Remove an MCP server by name asynchronously.
    /// Host app servers cannot be removed.
    /// </summary>
    Task<bool> RemoveServerAsync(string name);

    /// <summary>
    /// Enable or disable an MCP server.
    /// </summary>
    void SetServerEnabled(string name, bool enabled);

    /// <summary>
    /// Build the JSON configuration string for Claude CLI --mcp-config flag.
    /// Returns null if no enabled servers are configured.
    /// </summary>
    string? BuildCliConfigJson();

    /// <summary>
    /// The host application's MCP server, if detected.
    /// </summary>
    McpServerConfig? HostAppServer { get; }

    /// <summary>
    /// Event raised when server configuration changes.
    /// </summary>
    event EventHandler? ConfigurationChanged;
}
