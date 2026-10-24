using RAMpocalypse.Server.Game;
using Xunit;

namespace RAMpocalypse.Tests;

public class PlayerTests
{
    private static Player CreatePlayerWithWeapon(double angle)
    {
        var player = new Player("player", new SpriteData("player.png", 420, 128, 1))
        {
            Position = new Position(500, 500, angle),
        };
        // weapon is 102 x 318 after scaling, offset 159 to the right of player center
        player.SubEntities.Add(new SubEntity(new Position(159, 0), new SpriteData("weapon.png", 17, 53, 6), "weapon_player"));
        return player;
    }

    [Fact]
    public void GetAttackPosition_FacingUp_ReturnsTopOfWeapon()
    {
        var player = CreatePlayerWithWeapon(0);

        var position = player.GetAttackPosition();

        Assert.Equal(659, position.X, 6);
        Assert.Equal(341, position.Y, 6);
    }

    [Fact]
    public void GetAttackPosition_FacingRight_WeaponRotatesAroundItsBottomCenter()
    {
        var player = CreatePlayerWithWeapon(Math.PI / 2);

        var position = player.GetAttackPosition();

        Assert.Equal(977, position.X, 6);
        Assert.Equal(659, position.Y, 6);
    }

    [Fact]
    public void GetAttackPosition_FacingDown_WeaponRotatesAroundItsBottomCenter()
    {
        var player = CreatePlayerWithWeapon(Math.PI);

        var position = player.GetAttackPosition();

        Assert.Equal(659, position.X, 6);
        Assert.Equal(977, position.Y, 6);
    }

    [Theory]
    [InlineData(100, 159)]
    [InlineData(1800, -159)]
    public void SetWeaponArmTowardsCenter_HoldsWeaponInArmFacingArenaCenter(double x, double expectedWeaponX)
    {
        var player = CreatePlayerWithWeapon(0);
        player.Position = new Position(x, 500);

        player.SetWeaponArmTowardsCenter(1920);

        Assert.Equal(expectedWeaponX, player.SubEntities[0].Position.X);
    }

    [Fact]
    public void SwitchWeaponArm_MirrorsWeaponAndAttackPosition()
    {
        var player = CreatePlayerWithWeapon(0);

        player.SwitchWeaponArm();

        Assert.Equal(-159, player.SubEntities[0].Position.X);
        Assert.Equal(341, player.GetAttackPosition().X, 6);
    }
}
