using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Server.Database;

public class DbCache
{
    private readonly Lock cacheLock = new();
    private LinkedList<ChatMessage> cachedGlobalMessages = [];
    public void SaveChatMessage(ChatMessage message)
    {
        lock (cacheLock)
        {
            cachedGlobalMessages.AddLast(message);
            if (cachedGlobalMessages.Count > IDatabase.CHAT_HISTORY_MAX_COUNT)
                cachedGlobalMessages.RemoveFirst();
        }
    }
    public ChatMessage[] GetGlobalChatMessagesHistory(int count)
    {
        count = Math.Clamp(count, 1, IDatabase.CHAT_HISTORY_MAX_COUNT);
        ChatMessage[] msgs;
        lock (cachedGlobalMessages)
        {
            msgs = cachedGlobalMessages.TakeLast(count).ToArray();
        }
        return msgs;
    }
    public void FillCache(List<ChatMessage> messages)
    {
        LinkedList<ChatMessage> msgs = new(messages.TakeLast(IDatabase.CHAT_HISTORY_MAX_COUNT));
        lock (cacheLock)
        {
            cachedGlobalMessages = msgs;
        }
    }
}
