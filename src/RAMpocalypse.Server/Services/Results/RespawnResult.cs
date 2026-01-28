using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services.Results;

public class RespawnResult
{
    public Position RespawnPosition { get; set; }
    public List<Player> PlayersToNotify { get; set; } = [];
}
