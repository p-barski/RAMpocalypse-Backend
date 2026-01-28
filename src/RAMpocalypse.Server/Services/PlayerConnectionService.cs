using System.Collections.Concurrent;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class PlayerConnectionService : IPlayerConnectionService
{
    private readonly ConcurrentDictionary<string, Player> connectionToPlayerMap = [];
    private readonly ConcurrentDictionary<Player, string> playerToConnectionMap = [];

    public Player? GetPlayerByConnectionId(string connectionId)
    {
        connectionToPlayerMap.TryGetValue(connectionId, out var player);
        return player;
    }

    public void AddPlayer(string connectionId, Player player)
    {
        connectionToPlayerMap[connectionId] = player;
        playerToConnectionMap[player] = connectionId;
    }

    public void RemovePlayer(string connectionId)
    {
        if (connectionToPlayerMap.TryRemove(connectionId, out var player))
        {
            playerToConnectionMap.TryRemove(player, out _);
        }
    }

    public string? GetConnectionIdByPlayer(Player player)
    {
        playerToConnectionMap.TryGetValue(player, out var connectionId);
        return connectionId;
    }
}
