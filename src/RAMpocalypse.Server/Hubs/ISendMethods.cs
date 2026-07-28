using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

public interface ISendMethods
{
    Task PlayerDamaged(string playerId, int damage, int health);
    Task AttackPerformed(List<AttackEntity> attackEntities);
    Task PlayerPositionUpdated(string playerId, Position position);
    Task PositionCorrected(Position correctedPosition);
    Task LobbyStarted(string lobbyId, List<Player> players);
    Task PlayerJoinedLobby(Player player);
    Task PlayerDied(string playerId);
    Task PlayerRespawned(string playerId, Position position);
    Task PlayerLeftLobby(string playerId);
    Task GameEnded(string winnerId);
    Task MessageReceived(ChatMessage message);
}
