namespace RAMpocalypse.Server.Game;

public static class MovementValidator
{
    // Maximum movement speed in pixels per second
    private const double MAX_MOVEMENT_SPEED = 500.0; // 5 pixels per frame * 60 fps = 300, but allow some buffer

    // Maximum distance that can be traveled in a single update (with buffer for network lag)
    // Assuming updates every ~20ms, max distance = speed * time
    private const double MAX_DISTANCE_PER_UPDATE = MAX_MOVEMENT_SPEED * 0.05; // 50ms buffer

    public static ValidationResult ValidateMovement(Player player, Position newPosition, int gameWidth, int gameHeight, double timeSinceLastUpdate)
    {
        var result = new ValidationResult { CorrectedPosition = player.Position };
        var distance = Position.CalculateDistance(player.Position, newPosition);
        var maxAllowedDistance = MAX_DISTANCE_PER_UPDATE + (MAX_MOVEMENT_SPEED * timeSinceLastUpdate);

        // Check if movement is too far (teleportation detection)
        if (distance > maxAllowedDistance)
        {
            result.Reason = $"Movement too far: {distance:F2} > {maxAllowedDistance:F2}";
            return result;
        }

        // Validate boundaries
        var correctedX = Math.Max(0, Math.Min(newPosition.X, gameWidth - player.SpriteData.Width));
        var correctedY = Math.Max(0, Math.Min(newPosition.Y, gameHeight - player.SpriteData.Height));

        if (Math.Abs(correctedX - newPosition.X) > 0.1 || Math.Abs(correctedY - newPosition.Y) > 0.1)
        {
            result.CorrectedPosition = new Position(correctedX, correctedY);
            result.Reason = "Position outside boundaries";
            return result;
        }

        result.IsValid = true;
        result.CorrectedPosition = newPosition;
        result.Reason = "Valid";
        return result;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public Position CorrectedPosition { get; set; }
    public string Reason { get; set; } = string.Empty;
}
