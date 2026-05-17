namespace RAMpocalypse.Server.Game;

public class AttackEntity
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public AttackType Type { get; set; }
    public Position CurrentPosition { get; set; }
    public Position VelocityVector { get; set; }
    public long Lifetime { get; set; }
    public long CreationTime { get; set; }
}