namespace RAMpocalypse.Server.Game;

public readonly struct Position(double x, double y)
{
    public double X { get; init; } = x;
    public double Y { get; init; } = y;

    public static bool operator ==(Position left, Position right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(Position left, Position right) => !(left == right);

    public override readonly bool Equals(object? obj) => obj is Position other && this == other;
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

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

    public static double CalculateDistance(Position p1, Position p2)
    {
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
