using System.Diagnostics;

namespace Mythetech.Plugins.Agent.Services;

public class ClaudeCliDetector : IClaudeCliDetector
{
    private string? _cachedPath;
    private bool? _cachedInstalled;

    public async Task<bool> IsInstalledAsync()
    {
        if (_cachedInstalled.HasValue) return _cachedInstalled.Value;

        var path = await GetCliPathAsync();
        _cachedInstalled = !string.IsNullOrEmpty(path);
        return _cachedInstalled.Value;
    }

    public async Task<string?> GetCliPathAsync()
    {
        if (_cachedPath != null) return _cachedPath;

        // Try common locations based on platform
        foreach (var path in GetCandidatePaths())
        {
            if (File.Exists(path))
            {
                _cachedPath = path;
                return path;
            }
        }

        // Try 'which' / 'where' command
        _cachedPath = await TryFindInPathAsync();
        return _cachedPath;
    }

    public async Task<string?> GetVersionAsync()
    {
        var cliPath = await GetCliPathAsync();
        if (string.IsNullOrEmpty(cliPath)) return null;

        try
        {
            var psi = new ProcessStartInfo(cliPath, "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<string> GetCandidatePaths()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            yield return Path.Combine(localAppData, "Programs", "claude", "claude.exe");
            yield return Path.Combine(programFiles, "claude", "claude.exe");
            // npm global install location
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "npm", "claude.cmd");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/usr/local/bin/claude";
            yield return "/opt/homebrew/bin/claude";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".local", "bin", "claude");
            // npm global install location
            yield return Path.Combine(home, ".npm-global", "bin", "claude");
        }
        else // Linux
        {
            yield return "/usr/bin/claude";
            yield return "/usr/local/bin/claude";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".local", "bin", "claude");
            // npm global install location
            yield return Path.Combine(home, ".npm-global", "bin", "claude");
        }
    }

    private async Task<string?> TryFindInPathAsync()
    {
        var command = OperatingSystem.IsWindows() ? "where" : "which";
        try
        {
            var psi = new ProcessStartInfo(command, "claude")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var firstLine = output.Trim().Split('\n')[0].Trim();
                return string.IsNullOrEmpty(firstLine) ? null : firstLine;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public string GetInstallationInstructions()
    {
        if (OperatingSystem.IsMacOS())
        {
            return """
                ## Install Claude CLI on macOS

                **Option 1: npm (recommended)**
                ```bash
                npm install -g @anthropic-ai/claude-code
                ```

                **Option 2: Homebrew**
                ```bash
                brew install claude
                ```

                **After installation:**
                ```bash
                claude auth
                ```

                This will open a browser to authenticate with your Anthropic account.
                """;
        }
        else if (OperatingSystem.IsWindows())
        {
            return """
                ## Install Claude CLI on Windows

                **Using npm:**
                ```powershell
                npm install -g @anthropic-ai/claude-code
                ```

                **After installation:**
                ```powershell
                claude auth
                ```

                This will open a browser to authenticate with your Anthropic account.
                """;
        }
        else
        {
            return """
                ## Install Claude CLI on Linux

                **Using npm:**
                ```bash
                npm install -g @anthropic-ai/claude-code
                ```

                **After installation:**
                ```bash
                claude auth
                ```

                This will open a browser to authenticate with your Anthropic account.
                """;
        }
    }

    /// <summary>
    /// Clear cached detection results to force re-detection
    /// </summary>
    public void ClearCache()
    {
        _cachedPath = null;
        _cachedInstalled = null;
    }
}
