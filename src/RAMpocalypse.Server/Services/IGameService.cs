using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public interface IGameService
{
    PositionUpdateResult ValidateAndUpdatePosition(Player player, Position newPosition, GameLobby lobby);
    AttackResult PerformMeleeAttack(Player attacker, GameLobby lobby);
    AttackResult PerformProjectileAttack(Player attacker, GameLobby lobby);
    AttackResult PerformSpecialAttack(Player attacker, GameLobby lobby);
    AttackResult HandleProjectileHit(string attackId, Player hitPlayer, GameLobby lobby);
    Player? CheckWinCondition(GameLobby lobby);
    RespawnResult RespawnPlayer(Player player, GameLobby lobby);
}
