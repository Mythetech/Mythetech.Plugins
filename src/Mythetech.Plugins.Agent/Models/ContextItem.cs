namespace Mythetech.Plugins.Agent.Models;

public enum ContextItemType
{
    Text,
    File,
    CodeSnippet,
    Image,
    Custom
}

public class ContextItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ContextItemType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
