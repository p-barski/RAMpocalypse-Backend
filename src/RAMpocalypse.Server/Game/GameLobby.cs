using System.Collections.Concurrent;

namespace RAMpocalypse.Server.Game;

public class GameLobby(string id, MaxNumberOfPlayers maxNumberOfPlayers, DateTime creationTime)
{
    public string Id { get; init; } = id;
    public List<Player> Players { get; init; } = [];
    public MaxNumberOfPlayers MaxNumberOfPlayers { get; init; } = maxNumberOfPlayers;
    public DateTime CreationTime { get; init; } = creationTime;
    public ConcurrentDictionary<string, AttackEntity> LongLivedAttacks { get; init; } = [];

    public void AddPlayer(Player player)
    {
        Players.Add(player);
    }
}
