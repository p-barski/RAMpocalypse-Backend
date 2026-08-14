using RAMpocalypse.Server.Game;
using Xunit;

namespace RAMpocalypse.Tests;

public class PositionTests
{
    private static void AssertSpawnAngle(Position position, double xOffset, double yOffset, int gameWidth, int gameHeight)
    {
        var isTop = position.Y == yOffset;
        var isBottom = position.Y == gameHeight - yOffset;
        var isLeft = position.X == xOffset;
        var isRight = position.X == gameWidth - xOffset;

        if (isTop && isLeft)
        {
            Assert.Equal(3 * Math.PI / 4, position.Angle); // faces south-east, toward center
        }
        else if (isTop && isRight)
        {
            Assert.Equal(-3 * Math.PI / 4, position.Angle); // faces south-west, toward center
        }
        else if (isBottom && isLeft)
        {
            Assert.Equal(Math.PI / 4, position.Angle); // faces north-east, toward center
        }
        else if (isBottom && isRight)
        {
            Assert.Equal(-Math.PI / 4, position.Angle); // faces north-west, toward center
        }
        else
        {
            Assert.Fail($"Unexpected spawn coordinate: ({position.X}, {position.Y})");
        }
    }

    [Fact]
    public void GetRandomOppositeCorners_CornersFaceDiagonallyTowardCenter()
    {
        const int gameWidth = 800;
        const int gameHeight = 600;
        const double xOffset = 32;
        const double yOffset = 32;

        for (var i = 0; i < 50; i++)
        {
            var (corner1, corner2) = Position.GetRandomOppositeCorners(gameWidth, gameHeight, xOffset, yOffset);

            AssertSpawnAngle(corner1, xOffset, yOffset, gameWidth, gameHeight);
            AssertSpawnAngle(corner2, xOffset, yOffset, gameWidth, gameHeight);
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
