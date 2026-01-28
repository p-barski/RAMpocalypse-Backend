using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services.Results;

public class MatchmakingResult
{
    public GameLobby? Lobby { get; set; }
    public List<Player> PlayersToNotify { get; set; } = [];
}
