using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class GameConfig : IGameConfig
{
    public int GameWidth { get; set; } = 1920;
    public int GameHeight { get; set; } = 1080;
    public int MovementSpeed { get; set; } = 500;
    public int PositionUpdateIntervalMs { get; set; } = 16;
    public double DashSpeedMultiplier { get; set; } = 4.0;
    public int DashCooldownMs { get; set; } = 1000;
    public int DashDurationMs { get; set; } = 250;
    public MaxNumberOfPlayers LobbySize { get; set; } = MaxNumberOfPlayers.Four;
    public int MeleeCooldownMs { get; set; } = 700;
    public int ProjectileCooldownMs { get; set; } = 500;
    public int SpecialCooldownMs { get; set; } = 3000;
    public int SharedAttackCooldownMs { get; set; } = 400;
    public double MeleeRange { get; set; } = 300.0;
    public double SpecialAttackRange { get; set; } = 100.0;
    public double ProjectileSpeed { get; set; } = 800.0;
    public double SpecialSpeed { get; set; } = 400.0;
    public int MeleeLifetime { get; set; } = 200;
    public int ProjectileLifetime { get; set; } = 3000;
    public int SpecialLifetime { get; set; } = 1000;
    public int MeleeDamage { get; set; } = 25;
    public int ProjectileDamage { get; set; } = 15;
    public int SpecialDamage { get; set; } = 40;
    public int RespawnCooldownMs { get; set; } = 3000;
    public int SpawnProtectionMs { get; set; } = 1000;
    public int MaxMessageLength { get; set; } = 100;
    public int MaxNameLength { get; set; } = 20;
}
