using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class GameServiceTests
{
    // Places `target` just in front of `attacker` (both facing angle 0) so melee/special/projectile
    // hit-detection registers a hit, unless spawn protection filters the target out first.
    private static (Player attacker, Player target, GameLobby lobby) CreateAttackerAndTargetInRange(DateTime lobbyCreationTime)
    {
        var weaponSprite = new SpriteData("weapon.png", 0, 0, 1);
        var attacker = new Player("attacker", new SpriteData("attacker.png", 10, 10, 1))
        {
            Position = new Position(500, 500, 0),
        };
        attacker.SubEntities.Add(new SubEntity(new Position(0, 0), weaponSprite, "weapon_attacker"));

        var target = new Player("target", new SpriteData("target.png", 10, 10, 1))
        {
            Position = new Position(500, 480, 0),
        };

        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, lobbyCreationTime);
        lobby.AddPlayer(attacker);
        lobby.AddPlayer(target);
        return (attacker, target, lobby);
    }

    [Fact]
    public void PerformMeleeAttack_TargetNotSpawnProtected_HitsAndDamagesTarget()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.MeleeRange.Returns(1000.0);
        gameConfig.MeleeDamage.Returns(25);
        gameConfig.SpawnProtectionMs.Returns(1000);

        var timeProvider = new TestTimeProvider(1_000_000);
        var (attacker, target, lobby) = CreateAttackerAndTargetInRange(timeProvider.GetUtcNow().UtcDateTime);
        target.JoinTime = DateTime.MinValue;

        var sut = new GameService(gameConfig, timeProvider);
        var result = sut.PerformMeleeAttack(attacker, lobby);

        Assert.True(result.Success);
        Assert.Single(result.HitPlayers);
        Assert.Equal(75, target.Health);
    }

    [Fact]
    public void PerformMeleeAttack_TargetRecentlyJoined_IsSpawnProtectedAndTakesNoDamage()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.MeleeRange.Returns(1000.0);
        gameConfig.MeleeDamage.Returns(25);
        gameConfig.SpawnProtectionMs.Returns(1000);

        var timeProvider = new TestTimeProvider(1_000_000);
        var (attacker, target, lobby) = CreateAttackerAndTargetInRange(timeProvider.GetUtcNow().UtcDateTime);
        target.JoinTime = timeProvider.GetUtcNow().UtcDateTime;

        var sut = new GameService(gameConfig, timeProvider);
        var result = sut.PerformMeleeAttack(attacker, lobby);

        Assert.True(result.Success);
        Assert.Empty(result.HitPlayers);
        Assert.Equal(100, target.Health);
        Assert.True(target.IsAlive);
    }

    [Fact]
    public void HandleSpecialExplosion_TargetRecentlyJoined_IsSpawnProtectedAndTakesNoDamage()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(1920);
        gameConfig.GameHeight.Returns(1080);
        gameConfig.SpecialAttackRange.Returns(100.0);
        gameConfig.SpecialDamage.Returns(40);
        gameConfig.SpawnProtectionMs.Returns(1000);

        var timeProvider = new TestTimeProvider(1_000_000);
        var (attacker, target, lobby) = CreateAttackerAndTargetInRange(timeProvider.GetUtcNow().UtcDateTime);
        target.JoinTime = timeProvider.GetUtcNow().UtcDateTime;
        attacker.Position = new Position(-10000, -10000, 0); // out of explosion range, so only the target's protection is under test

        var attackEntity = new AttackEntity
        {
            Id = "atk1",
            OwnerId = attacker.Id,
            Type = AttackType.Special,
            CurrentPosition = new Position(500, 500, 0),
            VelocityVector = new Position(0, 0, 0),
            Lifetime = 0,
            CreationTime = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
        };

        var sut = new GameService(gameConfig, timeProvider);
        var result = sut.HandleSpecialExplosion(attackEntity, lobby);

        Assert.Empty(result.HitPlayers);
        Assert.Equal(100, target.Health);
        Assert.True(target.IsAlive);
    }

    [Fact]
    public void HandleProjectileHit_TargetRecentlyJoined_IsSpawnProtectedAndTakesNoDamage()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.ProjectileDamage.Returns(15);
        gameConfig.SpawnProtectionMs.Returns(1000);

        var timeProvider = new TestTimeProvider(1_000_000);
        var target = new Player("target", new SpriteData("target.png", 20, 20, 1))
        {
            Position = new Position(500, 500, 0),
            JoinTime = timeProvider.GetUtcNow().UtcDateTime,
        };
        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, timeProvider.GetUtcNow().UtcDateTime);
        lobby.AddPlayer(target);

        var attackEntity = new AttackEntity
        {
            Id = "atk1",
            OwnerId = "attacker",
            Type = AttackType.Projectile,
            CurrentPosition = new Position(500, 500, 0),
            VelocityVector = new Position(0, 0, 0),
            Lifetime = 3000,
            CreationTime = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
        };
        lobby.LongLivedAttacks[attackEntity.Id] = attackEntity;

        var sut = new GameService(gameConfig, timeProvider);
        var result = sut.HandleProjectileHit(attackEntity.Id, target, lobby);

        Assert.False(result.Success);
        Assert.Empty(result.HitPlayers);
        Assert.Equal(100, target.Health);
        Assert.True(target.IsAlive);
        Assert.True(lobby.LongLivedAttacks.ContainsKey(attackEntity.Id));
    }


    [Fact]
    public void ValidateAndUpdatePosition_ValidMovement_UpdatesPlayerAndReturnsNoCorrection()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(600);
        gameConfig.MovementSpeed.Returns(500);
        gameConfig.PositionUpdateIntervalMs.Returns(30);

        var spriteData = new SpriteData("png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            Position = new Position(100.0, 100.0),
            LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.5)
        };
        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, DateTime.UtcNow);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig, TimeProvider.System);
        var newPosition = new Position(105.0, 102.0);

        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.False(result.NeedsCorrection);
        Assert.Equal(newPosition.X, result.FinalPosition.X);
        Assert.Equal(newPosition.Y, result.FinalPosition.Y);
        Assert.Equal(newPosition.X, player.Position.X);
        Assert.Equal(newPosition.Y, player.Position.Y);
    }

    [Fact]
    public void ValidateAndUpdatePosition_OutOfBounds_ClampsPositionToGameBoundaries()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(600);
        gameConfig.MovementSpeed.Returns(500);
        gameConfig.PositionUpdateIntervalMs.Returns(30);

        var spriteData = new SpriteData("png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            Position = new Position(65.0, 100.0),
            LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.5)
        };
        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, DateTime.UtcNow);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig, TimeProvider.System);
        var newPosition = new Position(63.0, 100.0); // Outside left boundary but within movement distance

        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.False(result.NeedsCorrection);
        Assert.Equal(64.0, result.FinalPosition.X);
        Assert.Equal(100.0, result.FinalPosition.Y);
        Assert.Equal(64.0, player.Position.X);
        Assert.Equal(100.0, player.Position.Y);
    }

    [Fact]
    public void ValidateAndUpdatePosition_MovementTooFar_UpdatesAsFarAsPossibleAndReturnsCorrection()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(800);
        gameConfig.MovementSpeed.Returns(500);
        gameConfig.PositionUpdateIntervalMs.Returns(30);

        var spriteData = new SpriteData("png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            Position = new Position(100.0, 100.0),
            LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.5)
        };
        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, DateTime.UtcNow);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig, TimeProvider.System);
        var newPosition = new Position(500.0, 500.0);

        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.True(result.NeedsCorrection);
        Assert.Equal(121.0, Math.Floor(result.FinalPosition.X));
        Assert.Equal(121.0, Math.Floor(result.FinalPosition.Y));
        Assert.Equal(121.0, Math.Floor(player.Position.X));
        Assert.Equal(121.0, Math.Floor(player.Position.Y));
    }

    [Fact]
    public void ValidateAndUpdatePosition_Dash_UpdatesPositionBasedOnDashVelocity()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(800);
        gameConfig.MovementSpeed.Returns(500);
        gameConfig.DashDurationMs.Returns(500);
        gameConfig.PositionUpdateIntervalMs.Returns(30);

        var spriteData = new SpriteData("png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            IsDashing = true,
            DashVelocity = new(100, 0),
            Position = new Position(100.0, 100.0),
        };
        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, DateTime.UtcNow);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig, TimeProvider.System);
        var newPosition = new Position(103.0, 100.0);
        player.LastPositionUpdateTime = DateTime.UtcNow.AddMilliseconds(-30);
        player.LastDashTime = player.LastPositionUpdateTime;
        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.False(result.NeedsCorrection);
        Assert.Equal(103.0, Math.Floor(result.FinalPosition.X));
        Assert.Equal(100.0, Math.Floor(result.FinalPosition.Y));
        Assert.Equal(103.0, Math.Floor(player.Position.X));
        Assert.Equal(100.0, Math.Floor(player.Position.Y));
    }

    [Fact]
    public void ValidateAndUpdatePosition_Dash_SetsIsDashingFalseWhenPassedDashDuration()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(800);
        gameConfig.MovementSpeed.Returns(500);
        gameConfig.DashDurationMs.Returns(500);
        gameConfig.PositionUpdateIntervalMs.Returns(30);

        var spriteData = new SpriteData("png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            IsDashing = true,
            DashVelocity = new(100, 0),
            Position = new Position(100.0, 100.0),
        };
        var lobby = new GameLobby("l1", MaxNumberOfPlayers.Four, DateTime.UtcNow);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig, TimeProvider.System);
        var newPosition = new Position(150.0, 150.0);
        player.LastPositionUpdateTime = DateTime.UtcNow;
        player.LastDashTime = player.LastPositionUpdateTime.AddMilliseconds(-gameConfig.DashDurationMs);
        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.True(result.NeedsCorrection);
        Assert.False(player.IsDashing);
        Assert.Equal(100.0, Math.Floor(result.FinalPosition.X));
        Assert.Equal(100.0, Math.Floor(result.FinalPosition.Y));
        Assert.Equal(100.0, Math.Floor(player.Position.X));
        Assert.Equal(100.0, Math.Floor(player.Position.Y));
    }

    [Fact]
    public void CalculateDistance_ReturnsCorrectDistance()
    {
        var a = new Position(0.0, 0.0);
        var b = new Position(3.0, 4.0);

        var distance = Position.CalculateDistance(a, b);

        Assert.Equal(5.0, distance);
    }

    [Fact]
    public void CalculatePointToLineDistanceSquared_ReturnsCorrectDistance1()
    {
        var origin = new Position(0.0, 0.0);
        var a = new Position(0.0, 2.0);
        var b = new Position(3.0, 5.0);

        var distance = Position.CalculatePointToLineDistanceSquared(origin, a, b);

        Assert.Equal(4.0, distance);
    }

    [Fact]
    public void CalculatePointToLineDistanceSquared_ReturnsCorrectDistance2()
    {
        var origin = new Position(5.0, 7.0);
        var a = new Position(0.0, 2.0);
        var b = new Position(20.0, 2.0);

        var distance = Position.CalculatePointToLineDistanceSquared(origin, a, b);
        Assert.Equal(25.0, distance);
    }
}
