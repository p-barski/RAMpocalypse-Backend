using System.Collections.Concurrent;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class LobbyManager(IGameConfig gameConfig) : ILobbyManager
{
    private readonly ConcurrentDictionary<Player, GameLobby> playerToLobbyMap = [];
    private readonly ConcurrentDictionary<string, GameLobby> lobbies = [];
    private readonly IGameConfig gameConfig = gameConfig;

    private static string GenerateLobbyId()
    {
        return $"lobby_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }

    public GameLobby? GetLobbyByPlayer(Player player)
    {
        playerToLobbyMap.TryGetValue(player, out var lobby);
        return lobby;
    }

    public GameLobby CreateLobby(Player player1, Player player2)
    {
        var lobby = new GameLobby(GenerateLobbyId(), gameConfig.LobbySize);
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);
        playerToLobbyMap[player1] = lobby;
        playerToLobbyMap[player2] = lobby;
        lobbies[lobby.Id] = lobby;
        return lobby;
    }

    public List<Player> RemovePlayerFromLobby(Player player)
    {
        if (!playerToLobbyMap.TryRemove(player, out var lobby))
        {
            return [];
        }
        lobby.Players.Remove(player);

        // People can't join lobby that already started, so remove the lobby if there is only one player left
        if (lobby.Players.Count == 1)
        {
            foreach (var lobbyPlayer in lobby.Players)
            {
                playerToLobbyMap.TryRemove(lobbyPlayer, out _);
            }
            lobbies.TryRemove(lobby.Id, out _);
        }
        return lobby.Players;
    }

    public void RemoveLobby(GameLobby lobby)
    {
        foreach (var player in lobby.Players)
        {
            player.ResetPlayerState();
            playerToLobbyMap.TryRemove(player, out _);
        }
        lobbies.TryRemove(lobby.Id, out _);
    }
}
