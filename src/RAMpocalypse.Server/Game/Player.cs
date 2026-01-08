namespace RAMpocalypse.Server.Game;

public class Player(string id, Position position, int spriteVariant = 1)
{
    public string Id { get; init; } = id;
    public Position Position { get; set; } = position;
    public DateTime LastPositionUpdateTime { get; set; } = DateTime.UtcNow;
    public int SpriteVariant { get; init; } = spriteVariant;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public bool IsAlive { get; set; } = true;
    public DateTime LastMeleeAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastProjectileAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastSpecialAttackTime { get; set; } = DateTime.MinValue;
    public DateTime DeathTime { get; set; } = DateTime.MinValue;
}
