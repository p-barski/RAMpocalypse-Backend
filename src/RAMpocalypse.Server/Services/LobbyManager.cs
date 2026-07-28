using System.Collections.Concurrent;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class LobbyManager(IGameConfig gameConfig, TimeProvider timeProvider) : ILobbyManager
{
    private readonly ConcurrentDictionary<Player, GameLobby> playerToLobbyMap = [];
    private readonly ConcurrentDictionary<string, GameLobby> lobbies = [];
    private readonly IGameConfig gameConfig = gameConfig;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly Lock lobbyJoinLock = new();

    private string GenerateLobbyId()
    {
        return $"lobby_{timeProvider.GetUtcNow().ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }

    public IReadOnlyCollection<GameLobby> GetActiveLobbies()
    {
        var snapshot = new List<GameLobby>(lobbies.Count);
        foreach (var kvp in lobbies)
        {
            snapshot.Add(kvp.Value);
        }

        return snapshot;
    }

    public GameLobby? GetLobbyByPlayer(Player player)
    {
        playerToLobbyMap.TryGetValue(player, out var lobby);
        return lobby;
    }

    public GameLobby CreateLobby(Player player1, Player player2)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var lobby = new GameLobby(GenerateLobbyId(), gameConfig.LobbySize, utcNow);
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);
        playerToLobbyMap[player1] = lobby;
        playerToLobbyMap[player2] = lobby;
        lobbies[lobby.Id] = lobby;
        return lobby;
    }

    public GameLobby? TryAddPlayerToLobby(Player player, int gameWidth, int gameHeight, double xOffset, double yOffset)
    {
        lock (lobbyJoinLock)
        {
            foreach (var kvp in lobbies)
            {
                var lobby = kvp.Value;
                if (lobby.Players.Count < (int)lobby.MaxNumberOfPlayers)
                {
                    var positions = lobby.Players.Select(p => p.Position);
                    player.Position = Position.GetRandomUnoccupiedCorner(positions, gameWidth, gameHeight, xOffset, yOffset);
                    lobby.AddPlayer(player);
                    playerToLobbyMap[player] = lobby;
                    return lobby;
                }
            }
            return null;
        }
    }

    public List<Player> RemovePlayerFromLobby(Player player)
    {
        if (!playerToLobbyMap.TryRemove(player, out var lobby))
        {
            return [];
        }

        lock (lobbyJoinLock)
        {
            lobby.Players.Remove(player);

            // A single remaining player has no one left to play against, so the lobby is removed
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
    }

    public void RemoveLobby(GameLobby lobby)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var player in lobby.Players)
        {
            player.ResetPlayerState(utcNow);
            playerToLobbyMap.TryRemove(player, out _);
        }
        lobbies.TryRemove(lobby.Id, out _);
    }
}
