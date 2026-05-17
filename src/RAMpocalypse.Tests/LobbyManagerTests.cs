using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class LobbyManagerTests
{
    private static Player NewPlayer(string id) => new(id, new("t"));
    private static LobbyManager CreateSut()
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.LobbySize.Returns(MaxNumberOfPlayers.Two);
        return new LobbyManager(gameConfig, TimeProvider.System);
    }

    [Fact]
    public void GetActiveLobbies_NoLobbies_ReturnsEmpty()
    {
        var sut = CreateSut();

        var active = sut.GetActiveLobbies();

        Assert.Empty(active);
    }

    [Fact]
    public void GetActiveLobbies_ReturnsExactlyOneLobby()
    {
        var sut = CreateSut();

        var created = sut.CreateLobby(NewPlayer("a"), NewPlayer("b"));
        var active = sut.GetActiveLobbies();

        Assert.Single(active);
        Assert.Same(created, active.First());
    }

    [Fact]
    public void GetActiveLobbies_HasCountTwoAndDistinctInstances()
    {
        var sut = CreateSut();

        sut.CreateLobby(NewPlayer("a1"), NewPlayer("a2"));
        sut.CreateLobby(NewPlayer("b1"), NewPlayer("b2"));

        var active = sut.GetActiveLobbies();

        Assert.Equal(2, active.Count);
        Assert.Equal(2, active.Select(l => l.Id).Distinct().Count());
    }

    [Fact]
    public void GetActiveLobbies_ReturnsIndependentSnapshot_RemoveLobbyLeavesPriorCountUnchanged()
    {
        var sut = CreateSut();
        var lobby = sut.CreateLobby(NewPlayer("a"), NewPlayer("b"));

        var active = sut.GetActiveLobbies();
        Assert.Single(active);

        sut.RemoveLobby(lobby);

        Assert.Empty(sut.GetActiveLobbies());
        Assert.Single(active);
        Assert.Contains(lobby, active);
    }

    [Fact]
    public void GetLobbyByPlayer_AssociatesBothPlayersWithSameLobby()
    {
        var sut = CreateSut();
        var a = NewPlayer("a");
        var b = NewPlayer("b");
        var lobby = sut.CreateLobby(a, b);

        Assert.Same(lobby, sut.GetLobbyByPlayer(a));
        Assert.Same(lobby, sut.GetLobbyByPlayer(b));
    }

    [Fact]
    public void RemoveLobby_DropsLobbyFromFutureActiveSnapshots()
    {
        var sut = CreateSut();
        var lobby = sut.CreateLobby(NewPlayer("a"), NewPlayer("b"));

        sut.RemoveLobby(lobby);

        Assert.Empty(sut.GetActiveLobbies());
    }

    [Fact]
    public void RemovePlayerFromLobby_WhenLobbyWouldHaveSinglePlayer_Left_RemovesEntireLobby()
    {
        var sut = CreateSut();
        var one = NewPlayer("1");
        var two = NewPlayer("2");
        sut.CreateLobby(one, two);

        sut.RemovePlayerFromLobby(one);

        Assert.Null(sut.GetLobbyByPlayer(two));
        Assert.Empty(sut.GetActiveLobbies());
    }
}
