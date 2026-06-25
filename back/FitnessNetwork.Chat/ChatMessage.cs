using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FitnessNetwork.Chat;

public class ChatMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string RoomId { get; set; } = null!;
    public string SenderId { get; set; } = null!;
    public string SenderName { get; set; } = null!;
    public string SenderRole { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime SentAt { get; set; }
}
