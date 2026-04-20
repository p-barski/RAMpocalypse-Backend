using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class ReloadableGameConfig(IOptionsMonitor<GameConfig> monitor) : IGameConfig
{
    private readonly IOptionsMonitor<GameConfig> monitor = monitor;
    public int GameWidth => monitor.CurrentValue.GameWidth;
    public int GameHeight => monitor.CurrentValue.GameHeight;
    public int MovementSpeed => monitor.CurrentValue.MovementSpeed;
    public int PositionUpdateIntervalMs => monitor.CurrentValue.PositionUpdateIntervalMs;
    public double DashSpeedMultiplier => monitor.CurrentValue.DashSpeedMultiplier;
    public int DashCooldownMs => monitor.CurrentValue.DashCooldownMs;
    public int DashDurationMs => monitor.CurrentValue.DashDurationMs;
    public MaxNumberOfPlayers LobbySize => monitor.CurrentValue.LobbySize;
    public int MeleeCooldownMs => monitor.CurrentValue.MeleeCooldownMs;
    public int ProjectileCooldownMs => monitor.CurrentValue.ProjectileCooldownMs;
    public int SpecialCooldownMs => monitor.CurrentValue.SpecialCooldownMs;
    public double MeleeRange => monitor.CurrentValue.MeleeRange;
    public double SpecialAttackRange => monitor.CurrentValue.SpecialAttackRange;
    public double ProjectileSpeed => monitor.CurrentValue.ProjectileSpeed;
    public double SpecialSpeed => monitor.CurrentValue.SpecialSpeed;
    public int MeleeLifetime => monitor.CurrentValue.MeleeLifetime;
    public int ProjectileLifetime => monitor.CurrentValue.ProjectileLifetime;
    public int SpecialLifetime => monitor.CurrentValue.SpecialLifetime;
    public int MeleeDamage => monitor.CurrentValue.MeleeDamage;
    public int ProjectileDamage => monitor.CurrentValue.ProjectileDamage;
    public int SpecialDamage => monitor.CurrentValue.SpecialDamage;
    public int RespawnCooldownMs => monitor.CurrentValue.RespawnCooldownMs;
    public int MaxMessageLength => monitor.CurrentValue.MaxMessageLength;
}
