namespace RAMpocalypse.Server.Game;

public static class MovementValidator
{
    // Maximum movement speed in pixels per second
    private const double MAX_MOVEMENT_SPEED = 500f; // 5 pixels per frame * 60 fps = 300, but allow some buffer

    // Maximum distance that can be traveled in a single update (with buffer for network lag)
    // Assuming updates every ~20ms, max distance = speed * time
    private const double MAX_DISTANCE_PER_UPDATE = MAX_MOVEMENT_SPEED * 0.05f; // 50ms buffer

    public static ValidationResult ValidateMovement(Player player, Position newPosition, int gameWidth, int gameHeight, double timeSinceLastUpdate)
    {
        var distance = CalculateDistance(player.Position, newPosition);
        var maxAllowedDistance = MAX_DISTANCE_PER_UPDATE + (MAX_MOVEMENT_SPEED * timeSinceLastUpdate);

        // Check if movement is too far (teleportation detection)
        if (distance > maxAllowedDistance)
        {
            return new ValidationResult
            {
                IsValid = false,
                CorrectedPosition = player.Position,
                Reason = $"Movement too far: {distance:F2} > {maxAllowedDistance:F2}"
            };
        }

        // Validate boundaries
        var correctedX = Math.Max(0, Math.Min(newPosition.X, gameWidth - player.SpriteData.Width));
        var correctedY = Math.Max(0, Math.Min(newPosition.Y, gameHeight - player.SpriteData.Height));

        if (Math.Abs(correctedX - newPosition.X) > 0.1f || Math.Abs(correctedY - newPosition.Y) > 0.1f)
        {
            return new ValidationResult
            {
                IsValid = false,
                CorrectedPosition = new Position(correctedX, correctedY),
                Reason = "Position outside boundaries"
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            CorrectedPosition = newPosition,
            Reason = "Valid"
        };
    }

    private static double CalculateDistance(Position pos1, Position pos2)
    {
        var dx = pos2.X - pos1.X;
        var dy = pos2.Y - pos1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public Position CorrectedPosition { get; set; }
    public string Reason { get; set; } = string.Empty;
}
