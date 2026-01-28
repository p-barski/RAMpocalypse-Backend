using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services.Results;

public class AttackResult
{
    public bool Success { get; set; }
    public string AttackerId { get; set; } = string.Empty;
    public AttackType AttackType { get; set; }
    public List<HitPlayerInfo> HitPlayers { get; set; } = [];
    public Position AttackPosition { get; set; }
    public Position AttackDirection { get; set; }
    public List<Player> PlayersToNotify { get; set; } = [];
}
