using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public interface IGameService
{
    PositionUpdateResult ValidateAndUpdatePosition(Player player, Position newPosition, GameLobby lobby);
    AttackResult PerformMeleeAttack(Player attacker, Position attackDirection, GameLobby lobby);
    AttackResult PerformProjectileAttack(Player attacker, Position attackDirection, GameLobby lobby);
    AttackResult PerformSpecialAttack(Player attacker, Position attackPosition, GameLobby lobby);
    AttackResult HandleProjectileHit(Player projectileOwner, Player hitPlayer, GameLobby lobby);
    Player? CheckWinCondition(GameLobby lobby);
    RespawnResult RespawnPlayer(Player player, GameLobby lobby);
}
