using System.Reflection;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Plugins.Notes;

public class Manifest : IPluginManifest
{
    public string Id { get; } = ManifestConstants.Id;
    public string Name { get; } = "Notes";
    public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;
    public string Developer { get; } = "Mythetech";
    public string Description { get; } = "Adds an in-application markdown-friendly note editor";
    public PluginAsset[] Assets =>
    [
        PluginAsset.Css("/_content/PSC.Blazor.Components.MarkdownEditor/css/easymde.min.css"),
        PluginAsset.Css("/_content/PSC.Blazor.Components.MarkdownEditor/css/markdowneditor.css"),
        PluginAsset.Js("/_content/PSC.Blazor.Components.MarkdownEditor/js/easymde.min.js"),
        PluginAsset.Js("/_content/PSC.Blazor.Components.MarkdownEditor/js/markdownEditor.js"),
        //PluginAsset.Js("/_content/PSC.Blazor.Components.MarkdownEditor/js/mermaid.min.js"),
        PluginAsset.Js("/_content/PSC.Blazor.Components.MarkdownEditor/js/textarearesize.min.js"),
    ];
}

public static class ManifestConstants
{
    public static string Id => "Mythetech.Plugins.Notes";
}