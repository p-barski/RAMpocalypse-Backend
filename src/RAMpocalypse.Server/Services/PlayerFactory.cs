using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class PlayerFactory(ILogger<PlayerFactory> logger, ISpriteInfo spriteInfo, TimeProvider timeProvider) : IPlayerFactory
{
    private readonly ILogger<PlayerFactory> logger = logger;
    private readonly ISpriteInfo spriteInfo = spriteInfo;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly Random random = new();
    public void RandomizePlayersSprites(List<Player> players, IReadOnlyCollection<Player>? existingPlayers = null)
    {
        var playerSprites = spriteInfo.GetAllOfType(SpriteType.Player);
        var weaponSprites = spriteInfo.GetAllOfType(SpriteType.Weapon);
        var usedPlayerVariants = new bool[playerSprites.Count];
        var usedWeaponVariants = new bool[weaponSprites.Count];

        foreach (var existingPlayer in existingPlayers ?? [])
        {
            MarkVariantAsUsed(playerSprites, usedPlayerVariants, existingPlayer.SpriteData.URL);
            var existingWeapon = existingPlayer.SubEntities.ElementAtOrDefault(0);
            if (existingWeapon is not null) //shouldn't be null, but just in case
            {
                MarkVariantAsUsed(weaponSprites, usedWeaponVariants, existingWeapon.SpriteData.URL);
            }
        }

        foreach (var player in players)
        {
            var playerVariant = PickRandomUnusedVariant(playerSprites.Count, usedPlayerVariants);
            var weaponVariant = PickRandomUnusedVariant(weaponSprites.Count, usedWeaponVariants);

            var playerSprite = playerSprites[playerVariant];
            player.SpriteData = playerSprite;
            var weapon = player.SubEntities.ElementAtOrDefault(0);
            var weaponSprite = weaponSprites[weaponVariant];
            var weaponOffset = CalculateWeaponOffset(playerSprite, weaponSprite);
            if (weapon is null) //shouldn't be null, but just in case
            {
                logger.LogWarning("Player ({PlayerID}) has no weapon sub entity", player.Id);
                weapon = new SubEntity(weaponOffset, weaponSprite, $"weapon_{player.Id}");
                player.SubEntities.Add(weapon);
            }
            else
            {
                weapon.SpriteData = weaponSprite;
                weapon.Position = weaponOffset;
            }
        }
    }

    private static void MarkVariantAsUsed(List<SpriteData> sprites, bool[] usedVariants, string url)
    {
        var index = sprites.FindIndex(s => s.URL == url);
        if (index >= 0) usedVariants[index] = true;
    }

    private int PickRandomUnusedVariant(int variantCount, bool[] usedVariants)
    {
        var variant = random.Next(0, variantCount);
        for (var attempt = 0; attempt < variantCount; attempt++)
        {
            if (!usedVariants[variant])
            {
                break;
            }
            variant = (variant + 1) % variantCount;
        }
        usedVariants[variant] = true;
        return variant;
    }

    public Player CreatePlayer()
    {
        var playerSprites = spriteInfo.GetAllOfType(SpriteType.Player);
        var weaponSprites = spriteInfo.GetAllOfType(SpriteType.Weapon);
        var playerVariant = random.Next(0, playerSprites.Count);
        var weaponVariant = random.Next(0, weaponSprites.Count);
        var playerSprite = playerSprites[playerVariant];
        var weaponSprite = weaponSprites[weaponVariant];
        var player = new Player(GeneratePlayerId(), playerSprite)
        {
            LastPositionUpdateTime = timeProvider.GetUtcNow().UtcDateTime,
        };
        var weaponOffset = CalculateWeaponOffset(playerSprite, weaponSprite);
        var weapon = new SubEntity(weaponOffset, weaponSprite, $"weapon_{player.Id}");
        player.SubEntities.Add(weapon);
        return player;
    }

    private string GeneratePlayerId()
    {
        return $"player_{timeProvider.GetUtcNow().ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }

    private static Position CalculateWeaponOffset(SpriteData playerSprite, SpriteData weaponSprite)
    {
        return new(playerSprite.Width * playerSprite.ScaleFactor / 2 - weaponSprite.Width * weaponSprite.ScaleFactor / 2, 0);
    }
}
