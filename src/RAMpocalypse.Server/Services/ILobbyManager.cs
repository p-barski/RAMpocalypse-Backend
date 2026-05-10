using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface ILobbyManager
{
    IReadOnlyCollection<GameLobby> GetActiveLobbies();

    GameLobby? GetLobbyByPlayer(Player player);
    GameLobby CreateLobby(Player player1, Player player2);
    List<Player> RemovePlayerFromLobby(Player player);
    void RemoveLobby(GameLobby lobby);
}
