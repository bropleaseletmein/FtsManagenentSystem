using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Chat;

[ApiController]
[Route("chat")]
[Authorize]
public class ChatController(ChatService chatService) : ControllerBase
{
    [HttpGet("{roomId}/history")]
    public async Task<IActionResult> History(string roomId) =>
        Ok(await chatService.GetHistoryAsync(roomId));

    [HttpGet("rooms")]
    public async Task<IActionResult> Rooms() =>
        Ok(await chatService.GetRoomsAsync());
}
