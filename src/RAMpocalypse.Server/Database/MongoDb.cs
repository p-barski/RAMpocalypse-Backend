using MongoDB.Driver;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Server.Database;

public class MongoDb : IDatabase
{
    private readonly MongoClient client;
    private readonly IMongoDatabase database;
    private readonly IMongoCollection<LobbyResult> lobbyResults;
    private readonly IMongoCollection<ChatMessage> chatMessages;
    private readonly Lock cacheLock = new();
    private LinkedList<ChatMessage> cachedGlobalMessages = [];
    public MongoDb(MongoDbConfig config)
    {
        client = new MongoClient(config.ConnectionString);
        database = client.GetDatabase(config.DatabaseName);
        lobbyResults = database.GetCollection<LobbyResult>("LobbyResults");
        chatMessages = database.GetCollection<ChatMessage>("ChatMessages");

        var indexModel = new CreateIndexModel<ChatMessage>(Builders<ChatMessage>.IndexKeys.Descending(m => m.Timestamp));
        chatMessages.Indexes.CreateOne(indexModel);
    }
    public async Task SaveLobbyResult(LobbyResult lobbyResult)
    {
        await lobbyResults.InsertOneAsync(lobbyResult);
    }
    public async Task SaveTextMessage(ChatMessage message)
    {
        lock (cacheLock)
        {
            cachedGlobalMessages.AddLast(message);
            if (cachedGlobalMessages.Count > IDatabase.CHAT_HISTORY_MAX_COUNT)
                cachedGlobalMessages.RemoveFirst();
        }
        await chatMessages.InsertOneAsync(message);
    }
    public async Task FillGlobalChatHistoryCache()
    {
        var messages = await chatMessages
            .Find(Builders<ChatMessage>.Filter.Empty)
            .Sort(Builders<ChatMessage>.Sort.Descending(m => m.Timestamp))
            .Limit(IDatabase.CHAT_HISTORY_MAX_COUNT)
            .Sort(Builders<ChatMessage>.Sort.Ascending(m => m.Timestamp))
            .ToListAsync();
        LinkedList<ChatMessage> msgs = new(messages);
        lock (cacheLock)
        {
            cachedGlobalMessages = msgs;
        }
    }
    public Task<ChatMessage[]> GetGlobalChatMessagesHistory(int count)
    {
        count = Math.Clamp(count, 1, IDatabase.CHAT_HISTORY_MAX_COUNT);
        ChatMessage[] msgs;
        lock (cachedGlobalMessages)
        {
            msgs = cachedGlobalMessages.Take(count).ToArray();
        }
        return Task.FromResult(msgs);
    }
}
