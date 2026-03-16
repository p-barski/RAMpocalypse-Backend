using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services.Results;

public class AttackResult
{
    public bool Success { get; set; }
    public List<HitPlayerInfo> HitPlayers { get; set; } = [];
    public List<AttackEntity> AttackEntites { get; set; } = [];
    public List<Player> PlayersToNotify { get; set; } = [];
}
