using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Plugins.Agent.Models;

namespace Mythetech.Plugins.Agent.Services;

/// <summary>
/// Service for managing MCP server configurations.
/// Uses Claude CLI's `claude mcp add/remove` commands to persist servers.
/// </summary>
public class McpConfigService : IMcpConfigService
{
    private const string StorageKey = "mcp-servers";

    private readonly IPluginStorage? _storage;
    private readonly McpServerState? _mcpServerState;
    private readonly ClaudeMcpManager? _mcpManager;
    private readonly ILogger<McpConfigService> _logger;
    private readonly List<McpServerConfig> _servers = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public McpServerConfig? HostAppServer { get; private set; }

    public event EventHandler? ConfigurationChanged;

    public McpConfigService(
        IPluginStorage? storage,
        McpServerState? mcpServerState,
        ClaudeMcpManager? mcpManager,
        ILogger<McpConfigService> logger)
    {
        _storage = storage;
        _mcpServerState = mcpServerState;
        _mcpManager = mcpManager;
        _logger = logger;

        // Subscribe to host app MCP state changes
        if (_mcpServerState != null)
        {
            _mcpServerState.StateChanged += OnHostAppMcpStateChanged;
        }
    }

    // Convenience constructor for backward compatibility
    public McpConfigService(
        IPluginStorage? storage,
        McpServerState? mcpServerState,
        ILogger<McpConfigService> logger)
        : this(storage, mcpServerState, null, logger)
    {
    }

    public IReadOnlyList<McpServerConfig> GetAllServers()
    {
        var result = new List<McpServerConfig>();

        if (HostAppServer != null)
        {
            result.Add(HostAppServer);
        }

        result.AddRange(_servers);
        return result;
    }

    public async Task LoadAsync()
    {
        // Detect host app MCP server
        DetectHostAppServer();

        // Load persisted servers
        if (_storage != null)
        {
            try
            {
                var saved = await _storage.GetAsync<List<McpServerConfig>>(StorageKey);
                if (saved != null)
                {
                    _servers.Clear();
                    _servers.AddRange(saved.Where(s => !s.IsHostApp));
                    _logger.LogDebug("Loaded {Count} MCP server configurations from storage", _servers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load MCP server configurations from storage");
            }
        }

        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsync()
    {
        if (_storage == null)
        {
            _logger.LogDebug("No storage available, skipping save");
            return;
        }

        try
        {
            // Only persist user-added servers (not host app)
            var toSave = _servers.Where(s => !s.IsHostApp).ToList();
            await _storage.SetAsync(StorageKey, toSave);
            _logger.LogDebug("Saved {Count} MCP server configurations to storage", toSave.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save MCP server configurations to storage");
        }
    }

    public void AddServer(McpServerConfig server)
    {
        // Use async version internally
        AddServerAsync(server).GetAwaiter().GetResult();
    }

    public async Task AddServerAsync(McpServerConfig server)
    {
        if (string.IsNullOrWhiteSpace(server.Name))
        {
            throw new ArgumentException("Server name is required", nameof(server));
        }

        if (GetAllServers().Any(s => s.Name.Equals(server.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Server with name '{server.Name}' already exists");
        }

        server.IsHostApp = false; // Ensure user-added servers are not marked as host app

        // Register with Claude CLI
        if (_mcpManager != null)
        {
            var success = await _mcpManager.AddServerAsync(server);
            if (!success)
            {
                _logger.LogWarning("Failed to register MCP server '{Name}' with Claude CLI", server.Name);
                // Continue anyway - we'll still track it locally
            }
            else
            {
                _logger.LogInformation("Registered MCP server '{Name}' with Claude CLI", server.Name);
            }
        }

        _servers.Add(server);
        _logger.LogInformation("Added MCP server: {Name}", server.Name);
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool RemoveServer(string name)
    {
        // Use async version internally
        return RemoveServerAsync(name).GetAwaiter().GetResult();
    }

    public async Task<bool> RemoveServerAsync(string name)
    {
        var server = _servers.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (server == null)
        {
            return false;
        }

        if (server.IsHostApp)
        {
            _logger.LogWarning("Cannot remove host app MCP server");
            return false;
        }

        // Unregister from Claude CLI
        if (_mcpManager != null)
        {
            var success = await _mcpManager.RemoveServerAsync(name);
            if (!success)
            {
                _logger.LogWarning("Failed to unregister MCP server '{Name}' from Claude CLI", name);
                // Continue anyway - remove from local tracking
            }
            else
            {
                _logger.LogInformation("Unregistered MCP server '{Name}' from Claude CLI", name);
            }
        }

        _servers.Remove(server);
        _logger.LogInformation("Removed MCP server: {Name}", name);
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void SetServerEnabled(string name, bool enabled)
    {
        // Check host app server first
        if (HostAppServer?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
        {
            HostAppServer.Enabled = enabled;
            _logger.LogInformation("Set host app MCP server enabled: {Enabled}", enabled);
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var server = _servers.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (server != null)
        {
            server.Enabled = enabled;
            _logger.LogInformation("Set MCP server '{Name}' enabled: {Enabled}", name, enabled);
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string? BuildCliConfigJson()
    {
        var enabledServers = GetAllServers().Where(s => s.Enabled).ToList();
        if (enabledServers.Count == 0)
        {
            return null;
        }

        var mcpConfig = new Dictionary<string, object>
        {
            ["mcpServers"] = enabledServers.ToDictionary(
                s => s.Name,
                s => BuildServerConfig(s))
        };

        return JsonSerializer.Serialize(mcpConfig, JsonOptions);
    }

    private static object BuildServerConfig(McpServerConfig server)
    {
        if (server.Type == McpTransportType.Http)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "http",
                ["url"] = server.Url
            };
        }

        var config = new Dictionary<string, object?>
        {
            ["type"] = "stdio",
            ["command"] = server.Command
        };

        if (server.Args.Count > 0)
        {
            config["args"] = server.Args;
        }

        if (server.Env.Count > 0)
        {
            config["env"] = server.Env;
        }

        return config;
    }

    private void DetectHostAppServer()
    {
        if (_mcpServerState == null)
        {
            _logger.LogDebug("No McpServerState available, host app MCP detection skipped");
            return;
        }

        // Check if host app has HTTP MCP endpoint available
        var httpEndpoint = _mcpServerState.HttpEndpoint;
        if (!string.IsNullOrEmpty(httpEndpoint))
        {
            // HTTP transport available - create host app server config
            HostAppServer = new McpServerConfig
            {
                Name = "host-app",
                Type = McpTransportType.Http,
                Url = httpEndpoint,
                IsHostApp = true,
                Enabled = _mcpServerState.IsRunning
            };

            _logger.LogInformation("Detected host app HTTP MCP server: {Endpoint}", httpEndpoint);

            // Auto-register with Claude CLI if running
            if (_mcpServerState.IsRunning)
            {
                _ = RegisterHostAppWithClaudeAsync();
            }
        }
        else
        {
            _logger.LogDebug(
                "Host app has MCP capability (IsRunning={IsRunning}, Tools={ToolCount}), " +
                "but no HTTP endpoint available. Stdio transport not supported for host app.",
                _mcpServerState.IsRunning,
                _mcpServerState.RegisteredTools.Count);
        }
    }

    private async Task RegisterHostAppWithClaudeAsync()
    {
        if (HostAppServer == null || _mcpManager == null)
            return;

        try
        {
            // Check if already registered
            var exists = await _mcpManager.ServerExistsAsync(HostAppServer.Name);
            if (exists)
            {
                _logger.LogDebug("Host app MCP server already registered with Claude CLI");
                return;
            }

            var success = await _mcpManager.AddServerAsync(HostAppServer);
            if (success)
            {
                _logger.LogInformation("Registered host app MCP server with Claude CLI: {Endpoint}", HostAppServer.Url);
            }
            else
            {
                _logger.LogWarning("Failed to register host app MCP server with Claude CLI");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error registering host app MCP server with Claude CLI");
        }
    }

    private async void OnHostAppMcpStateChanged(object? sender, EventArgs e)
    {
        if (_mcpServerState == null)
            return;

        var httpEndpoint = _mcpServerState.HttpEndpoint;

        // If HTTP endpoint just became available, create the host app server
        if (!string.IsNullOrEmpty(httpEndpoint) && HostAppServer == null)
        {
            HostAppServer = new McpServerConfig
            {
                Name = "host-app",
                Type = McpTransportType.Http,
                Url = httpEndpoint,
                IsHostApp = true,
                Enabled = true
            };

            _logger.LogInformation("Host app HTTP MCP server now available: {Endpoint}", httpEndpoint);
            await RegisterHostAppWithClaudeAsync();
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (HostAppServer != null)
        {
            // Update enabled state and URL if changed
            HostAppServer.Enabled = _mcpServerState.IsRunning;
            if (!string.IsNullOrEmpty(httpEndpoint))
            {
                HostAppServer.Url = httpEndpoint;
            }
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
