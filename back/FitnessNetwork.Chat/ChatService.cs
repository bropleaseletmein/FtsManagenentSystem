using MongoDB.Driver;

namespace FitnessNetwork.Chat;

public class ChatService
{
    private readonly IMongoCollection<ChatMessage> _messages;

    public ChatService(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("Mongo"));
        var db = client.GetDatabase("fitness_chat");
        _messages = db.GetCollection<ChatMessage>("messages");
        _messages.Indexes.CreateOne(
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys.Ascending(m => m.RoomId).Ascending(m => m.SentAt)));
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(string roomId, int limit = 50)
    {
        return await _messages
            .Find(m => m.RoomId == roomId)
            .SortByDescending(m => m.SentAt)
            .Limit(limit)
            .ToListAsync()
            .ContinueWith(t => { t.Result.Reverse(); return t.Result; });
    }

    public async Task<List<string>> GetRoomsAsync()
    {
        return await _messages
            .Distinct<string>("RoomId", Builders<ChatMessage>.Filter.Empty)
            .ToListAsync();
    }

    public Task SaveAsync(ChatMessage msg) => _messages.InsertOneAsync(msg);
}
