using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class GameConfig : IGameConfig
{
    public int GameWidth { get; } = 1920;
    public int GameHeight { get; } = 1080;
    public MaxNumberOfPlayers LobbySize { get; } = MaxNumberOfPlayers.Two;
    public int MeleeCooldownMs { get; } = 50;
    public int ProjectileCooldownMs { get; } = 100;
    public int SpecialCooldownMs { get; } = 300;
    public float MeleeRange { get; } = 300f;
    public double SpecialAttackRange { get; } = 400;
    public float ProjectileSpeed { get; } = 800f;
    public int MeleeDamage { get; } = 25;
    public int ProjectileDamage { get; } = 15;
    public int SpecialDamage { get; } = 40;
    public int RespawnCooldownMs { get; } = 3000;
}
