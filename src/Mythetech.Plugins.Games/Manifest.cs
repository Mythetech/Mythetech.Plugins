using MudBlazor;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Plugins.Games;

public class Manifest : IPluginManifest
{
    public string Id { get; } = ManifestConstants.Id;
    public string Name { get; } = "Games";
    public Version Version { get; } = new Version(1, 0, 0);
    public string Developer { get; } = "Mythetech";
    public string Description { get; } = "A collection of fun, simple single-player games";
    public string? Icon { get; } = Icons.Material.Outlined.Games;
    public PluginAsset[] Assets => [
        PluginAsset.Js("/breakout.js"), 
    ];  
}

public static class ManifestConstants
{
    public static string Id => "Mythetech.Plugins.Games";
}

