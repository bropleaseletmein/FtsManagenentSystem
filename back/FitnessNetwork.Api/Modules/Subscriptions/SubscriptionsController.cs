using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Subscriptions;

[ApiController]
[Route("subscriptions")]
[Authorize(Roles = Roles.Admin)]
public class SubscriptionsController(SubscriptionService svc) : ControllerBase
{
    public record FreezeRequest(DateOnly StartedAt);
    public record ChangeStatusRequest(string Status);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sub = await svc.GetByIdAsync(id);
        return sub is null ? NotFound() : Ok(sub);
    }

    [HttpPost("{id:guid}/freeze")]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] FreezeRequest req)
    {
        var result = await svc.FreezeAsync(id, req.StartedAt);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/unfreeze")]
    public async Task<IActionResult> Unfreeze(Guid id)
    {
        var result = await svc.UnfreezeAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest req)
    {
        if (!Enum.TryParse<SubscriptionStatus>(req.Status, true, out var status))
            return BadRequest(new { error = "Invalid status." });

        var result = await svc.ChangeStatusAsync(id, status);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}

[ApiController]
[Route("subscription-types")]
[Authorize]
public class SubscriptionTypesController(SubscriptionTypeService svc) : ControllerBase
{
    public record CreateTypeRequest(
        string Name, int? DurationDays, int? VisitsLimit, decimal Price,
        bool IsAllClubs, List<Guid>? ClubIds);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await svc.GetAllAsync());

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var st = await svc.GetByIdAsync(id);
        return st is null ? NotFound() : Ok(st);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateTypeRequest req)
    {
        var result = await svc.CreateAsync(req.Name, req.DurationDays, req.VisitsLimit, req.Price, req.IsAllClubs, req.ClubIds);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateTypeRequest req)
    {
        var result = await svc.UpdateAsync(id, req.Name, req.DurationDays, req.VisitsLimit, req.Price, req.IsAllClubs, req.ClubIds);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await svc.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
