namespace RAMpocalypse.Server.Game;

public readonly struct Position(double x, double y, double angle = 0)
{
    public double X { get; init; } = x;
    public double Y { get; init; } = y;
    public double Angle { get; init; } = angle;
    public static Position operator +(Position p1, Position p2) => new(p1.X + p2.X, p1.Y + p2.Y, p1.Angle + p2.Angle);
    public static Position operator -(Position p1, Position p2) => new(p1.X - p2.X, p1.Y - p2.Y, p1.Angle - p2.Angle);
    public static Position operator *(Position p1, double scalar) => new(p1.X * scalar, p1.Y * scalar, p1.Angle * scalar);
    public static Position operator /(Position p1, double scalar) => new(p1.X / scalar, p1.Y / scalar, p1.Angle / scalar);
    public static Position operator *(double scalar, Position p1) => p1 * scalar;
    public static Position operator /(double scalar, Position p1) => p1 / scalar;
    public static bool operator ==(Position left, Position right) =>
        left.X == right.X && left.Y == right.Y && left.Angle == right.Angle;
    public static bool operator !=(Position left, Position right) => !(left == right);

    public override readonly bool Equals(object? obj) => obj is Position other && this == other;
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Angle);
    public override readonly string ToString() => $"Position(X: {X}, Y: {Y}, Angle: {Angle})";
    public static (Position corner1, Position corner2) GetRandomOppositeCorners(int gameWidth, int gameHeight, int xOffset, int yOffset)
    {
        var random = new Random();
        var cornerIndex = random.Next(0, 4); // 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right
        int oppositeIndex = 3 - cornerIndex;

        Position[] corners = [
            new (xOffset, yOffset),                         // top-left
            new (gameWidth - xOffset, yOffset),             // top-right
            new (xOffset, gameHeight - yOffset),            // bottom-left
            new (gameWidth - xOffset, gameHeight - yOffset) // bottom-right
        ];

        return (corners[cornerIndex], corners[oppositeIndex]);
    }

    public static double CalculateDistance(Position p1, Position p2)
    {
        return Math.Sqrt(CalculateDistanceSquared(p1, p2));
    }
    public static double CalculateDistanceSquared(Position p1, Position p2)
    {
        var diff = p2 - p1;
        return diff.X * diff.X + diff.Y * diff.Y;
    }
    public static double CalculatePointToLineDistanceSquared(Position origin, Position line1, Position line2)
    {
        var lineDiff = line2 - line1;
        var originLineDiff = origin - line1;
        if (lineDiff.X == 0 && lineDiff.Y == 0)
        {
            return originLineDiff.X * originLineDiff.X + originLineDiff.Y * originLineDiff.Y;
        }
        var lineDistanceSquared = lineDiff.X * lineDiff.X + lineDiff.Y * lineDiff.Y;
        var dotProduct = originLineDiff.X * lineDiff.X + originLineDiff.Y * lineDiff.Y;
        var t = dotProduct / lineDistanceSquared;
        t = Math.Clamp(t, 0, 1);
        var a = originLineDiff.X - t * lineDiff.X;
        var b = originLineDiff.Y - t * lineDiff.Y;
        var f = a * a + b * b;
        return f;
    }
    public static Position CalculateClosestPointOnLine(Position origin, Position line1, Position line2)
    {
        var lineDiff = line2 - line1;
        if (lineDiff.X == 0 && lineDiff.Y == 0) return line1; // Should never happen but just in case
        var originLineDiff = origin - line1;
        var lineDistanceSquared = lineDiff.X * lineDiff.X + lineDiff.Y * lineDiff.Y;
        var t = (originLineDiff.X * lineDiff.X + originLineDiff.Y * lineDiff.Y) / lineDistanceSquared;
        t = Math.Clamp(t, 0, 1);
        return new Position(line1.X + t * lineDiff.X, line1.Y + t * lineDiff.Y);
    }
    public static bool IsInsideHalfCircle(Position origin, Position target, double radius, double angle)
    {
        var d = target - origin;
        if (d.X * d.X + d.Y * d.Y > radius * radius) return false;
        double fx = Math.Sin(angle);
        double fy = -Math.Cos(angle);
        return fx * d.X + fy * d.Y > 0;
    }
}
