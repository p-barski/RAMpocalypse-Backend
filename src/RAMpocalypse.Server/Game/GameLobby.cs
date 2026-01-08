namespace RAMpocalypse.Server.Game;

public class GameLobby(string id, MaxNumberOfPlayers maxNumberOfPlayers)
{
    public string Id { get; init; } = id;
    public List<Player> Players { get; init; } = [];
    public MaxNumberOfPlayers MaxNumberOfPlayers { get; init; } = maxNumberOfPlayers;

    public void AddPlayer(Player player)
    {
        Players.Add(player);
    }
}
