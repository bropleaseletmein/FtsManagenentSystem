using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FitnessNetwork.Chat;

[Authorize]
public class ChatHub(ChatService chatService) : Hub
{
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
    }

    public async Task SendMessage(string roomId, string text, string senderName)
    {
        var user = Context.User!;
        var msg = new ChatMessage
        {
            RoomId     = roomId,
            SenderId   = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
            SenderName = senderName,
            SenderRole = user.FindFirstValue(ClaimTypes.Role) ?? "client",
            Text       = text,
            SentAt     = DateTime.UtcNow,
        };
        await chatService.SaveAsync(msg);
        await Clients.Group(roomId).SendAsync("ReceiveMessage", msg);
    }
}
