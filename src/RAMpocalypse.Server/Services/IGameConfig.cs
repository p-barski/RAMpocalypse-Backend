using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface IGameConfig
{
    int GameWidth { get; }
    int GameHeight { get; }
    MaxNumberOfPlayers LobbySize { get; }
    int MeleeCooldownMs { get; }
    int ProjectileCooldownMs { get; }
    int SpecialCooldownMs { get; }
    float MeleeRange { get; }
    double SpecialAttackRange { get; }
    float ProjectileSpeed { get; }
    int MeleeDamage { get; }
    int ProjectileDamage { get; }
    int SpecialDamage { get; }
    int RespawnCooldownMs { get; }
}
