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
        gameConfig.MaxMovementSpeed.Returns(500.0);
        gameConfig.MaxDistancePerUpdate.Returns(25.0);

        var spriteData = new SpriteData("/assets/sprites/player_1.png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            Position = new Position(100.0, 100.0),
            LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.5)
        };
        var lobby = new GameLobby("lobby-1", MaxNumberOfPlayers.Four);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig);
        var newPosition = new Position(105.0, 102.0);

        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.False(result.NeedsCorrection);
        Assert.Equal(newPosition.X, result.CorrectedPosition.X);
        Assert.Equal(newPosition.Y, result.CorrectedPosition.Y);
        Assert.Equal(newPosition.X, player.Position.X);
        Assert.Equal(newPosition.Y, player.Position.Y);
    }

    [Fact]
    public void ValidateAndUpdatePosition_OutOfBounds_CorrectsPosition()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(600);
        gameConfig.MaxMovementSpeed.Returns(500.0);
        gameConfig.MaxDistancePerUpdate.Returns(25.0);

        var spriteData = new SpriteData("/assets/sprites/player_1.png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            Position = new Position(65.0, 100.0),
            LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.5)
        };
        var lobby = new GameLobby("lobby-1", MaxNumberOfPlayers.Four);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig);
        var newPosition = new Position(63.0, 100.0); // Outside left boundary but within movement distance

        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.True(result.NeedsCorrection);
        Assert.Equal(64.0, result.CorrectedPosition.X);
        Assert.Equal(100.0, result.CorrectedPosition.Y);
    }

    [Fact]
    public void ValidateAndUpdatePosition_MovementTooFar_DoesNotUpdateAndReturnsCorrection()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.GameWidth.Returns(800);
        gameConfig.GameHeight.Returns(800);
        gameConfig.MaxMovementSpeed.Returns(500.0);
        gameConfig.MaxDistancePerUpdate.Returns(25.0);

        var spriteData = new SpriteData("/assets/sprites/player_1.png", 64, 32, 2);
        var player = new Player("p1", spriteData)
        {
            Position = new Position(100.0, 100.0),
            LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.02)
        };
        var lobby = new GameLobby("lobby-1", MaxNumberOfPlayers.Four);
        lobby.AddPlayer(player);

        var sut = new GameService(gameConfig);
        var newPosition = new Position(500.0, 500.0); // Teleport - way too far

        var result = sut.ValidateAndUpdatePosition(player, newPosition, lobby);

        Assert.True(result.NeedsCorrection);
        Assert.Equal(100.0, result.CorrectedPosition.X);
        Assert.Equal(100.0, result.CorrectedPosition.Y);
        Assert.Equal(100.0, player.Position.X);
        Assert.Equal(100.0, player.Position.Y);
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
