using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class GameConfig : IGameConfig
{
    public int GameWidth { get; set; } = 1920;
    public int GameHeight { get; set; } = 1080;
    public double MaxMovementSpeed { get; set; } = 500.0;
    public double MaxDistancePerUpdate { get; set; } = 25.0;
    public MaxNumberOfPlayers LobbySize { get; set; } = MaxNumberOfPlayers.Two;
    public int MeleeCooldownMs { get; set; } = 50;
    public int ProjectileCooldownMs { get; set; } = 100;
    public int SpecialCooldownMs { get; set; } = 300;
    public double MeleeRange { get; set; } = 300.0;
    public double SpecialAttackRange { get; set; } = 400;
    public double ProjectileSpeed { get; set; } = 800.0;
    public int MeleeLifetime { get; set; } = 200;
    public int ProjectileLifetime { get; set; } = 3000;
    public int SpecialLifetime { get; set; } = 1000;
    public int MeleeDamage { get; set; } = 25;
    public int ProjectileDamage { get; set; } = 15;
    public int SpecialDamage { get; set; } = 40;
    public int RespawnCooldownMs { get; set; } = 3000;
}
