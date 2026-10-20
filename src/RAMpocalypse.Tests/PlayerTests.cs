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
    public void GetAttackPosition_FacingRight_WeaponRotatesAroundItsOwnCenter()
    {
        var player = CreatePlayerWithWeapon(Math.PI / 2);

        var position = player.GetAttackPosition();

        Assert.Equal(818, position.X, 6);
        Assert.Equal(500, position.Y, 6);
    }

    [Fact]
    public void GetAttackPosition_FacingDown_WeaponRotatesAroundItsOwnCenter()
    {
        var player = CreatePlayerWithWeapon(Math.PI);

        var position = player.GetAttackPosition();

        Assert.Equal(659, position.X, 6);
        Assert.Equal(659, position.Y, 6);
    }
}
