using MongoDB.Driver;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Server.Database;

public class MongoDb : IDatabase
{
    private readonly ILogger<MongoDb> logger;
    private readonly MongoClient client;
    private readonly IMongoDatabase database;
    private readonly IMongoCollection<LobbyResult> lobbyResults;
    private readonly IMongoCollection<ChatMessage> chatMessages;
    private readonly DbCache cache = new();
    public MongoDb(MongoDbConfig config, ILogger<MongoDb> logger)
    {
        this.logger = logger;
        var settings = MongoClientSettings.FromConnectionString(config.ConnectionString);
        settings.ConnectTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = settings.ConnectTimeout;
        settings.ServerSelectionTimeout = settings.ConnectTimeout;
        client = new MongoClient(settings);
        database = client.GetDatabase(config.DatabaseName);
        lobbyResults = database.GetCollection<LobbyResult>("LobbyResults");
        chatMessages = database.GetCollection<ChatMessage>("ChatMessages");

        var indexModel = new CreateIndexModel<ChatMessage>(Builders<ChatMessage>.IndexKeys.Descending(m => m.Timestamp));
        chatMessages.Indexes.CreateOne(indexModel);
    }
    public async Task SaveLobbyResult(LobbyResult lobbyResult)
    {
        try
        {
            await lobbyResults.InsertOneAsync(lobbyResult);
        }
        catch (Exception e)
        {
            logger.LogWarning("Could not save lobby result in mongodb - {Exception}", e);
        }
    }
    public async Task SaveChatMessage(ChatMessage message)
    {
        cache.SaveChatMessage(message);
        try
        {
            await chatMessages.InsertOneAsync(message);
        }
        catch (Exception e)
        {
            logger.LogWarning("Could not save chat message in mongodb - {Exception}", e);
        }
    }
    public async Task FillGlobalChatHistoryCache()
    {
        List<ChatMessage> messages;
        try
        {
            messages = await chatMessages
                .Find(Builders<ChatMessage>.Filter.Empty)
                .Sort(Builders<ChatMessage>.Sort.Descending(m => m.Timestamp))
                .Limit(IDatabase.CHAT_HISTORY_MAX_COUNT)
                .Sort(Builders<ChatMessage>.Sort.Ascending(m => m.Timestamp))
                .ToListAsync();
        }
        catch (Exception e)
        {
            logger.LogWarning("Could not load global chat history from mongodb - {Exception}", e);
            return;
        }
        cache.FillCache(messages);
    }
    public Task<ChatMessage[]> GetGlobalChatMessagesHistory(int count)
    {
        return Task.FromResult(cache.GetGlobalChatMessagesHistory(count));
    }
}
