namespace Mythetech.Plugins.Games;

public class GameInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Type ComponentType { get; set; } = null!;
}

