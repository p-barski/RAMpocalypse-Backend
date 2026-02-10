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
        var weaponData = new SpriteData($"http://localhost:5027/assets/sprites/lightning_1.png", 17, 53, 6);
        var weapon = new SubEntity(new Position(spriteData.Width * spriteData.ScaleFactor - 10, 0), weaponData, $"weapon_{player.Id}");
        player.SubEntities.Add(weapon);
        return player;
    }
}
