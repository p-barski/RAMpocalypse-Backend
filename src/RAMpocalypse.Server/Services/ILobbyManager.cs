using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface ILobbyManager
{
    GameLobby? GetLobbyByPlayer(Player player);
    GameLobby CreateLobby(Player player1, Player player2);
    GameLobby? RemovePlayerFromLobby(Player player);
    List<Player> GetAllPlayersInLobby(GameLobby lobby);
}
