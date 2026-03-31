using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface IGameConfig
{
    int GameWidth { get; }
    int GameHeight { get; }
    double MaxMovementSpeed { get; }
    double MaxDistancePerUpdate { get; }
    MaxNumberOfPlayers LobbySize { get; }
    int MeleeCooldownMs { get; }
    int ProjectileCooldownMs { get; }
    int SpecialCooldownMs { get; }
    double MeleeRange { get; }
    double SpecialAttackRange { get; }
    double ProjectileSpeed { get; }
    int MeleeLifetime { get; }
    int ProjectileLifetime { get; }
    int SpecialLifetime { get; }
    int MeleeDamage { get; }
    int ProjectileDamage { get; }
    int SpecialDamage { get; }
    int RespawnCooldownMs { get; }
    int MaxMessageLength { get; }
}
