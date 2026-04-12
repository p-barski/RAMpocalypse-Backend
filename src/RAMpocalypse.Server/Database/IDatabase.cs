using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Server.Database;

public interface IDatabase
{
    const int CHAT_HISTORY_MAX_COUNT = 200;
    Task SaveLobbyResult(LobbyResult lobbyResult);
    Task SaveChatMessage(ChatMessage message);
    Task<ChatMessage[]> GetGlobalChatMessagesHistory(int count);
}
