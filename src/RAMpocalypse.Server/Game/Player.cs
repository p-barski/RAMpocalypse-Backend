namespace RAMpocalypse.Server.Game;

public class Player(string id, SpriteData spriteData)
{
    public string Id { get; init; } = id;
    public Position Position { get; set; } = new Position(0, 0);
    public SpriteData SpriteData { get; init; } = spriteData;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public bool IsAlive { get; set; } = true;
    public DateTime LastPositionUpdateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastMeleeAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastProjectileAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastSpecialAttackTime { get; set; } = DateTime.MinValue;
    public DateTime DeathTime { get; set; } = DateTime.MinValue;
    public void ResetPlayerState()
    {
        Health = MaxHealth;
        IsAlive = true;
        LastPositionUpdateTime = DateTime.UtcNow;
        LastMeleeAttackTime = DateTime.MinValue;
        LastProjectileAttackTime = DateTime.MinValue;
        LastSpecialAttackTime = DateTime.MinValue;
        DeathTime = DateTime.MinValue;
    }
}
