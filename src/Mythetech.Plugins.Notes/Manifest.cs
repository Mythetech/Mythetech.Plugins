using MudBlazor;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Plugins.Notes;

public class Manifest : IPluginManifest
{
    public string Id { get; } = ManifestConstants.Id;
    public string Name { get; } = "Notes";
    public Version Version { get; } = new Version(1, 0, 0);
    public string Developer { get; } = "Mythetech";
    public string Description { get; } = "Adds an in-application markdown-friendly note editor";
    public string? Icon { get; } = Icons.Material.Outlined.NoteAlt;
    public PluginAsset[] Assets =>
    [
        PluginAsset.Css("/_content/PSC.Blazor.Components.MarkdownEditor/css/easymde.min.css"),
        PluginAsset.Css("/_content/PSC.Blazor.Components.MarkdownEditor/css/markdowneditor.css"),
    ];
}

public static class ManifestConstants
{
    public static string Id => "Mythetech.Plugins.Notes";
}