using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class MatchmakingQueueTests
{
    private static Player CreatePlayer(string id = "player-1") => new(id, new SpriteData("url"));

    [Fact]
    public void Count_EmptyQueue_ReturnsZero()
    {
        var queue = new MatchmakingQueue();
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void Count_AfterEnqueue_ReturnsCorrectCount()
    {
        var queue = new MatchmakingQueue();
        queue.Enqueue(CreatePlayer("p1"));
        queue.Enqueue(CreatePlayer("p2"));
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void Count_AfterDequeue_Decrements()
    {
        var queue = new MatchmakingQueue();
        queue.Enqueue(CreatePlayer("p1"));
        queue.Enqueue(CreatePlayer("p2"));
        queue.TryDequeue(out _);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void Count_AfterRemove_Decrements()
    {
        var queue = new MatchmakingQueue();
        var player = CreatePlayer();
        queue.Enqueue(player);
        queue.Enqueue(CreatePlayer("p2"));
        queue.Remove(player);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void Count_AfterClear_ReturnsZero()
    {
        var queue = new MatchmakingQueue();
        queue.Enqueue(CreatePlayer("p1"));
        queue.Enqueue(CreatePlayer("p2"));
        queue.Clear();
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void Enqueue_MultiplePlayes_AllContained()
    {
        var queue = new MatchmakingQueue();
        var p1 = CreatePlayer("p1");
        var p2 = CreatePlayer("p2");
        queue.Enqueue(p1);
        queue.Enqueue(p2);
        Assert.True(queue.Contains(p1));
        Assert.True(queue.Contains(p2));
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalse()
    {
        var queue = new MatchmakingQueue();
        var result = queue.TryDequeue(out var player);
        Assert.False(result);
        Assert.Null(player);
    }

    [Fact]
    public void TryDequeue_WithPlayer_ReturnsTrueAndPlayer()
    {
        var queue = new MatchmakingQueue();
        var enqueued = CreatePlayer();
        queue.Enqueue(enqueued);
        var result = queue.TryDequeue(out var dequeued);
        Assert.True(result);
        Assert.Equal(enqueued, dequeued);
    }

    [Fact]
    public void TryDequeue_PreservesFifoOrder()
    {
        var queue = new MatchmakingQueue();
        var p1 = CreatePlayer("p1");
        var p2 = CreatePlayer("p2");
        var p3 = CreatePlayer("p3");
        queue.Enqueue(p1);
        queue.Enqueue(p2);
        queue.Enqueue(p3);

        queue.TryDequeue(out var first);
        queue.TryDequeue(out var second);
        queue.TryDequeue(out var third);

        Assert.Equal(p1, first);
        Assert.Equal(p2, second);
        Assert.Equal(p3, third);
    }

    [Fact]
    public void TryDequeue_PlayerNoLongerContainedAfterDequeue()
    {
        var queue = new MatchmakingQueue();
        var player = CreatePlayer();
        queue.Enqueue(player);
        queue.TryDequeue(out _);
        Assert.False(queue.Contains(player));
    }

    [Fact]
    public void Remove_ExistingPlayer_ReturnsTrue()
    {
        var queue = new MatchmakingQueue();
        var player = CreatePlayer();
        queue.Enqueue(player);
        Assert.True(queue.Remove(player));
    }

    [Fact]
    public void Remove_NonExistingPlayer_ReturnsFalse()
    {
        var queue = new MatchmakingQueue();
        var player = CreatePlayer();
        Assert.False(queue.Remove(player));
    }

    [Fact]
    public void Remove_ExistingPlayer_PlayerNoLongerContained()
    {
        var queue = new MatchmakingQueue();
        var player = CreatePlayer();
        queue.Enqueue(player);
        queue.Remove(player);
        Assert.False(queue.Contains(player));
    }

    [Fact]
    public void Remove_MiddlePlayer_OtherPlayersRemain()
    {
        var queue = new MatchmakingQueue();
        var p1 = CreatePlayer("p1");
        var p2 = CreatePlayer("p2");
        var p3 = CreatePlayer("p3");
        queue.Enqueue(p1);
        queue.Enqueue(p2);
        queue.Enqueue(p3);

        queue.Remove(p2);

        Assert.True(queue.Contains(p1));
        Assert.False(queue.Contains(p2));
        Assert.True(queue.Contains(p3));
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void Remove_MiddlePlayer_RemainingPlayersDequeueInOrder()
    {
        var queue = new MatchmakingQueue();
        var p1 = CreatePlayer("p1");
        var p2 = CreatePlayer("p2");
        var p3 = CreatePlayer("p3");
        queue.Enqueue(p1);
        queue.Enqueue(p2);
        queue.Enqueue(p3);

        queue.Remove(p2);

        queue.TryDequeue(out var first);
        queue.TryDequeue(out var second);

        Assert.Equal(p1, first);
        Assert.Equal(p3, second);
    }

    [Fact]
    public void Contains_EmptyQueue_ReturnsFalse()
    {
        var queue = new MatchmakingQueue();
        Assert.False(queue.Contains(CreatePlayer()));
    }

    [Fact]
    public void Contains_EnqueuedPlayer_ReturnsTrue()
    {
        var queue = new MatchmakingQueue();
        var player = CreatePlayer();
        queue.Enqueue(player);
        Assert.True(queue.Contains(player));
    }

    [Fact]
    public void Clear_WithPlayers_AllPlayersRemoved()
    {
        var queue = new MatchmakingQueue();
        var p1 = CreatePlayer("p1");
        var p2 = CreatePlayer("p2");
        queue.Enqueue(p1);
        queue.Enqueue(p2);

        queue.Clear();

        Assert.False(queue.Contains(p1));
        Assert.False(queue.Contains(p2));
    }

    [Fact]
    public void Clear_WithPlayers_TryDequeueReturnsFalse()
    {
        var queue = new MatchmakingQueue();
        queue.Enqueue(CreatePlayer());
        queue.Clear();
        Assert.False(queue.TryDequeue(out _));
    }
}