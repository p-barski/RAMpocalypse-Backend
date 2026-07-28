using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class MatchmakingServiceTests
{
    private static Player NewPlayer(string id) => new(id, new("t"));

    private static (MatchmakingService sut, ILobbyManager lobbyManager, IPlayerConnectionService connectionService) CreateSut(
        MaxNumberOfPlayers lobbySize = MaxNumberOfPlayers.Four)
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.LobbySize.Returns(lobbySize);
        gameConfig.GameWidth.Returns(1920);
        gameConfig.GameHeight.Returns(1080);

        var lobbyManager = new LobbyManager(gameConfig, TimeProvider.System);

        var connectionService = Substitute.For<IPlayerConnectionService>();
        connectionService.GetConnectionIdByPlayer(Arg.Any<Player>()).Returns("connection");

        var sut = new MatchmakingService(connectionService, lobbyManager, gameConfig);
        return (sut, lobbyManager, connectionService);
    }

    [Fact]
    public void RequestMatchmaking_NoOneWaiting_QueuesPlayerAndReturnsNoLobby()
    {
        var (sut, lobbyManager, _) = CreateSut();
        var player = NewPlayer("a");

        var result = sut.RequestMatchmaking(player);

        Assert.Null(result.Lobby);
        Assert.Empty(result.PlayersToNotify);
        Assert.Null(lobbyManager.GetLobbyByPlayer(player));
    }

    [Fact]
    public void RequestMatchmaking_OnePlayerWaiting_CreatesTwoPlayerLobby()
    {
        var (sut, lobbyManager, _) = CreateSut();
        var first = NewPlayer("a");
        var second = NewPlayer("b");

        sut.RequestMatchmaking(first);
        var result = sut.RequestMatchmaking(second);

        Assert.NotNull(result.Lobby);
        Assert.Equal(2, result.Lobby.Players.Count);
        Assert.Equal([second, first], result.PlayersToNotify);
        Assert.Empty(result.ExistingPlayersToNotify);
        Assert.Same(result.Lobby, lobbyManager.GetLobbyByPlayer(first));
        Assert.Same(result.Lobby, lobbyManager.GetLobbyByPlayer(second));
    }

    [Fact]
    public void RequestMatchmaking_NoOneWaitingButLobbyHasSpace_JoinsExistingLobby()
    {
        var (sut, lobbyManager, _) = CreateSut();
        var first = NewPlayer("a");
        var second = NewPlayer("b");
        var third = NewPlayer("c");

        sut.RequestMatchmaking(first);
        sut.RequestMatchmaking(second); // creates the 2-player lobby

        var result = sut.RequestMatchmaking(third);

        Assert.NotNull(result.Lobby);
        Assert.Equal(3, result.Lobby.Players.Count);
        Assert.Equal([third], result.PlayersToNotify);
        Assert.Equal(2, result.ExistingPlayersToNotify.Count);
        Assert.Contains(first, result.ExistingPlayersToNotify);
        Assert.Contains(second, result.ExistingPlayersToNotify);
        Assert.Same(result.Lobby, lobbyManager.GetLobbyByPlayer(third));
    }

    [Fact]
    public void RequestMatchmaking_LobbyFillsUpToFour_ThenNextPlayerIsQueued()
    {
        var (sut, lobbyManager, _) = CreateSut();
        var players = Enumerable.Range(0, 4).Select(i => NewPlayer($"p{i}")).ToList();

        foreach (var player in players)
        {
            sut.RequestMatchmaking(player);
        }

        var lobby = lobbyManager.GetLobbyByPlayer(players[0]);
        Assert.NotNull(lobby);
        Assert.Equal(4, lobby.Players.Count);

        var fifth = NewPlayer("p4");
        var result = sut.RequestMatchmaking(fifth);

        Assert.Null(result.Lobby);
        Assert.Null(lobbyManager.GetLobbyByPlayer(fifth));
    }

    [Fact]
    public void RequestMatchmaking_LobbySizeTwo_DoesNotAddThirdPlayerToFullLobby()
    {
        var (sut, lobbyManager, _) = CreateSut(MaxNumberOfPlayers.Two);
        var first = NewPlayer("a");
        var second = NewPlayer("b");
        var third = NewPlayer("c");

        sut.RequestMatchmaking(first);
        sut.RequestMatchmaking(second);
        var result = sut.RequestMatchmaking(third);

        Assert.Null(result.Lobby);
        Assert.Null(lobbyManager.GetLobbyByPlayer(third));
    }

    [Fact]
    public void RequestMatchmaking_PlayerAlreadyInLobby_ReturnsNoLobby()
    {
        var (sut, _, _) = CreateSut();
        var first = NewPlayer("a");
        var second = NewPlayer("b");
        sut.RequestMatchmaking(first);
        sut.RequestMatchmaking(second);

        var result = sut.RequestMatchmaking(first);

        Assert.Null(result.Lobby);
        Assert.Empty(result.PlayersToNotify);
    }
}
