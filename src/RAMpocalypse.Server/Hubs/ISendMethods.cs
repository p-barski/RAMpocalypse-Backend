using RAMpocalypse.Server.Game;

public interface ISendMethods
{
    Task PlayerDamaged(string playerId, int damage, int health);
    Task AttackPerformed(string playerId, AttackType attackType, Position attackPosition, Position attackDirection);
    Task PlayerPositionUpdated(string playerId, Position position);
    Task PositionCorrected(Position correctedPosition);
    Task LobbyStarted(string lobbyId, List<Player> players);
    Task PlayerDied(string playerId);
    Task PlayerRespawned(string playerId, Position position);
    Task PlayerLeftLobby(string playerId);
    Task GameEnded(string winnerId);
    Task ReceiveMessage(string user, string message);
}