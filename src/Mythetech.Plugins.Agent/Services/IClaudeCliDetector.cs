namespace Mythetech.Plugins.Agent.Services;

public interface IClaudeCliDetector
{
    /// <summary>
    /// Check if Claude CLI is installed and accessible
    /// </summary>
    Task<bool> IsInstalledAsync();

    /// <summary>
    /// Get the path to the Claude CLI executable
    /// </summary>
    Task<string?> GetCliPathAsync();

    /// <summary>
    /// Get the installed version of Claude CLI
    /// </summary>
    Task<string?> GetVersionAsync();

    /// <summary>
    /// Get installation instructions for the current platform
    /// </summary>
    string GetInstallationInstructions();
}
