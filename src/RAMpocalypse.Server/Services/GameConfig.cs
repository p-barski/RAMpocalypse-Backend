using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class GameConfig : IGameConfig
{
    public int GameWidth { get; } = 1920;
    public int GameHeight { get; } = 1080;
    public double MaxMovementSpeed { get; } = 500.0;
    public double MaxDistancePerUpdate { get; } = 25.0; // MaxMovementSpeed * 0.05 (50ms buffer)
    public MaxNumberOfPlayers LobbySize { get; } = MaxNumberOfPlayers.Two;
    public int MeleeCooldownMs { get; } = 50;
    public int ProjectileCooldownMs { get; } = 100;
    public int SpecialCooldownMs { get; } = 300;
    public double MeleeRange { get; } = 300.0;
    public double SpecialAttackRange { get; } = 400;
    public double ProjectileSpeed { get; } = 800.0;
    public int MeleeLifetime { get; } = 200;
    public int ProjectileLifetime { get; } = 3000;
    public int SpecialLifetime { get; } = 1000;
    public int MeleeDamage { get; } = 25;
    public int ProjectileDamage { get; } = 15;
    public int SpecialDamage { get; } = 40;
    public int RespawnCooldownMs { get; } = 3000;
}
