using MudBlazor;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Plugins.Agent;

public class Manifest : IPluginManifest
{
    public string Id { get; } = ManifestConstants.Id;
    public string Name { get; } = "Claude Agent";
    public Version Version { get; } = new Version(1, 0, 0);
    public string Developer { get; } = "Mythetech";
    public string Description { get; } = "Chat interface wrapping the Claude CLI for AI-assisted conversations";
    public string? Icon { get; } = Icons.Material.Outlined.SmartToy;
    public PluginAsset[] Assets => [];
    public bool IsDevPlugin { get; } = true;
}
