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
    public async Task<IActionResult> GetVisits(
        [FromQuery] Guid? clubId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to) =>
        Ok(await svc.GetVisitsAsync(clubId, from, to));

    [HttpPost("entry")]
    public async Task<IActionResult> RecordEntry([FromBody] EntryRequest req)
    {
        if (!Enum.TryParse<EntryMethod>(req.EntryMethod, true, out var method))
            return BadRequest(new { error = "Invalid entry method. Use 'card', 'qr', or 'bracelet'." });

        var result = await svc.RecordEntryAsync(req.ClubId, req.ClientSubscriptionId, method);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}/exit")]
    public async Task<IActionResult> RecordExit(Guid id)
    {
        var result = await svc.RecordExitAsync(id);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
