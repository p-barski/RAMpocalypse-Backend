namespace RAMpocalypse.Server.Game;

public class SubEntity(Position position, SpriteData spriteData, string id)
{
    public Position Position { get; init; } = position;
    public SpriteData SpriteData { get; init; } = spriteData;
    public string Id { get; init; } = id;
    public List<SubEntity> SubEntities { get; set; } = [];
}

public class Player(string id, SpriteData spriteData)
{
    public string Id { get; init; } = id;
    public Position Position { get; set; } = new Position(0, 0);
    public SpriteData SpriteData { get; init; } = spriteData;
    public List<SubEntity> SubEntities { get; set; } = [];
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
    public List<(Position, Position)> GetHitboxLines()
    {
        List<Position> corners = [
            this.Position,
            new Position(this.Position.X + (this.SpriteData.Width * this.SpriteData.ScaleFactor), this.Position.Y),
            new Position(this.Position.X + (this.SpriteData.Width * this.SpriteData.ScaleFactor), this.Position.Y + (this.SpriteData.Height * this.SpriteData.ScaleFactor)),
            new Position(this.Position.X, this.Position.Y + (this.SpriteData.Height * this.SpriteData.ScaleFactor)),
        ];
        return [
            (corners[0], corners[1]),
            (corners[1], corners[2]),
            (corners[2], corners[3]),
            (corners[3], corners[0]),
        ];
    }
}
