using System.Reflection;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Plugins.Games;

public class Manifest : IPluginManifest
{
    public string Id { get; } = ManifestConstants.Id;
    public string Name { get; } = "Games";
    public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;
    public string Developer { get; } = "Mythetech";
    public string Description { get; } = "A collection of fun, simple single-player games";
    public string? Icon { get; } = Icons.Material.Outlined.Games;
    public PluginAsset[] Assets => [];
}

public static class ManifestConstants
{
    public static string Id => "Mythetech.Plugins.Games";
}

