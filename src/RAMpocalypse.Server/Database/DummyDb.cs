using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Server.Database;

public class DummyDb : IDatabase
{
    private readonly DbCache cache = new();
    public Task SaveLobbyResult(LobbyResult lobbyResult)
    {
        return Task.CompletedTask;
    }
    public Task SaveChatMessage(ChatMessage message)
    {
        cache.SaveChatMessage(message);
        return Task.CompletedTask;
    }
    public Task<ChatMessage[]> GetGlobalChatMessagesHistory(int count)
    {
        return Task.FromResult(cache.GetGlobalChatMessagesHistory(count));
    }
}
