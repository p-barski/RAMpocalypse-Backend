using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface IGameConfig
{
    int GameWidth { get; }
    int GameHeight { get; }
    int MovementSpeed { get; }
    int PositionUpdateIntervalMs { get; }
    double DashSpeedMultiplier { get; }
    int DashCooldownMs { get; }
    int DashDurationMs { get; }
    MaxNumberOfPlayers LobbySize { get; }
    int MeleeCooldownMs { get; }
    int ProjectileCooldownMs { get; }
    int SpecialCooldownMs { get; }
    double MeleeRange { get; }
    double SpecialAttackRange { get; }
    double ProjectileSpeed { get; }
    double SpecialSpeed { get; }
    int MeleeLifetime { get; }
    int ProjectileLifetime { get; }
    int SpecialLifetime { get; }
    int MeleeDamage { get; }
    int ProjectileDamage { get; }
    int SpecialDamage { get; }
    int RespawnCooldownMs { get; }
    int MaxMessageLength { get; }
}
