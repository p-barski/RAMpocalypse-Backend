using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class GameServiceTests
{
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
