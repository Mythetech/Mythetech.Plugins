namespace Mythetech.Plugins.Agent.Models;

/// <summary>
/// Generic context model that consuming applications can populate.
/// Designed to be app-agnostic - works for code editors, document processors, etc.
/// </summary>
public class AgentContext
{
    /// <summary>
    /// Human-readable description of what this context represents
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Collection of context items (files, snippets, custom data)
    /// </summary>
    public List<ContextItem> Items { get; set; } = new();

    /// <summary>
    /// App-specific metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Optional system prompt to prepend to conversations
    /// </summary>
    public string? SystemPrompt { get; set; }
}
