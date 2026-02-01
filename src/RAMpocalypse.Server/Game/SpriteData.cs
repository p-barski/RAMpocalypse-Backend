namespace RAMpocalypse.Server.Game;

public class SpriteData(string url, int width = 64, int height = 32, int scaleFactor = 8)
{
    public string URL { get; init; } = url;
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public int ScaleFactor { get; init; } = scaleFactor;
}
