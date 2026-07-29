namespace RAMpocalypse.Server.Game;

public class SubEntity(Position position, SpriteData spriteData, string id)
{
    public Position Position { get; set; } = position;
    public SpriteData SpriteData { get; set; } = spriteData;
    public string Id { get; init; } = id;
    public List<SubEntity> SubEntities { get; set; } = [];
}

public class Player(string id, SpriteData spriteData)
{
    public string Id { get; init; } = id;
    public Position Position { get; set; } = new Position(0, 0);
    public SpriteData SpriteData { get; set; } = spriteData;
    public List<SubEntity> SubEntities { get; set; } = [];
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public bool IsAlive { get; set; } = true;
    public DateTime LastPositionUpdateTime { get; set; } = DateTime.MinValue;
    public DateTime LastMeleeAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastProjectileAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastSpecialAttackTime { get; set; } = DateTime.MinValue;
    public DateTime LastAnyAttackTime { get; set; } = DateTime.MinValue;
    public DateTime DeathTime { get; set; } = DateTime.MinValue;
    public DateTime JoinTime { get; set; } = DateTime.MinValue;
    public DateTime LastDashTime { get; set; } = DateTime.MinValue;
    public Position DashVelocity { get; set; }
    public bool IsDashing { get; set; } = false;
    public void ResetPlayerState(DateTime utcNow)
    {
        Health = MaxHealth;
        IsAlive = true;
        LastPositionUpdateTime = utcNow;
        LastMeleeAttackTime = DateTime.MinValue;
        LastProjectileAttackTime = DateTime.MinValue;
        LastSpecialAttackTime = DateTime.MinValue;
        LastAnyAttackTime = DateTime.MinValue;
        DeathTime = DateTime.MinValue;
        JoinTime = DateTime.MinValue;
    }
    public (Position, Position)[] GetHitboxLines()
    {
        double halfWidth = this.SpriteData.Width * this.SpriteData.ScaleFactor / 2.0;
        double halfHeight = this.SpriteData.Height * this.SpriteData.ScaleFactor / 2.0;
        double sin = Math.Sin(this.Position.Angle);
        double cos = Math.Cos(this.Position.Angle);
        double dxSin = halfWidth * sin;
        double dxCos = halfWidth * cos;
        double dySin = halfHeight * sin;
        double dyCos = halfHeight * cos;

        Position[] corners = [
            new(this.Position.X - dxCos + dySin, this.Position.Y - dxSin - dyCos),
            new(this.Position.X + dxCos + dySin, this.Position.Y + dxSin - dyCos),
            new(this.Position.X + dxCos - dySin, this.Position.Y + dxSin + dyCos),
            new(this.Position.X - dxCos - dySin, this.Position.Y - dxSin + dyCos),
        ];
        return [
            (corners[0], corners[1]),
            (corners[1], corners[2]),
            (corners[2], corners[3]),
            (corners[3], corners[0]),
        ];
    }
    public Position GetAttackPosition(double? sin = null, double? cos = null)
    {
        sin ??= Math.Sin(this.Position.Angle);
        cos ??= Math.Cos(this.Position.Angle);
        var weapon = SubEntities[0];
        double yOffset = weapon.Position.Y - weapon.SpriteData.Height * weapon.SpriteData.ScaleFactor / 2;
        double rotatedX = weapon.Position.X * cos.Value - yOffset * sin.Value;
        double rotatedY = weapon.Position.X * sin.Value + yOffset * cos.Value;
        double x = this.Position.X + rotatedX;
        double y = this.Position.Y + rotatedY;
        return new Position(x, y);
    }
}
