using Mythetech.Plugins.Agent.Models;

namespace Mythetech.Plugins.Agent.Services;

public interface IClaudeCliService
{
    /// <summary>
    /// Send a message and receive streaming response
    /// </summary>
    IAsyncEnumerable<string> SendMessageAsync(
        string message,
        AgentContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a request is currently in progress
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Cancel the current request
    /// </summary>
    void CancelCurrentRequest();

    /// <summary>
    /// Event fired when processing state changes
    /// </summary>
    event EventHandler<bool>? ProcessingStateChanged;
}
