namespace RAMpocalypse.Server.Game;

public struct Position(double x, double y)
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public static (Position corner1, Position corner2) GetRandomOppositeCorners(int gameWidth, int gameHeight)
    {
        var random = new Random();
        var cornerIndex = random.Next(0, 4); // 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right
        int oppositeIndex = 3 - cornerIndex;

        var corners = new[]
        {
            new Position(0.0, 0.0),                             // top-left
            new Position(gameWidth - 100.0, 0.0),               // top-right
            new Position(0.0, gameHeight - 100.0),              // bottom-left
            new Position(gameWidth - 100.0, gameHeight - 100.0) // bottom-right
        };

        return (corners[cornerIndex], corners[oppositeIndex]);
    }
}