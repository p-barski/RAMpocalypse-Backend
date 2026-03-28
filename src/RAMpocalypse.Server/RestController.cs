using Microsoft.AspNetCore.Mvc;
using RAMpocalypse.Server.Database;

namespace RAMpocalypse.Server;

[ApiController]
[Route("api")]
public class RestController(IDatabase database) : ControllerBase
{
    private readonly IDatabase database = database;

    [HttpGet("globalChatHistory")]
    public async Task<IActionResult> GetGlobalChatHistory([FromQuery] int count = IDatabase.CHAT_HISTORY_MAX_COUNT)
    {
        var messages = await database.GetGlobalChatMessagesHistory(count);
        return Ok(messages);
    }
}
