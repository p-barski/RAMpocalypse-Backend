using RAMpocalypse.Server.Database;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class DbCacheTests
{
    private readonly DbCache cache;
    public DbCacheTests()
    {
        cache = new DbCache();
    }

    [Fact]
    public void SaveChatMessage_SingleMessage_CanBeRetrieved()
    {
        cache.SaveChatMessage(new ChatMessage());
        var result = cache.GetGlobalChatMessagesHistory(10);
        Assert.Single(result);
    }

    [Fact]
    public void SaveChatMessage_ExceedsMaxCount_OldestMessageRemoved()
    {
        ChatMessage first = new();
        int count = IDatabase.CHAT_HISTORY_MAX_COUNT + 1;
        cache.SaveChatMessage(first);
        for (int i = 1; i < count; i++)
            cache.SaveChatMessage(new ChatMessage());

        var result = cache.GetGlobalChatMessagesHistory(count);
        Assert.DoesNotContain(first, result);
    }

    [Fact]
    public void GetGlobalChatMessagesHistory_EmptyCache_ReturnsEmptyArray()
    {
        var result = cache.GetGlobalChatMessagesHistory(10);
        Assert.Empty(result);
    }

    [Fact]
    public void GetGlobalChatMessagesHistory_CountClamped_WhenBelowOne()
    {
        cache.SaveChatMessage(new ChatMessage());
        cache.SaveChatMessage(new ChatMessage());
        var result = cache.GetGlobalChatMessagesHistory(0);
        Assert.Single(result);
    }

    [Fact]
    public void GetGlobalChatMessagesHistory_CountClamped_WhenAboveMax()
    {
        int count = IDatabase.CHAT_HISTORY_MAX_COUNT + 100;
        for (int i = 0; i < count; i++)
            cache.SaveChatMessage(new ChatMessage());

        var result = cache.GetGlobalChatMessagesHistory(count);
        Assert.Equal(IDatabase.CHAT_HISTORY_MAX_COUNT, result.Length);
    }

    [Fact]
    public void GetGlobalChatMessagesHistory_ReturnsCorrectCount()
    {
        for (int i = 0; i < 10; i++)
            cache.SaveChatMessage(new ChatMessage());

        var result = cache.GetGlobalChatMessagesHistory(3);
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void GetGlobalChatMessagesHistory_ReturnsLatestMessages()
    {
        ChatMessage first = new();
        ChatMessage last = new();
        cache.SaveChatMessage(first);
        for (int i = 0; i < 5; i++)
            cache.SaveChatMessage(new ChatMessage());
        cache.SaveChatMessage(last);

        var result = cache.GetGlobalChatMessagesHistory(3);
        Assert.Contains(last, result);
        Assert.DoesNotContain(first, result);
    }

    [Fact]
    public void FillCache_PopulatesMessages()
    {
        List<ChatMessage> messages = [new(), new(), new()];
        cache.FillCache(messages);
        var result = cache.GetGlobalChatMessagesHistory(10);
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void FillCache_ExceedsMaxCount_ClampsToMax()
    {
        int count = IDatabase.CHAT_HISTORY_MAX_COUNT + 1;
        ChatMessage first = new();
        List<ChatMessage> messages = new(count) { first };
        for (int i = 1; i < count; i++)
            messages.Add(new ChatMessage());

        cache.FillCache(messages);
        var result = cache.GetGlobalChatMessagesHistory(IDatabase.CHAT_HISTORY_MAX_COUNT);
        Assert.Equal(IDatabase.CHAT_HISTORY_MAX_COUNT, result.Length);
        Assert.DoesNotContain(first, result);
    }

    [Fact]
    public void FillCache_ThenSaveMessage_MessageAddedCorrectly()
    {
        cache.FillCache([new(), new()]);
        ChatMessage newMessage = new();
        cache.SaveChatMessage(newMessage);
        var result = cache.GetGlobalChatMessagesHistory(10);

        Assert.Equal(3, result.Length);
        Assert.Contains(newMessage, result);
    }

    [Fact]
    public void FillCache_CalledTwice_ReplacesExistingMessages()
    {
        ChatMessage first = new();
        cache.FillCache([first, new()]);
        List<ChatMessage> newMessages = [new()];
        cache.FillCache(newMessages);
        var result = cache.GetGlobalChatMessagesHistory(10);

        Assert.Single(result);
        Assert.DoesNotContain(first, result);
    }
}