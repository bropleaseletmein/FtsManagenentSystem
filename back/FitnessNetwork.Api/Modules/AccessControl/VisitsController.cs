using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.AccessControl;

[ApiController]
[Route("visits")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
public class VisitsController(VisitService svc) : ControllerBase
{
    public record EntryRequest(Guid ClubId, Guid ClientSubscriptionId, string EntryMethod);

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVisits(
        [FromQuery] Guid? clubId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await svc.GetVisitsPagedAsync(page, pageSize, clubId, from, to));

    [HttpPost("entry")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordEntry([FromBody] EntryRequest req)
    {
        if (!Enum.TryParse<EntryMethod>(req.EntryMethod, true, out var method))
            return BadRequest(new { error = "Invalid entry method. Use 'card', 'qr', or 'bracelet'." });

        var result = await svc.RecordEntryAsync(req.ClubId, req.ClientSubscriptionId, method);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}/exit")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordExit(Guid id)
    {
        var result = await svc.RecordExitAsync(id);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
