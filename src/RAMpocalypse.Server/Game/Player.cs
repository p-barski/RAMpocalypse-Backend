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
    public string Name { get; set; } = "";
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
    public DateTime LastArmSwitchTime { get; set; } = DateTime.MinValue;
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
        LastArmSwitchTime = DateTime.MinValue;
        DeathTime = DateTime.MinValue;
        JoinTime = DateTime.MinValue;
    }
    public (Position, Position)[] GetHitboxLines()
    {
        double halfWidth = this.SpriteData.Width * this.SpriteData.ScaleFactor / 2.0;
        double halfHeight = this.SpriteData.Height * this.SpriteData.ScaleFactor / 2.0;

        Position[] corners = [
            new(this.Position.X - halfWidth, this.Position.Y - halfHeight),
            new(this.Position.X + halfWidth, this.Position.Y - halfHeight),
            new(this.Position.X + halfWidth, this.Position.Y + halfHeight),
            new(this.Position.X - halfWidth, this.Position.Y + halfHeight),
        ];
        return [
            (corners[0], corners[1]),
            (corners[1], corners[2]),
            (corners[2], corners[3]),
            (corners[3], corners[0]),
        ];
    }
    public void SetWeaponArmTowardsCenter(int gameWidth)
    {
        var weapon = SubEntities.ElementAtOrDefault(0);
        if (weapon is null) return;
        var offsetX = Math.Abs(weapon.Position.X);
        weapon.Position = weapon.Position with { X = Position.X < gameWidth / 2.0 ? offsetX : -offsetX };
    }
    public void SwitchWeaponArm()
    {
        var weapon = SubEntities.ElementAtOrDefault(0);
        if (weapon is null) return;
        weapon.Position = weapon.Position with { X = -weapon.Position.X };
    }
    public Position GetAttackPosition(double? sin = null, double? cos = null)
    {
        sin ??= Math.Sin(this.Position.Angle);
        cos ??= Math.Cos(this.Position.Angle);
        var weapon = SubEntities[0];
        double weaponHeight = weapon.SpriteData.Height * weapon.SpriteData.ScaleFactor;
        double x = this.Position.X + weapon.Position.X + weaponHeight * sin.Value;
        double y = this.Position.Y + weapon.Position.Y + weaponHeight / 2 - weaponHeight * cos.Value;
        return new Position(x, y);
    }
}
