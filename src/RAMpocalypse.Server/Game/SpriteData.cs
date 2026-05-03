namespace RAMpocalypse.Server.Game;

public enum SpriteType
{
    Player,
    Weapon,
}

public class SpriteData(string url, int width = 64, int height = 32, double scaleFactor = 8.0, SpriteType type = SpriteType.Player)
{
    public string URL { get; set; } = url;
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public double ScaleFactor { get; init; } = scaleFactor;
    public SpriteType Type { get; init; } = type;
}
