using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class ReloadableGameConfig : IGameConfig
{
    private GameConfig cache;
    public ReloadableGameConfig(IOptionsMonitor<GameConfig> monitor)
    {
        cache = monitor.CurrentValue;
        monitor.OnChange(config => cache = config);
    }
    public int GameWidth => cache.GameWidth;
    public int GameHeight => cache.GameHeight;
    public int MovementSpeed => cache.MovementSpeed;
    public int PositionUpdateIntervalMs => cache.PositionUpdateIntervalMs;
    public double DashSpeedMultiplier => cache.DashSpeedMultiplier;
    public int DashCooldownMs => cache.DashCooldownMs;
    public int DashDurationMs => cache.DashDurationMs;
    public MaxNumberOfPlayers LobbySize => cache.LobbySize;
    public int MeleeCooldownMs => cache.MeleeCooldownMs;
    public int ProjectileCooldownMs => cache.ProjectileCooldownMs;
    public int SpecialCooldownMs => cache.SpecialCooldownMs;
    public int SharedAttackCooldownMs => cache.SharedAttackCooldownMs;
    public double MeleeRange => cache.MeleeRange;
    public double SpecialAttackRange => cache.SpecialAttackRange;
    public double ProjectileSpeed => cache.ProjectileSpeed;
    public double SpecialSpeed => cache.SpecialSpeed;
    public int MeleeLifetime => cache.MeleeLifetime;
    public int ProjectileLifetime => cache.ProjectileLifetime;
    public int SpecialLifetime => cache.SpecialLifetime;
    public int MeleeDamage => cache.MeleeDamage;
    public int ProjectileDamage => cache.ProjectileDamage;
    public int SpecialDamage => cache.SpecialDamage;
    public int RespawnCooldownMs => cache.RespawnCooldownMs;
    public int SpawnProtectionMs => cache.SpawnProtectionMs;
    public int MaxMessageLength => cache.MaxMessageLength;
    public int MaxNameLength => cache.MaxNameLength;
}
