namespace RAMpocalypse.Server.Game;

public struct Position(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public static (Position corner1, Position corner2) GetRandomOppositeCorners(int gameWidth, int gameHeight)
    {
        var random = new Random();
        var cornerIndex = random.Next(0, 4); // 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right
        int oppositeIndex = 3 - cornerIndex;

        var corners = new[]
        {
            new Position(0f, 0f),                             // top-left
            new Position(gameWidth - 100f, 0f),               // top-right
            new Position(0f, gameHeight - 100f),              // bottom-left
            new Position(gameWidth - 100f, gameHeight - 100f) // bottom-right
        };

        return (corners[cornerIndex], corners[oppositeIndex]);
    }
}