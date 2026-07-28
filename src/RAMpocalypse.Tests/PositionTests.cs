using RAMpocalypse.Server.Game;
using Xunit;

namespace RAMpocalypse.Tests;

public class PositionTests
{
    private static void AssertSpawnAngle(Position position, double yOffset, int gameHeight)
    {
        if (position.Y == yOffset)
        {
            Assert.Equal(Math.PI, position.Angle);
        }
        else if (position.Y == gameHeight - yOffset)
        {
            Assert.Equal(0, position.Angle);
        }
        else
        {
            Assert.Fail($"Unexpected spawn Y coordinate: {position.Y}");
        }
    }

    [Fact]
    public void GetRandomOppositeCorners_TopCornersFaceDown_BottomCornersFaceUp()
    {
        const int gameWidth = 800;
        const int gameHeight = 600;
        const double xOffset = 32;
        const double yOffset = 32;

        for (var i = 0; i < 50; i++)
        {
            var (corner1, corner2) = Position.GetRandomOppositeCorners(gameWidth, gameHeight, xOffset, yOffset);

            AssertSpawnAngle(corner1, yOffset, gameHeight);
            AssertSpawnAngle(corner2, yOffset, gameHeight);
        }
    }

    [Fact]
    public void GetRandomUnoccupiedCorner_TwoCornersOccupied_ReturnsOneOfTheRemainingTwo()
    {
        const int gameWidth = 800;
        const int gameHeight = 600;
        const double xOffset = 32;
        const double yOffset = 32;
        var occupied = new[]
        {
            new Position(xOffset, yOffset),             // top-left
            new Position(gameWidth - xOffset, yOffset), // top-right
        };

        for (var i = 0; i < 50; i++)
        {
            var corner = Position.GetRandomUnoccupiedCorner(occupied, gameWidth, gameHeight, xOffset, yOffset);

            Assert.True(corner.Y == gameHeight - yOffset, $"Expected a bottom corner, got Y={corner.Y}");
        }
    }

    [Fact]
    public void GetRandomUnoccupiedCorner_ThreeCornersOccupied_ReturnsLastRemainingCorner()
    {
        const int gameWidth = 800;
        const int gameHeight = 600;
        const double xOffset = 32;
        const double yOffset = 32;
        var occupied = new[]
        {
            new Position(xOffset, yOffset),              // top-left
            new Position(gameWidth - xOffset, yOffset),  // top-right
            new Position(xOffset, gameHeight - yOffset), // bottom-left
        };

        for (var i = 0; i < 50; i++)
        {
            var corner = Position.GetRandomUnoccupiedCorner(occupied, gameWidth, gameHeight, xOffset, yOffset);

            Assert.Equal(gameWidth - xOffset, corner.X);
            Assert.Equal(gameHeight - yOffset, corner.Y);
        }
    }
}
