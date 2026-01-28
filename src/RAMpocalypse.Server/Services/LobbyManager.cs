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

        // Add players to lobby
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);

        // Map players to lobby
        playerToLobbyMap[player1] = lobby;
        playerToLobbyMap[player2] = lobby;

        // Store lobby
        lobbies[lobby.Id] = lobby;

        return lobby;
    }

    public GameLobby? RemovePlayerFromLobby(Player player)
    {
        if (!playerToLobbyMap.TryRemove(player, out var lobby))
        {
            return null;
        }

        lobby.Players.Remove(player);

        // Remove lobby if empty
        if (lobby.Players.Count == 0)
        {
            lobbies.TryRemove(lobby.Id, out _);
            return null;
        }

        return lobby;
    }

    public List<Player> GetAllPlayersInLobby(GameLobby lobby)
    {
        return lobby.Players.ToList();
    }
}
