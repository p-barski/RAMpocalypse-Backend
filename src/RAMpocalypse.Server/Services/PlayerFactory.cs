using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class PlayerFactory : IPlayerFactory
{
    private static string GeneratePlayerId()
    {
        return $"player_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }

    public Player CreatePlayer(string connectionId)
    {
        // Assign random sprite variant (1-4 for now, can be expanded)
        var random = new Random();
        var spriteVariant = random.Next(1, 5);
        var spriteData = new SpriteData($"http://localhost:5027/assets/sprites/player_{spriteVariant}.png");
        var player = new Player(GeneratePlayerId(), spriteData);
        return player;
    }
}
