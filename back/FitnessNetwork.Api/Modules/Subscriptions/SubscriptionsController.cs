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
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sub = await svc.GetByIdAsync(id);
        return sub is null ? NotFound() : Ok(sub);
    }

    [HttpPost("{id:guid}/freeze")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] FreezeRequest req)
    {
        var result = await svc.FreezeAsync(id, req.StartedAt);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/unfreeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unfreeze(Guid id)
    {
        var result = await svc.UnfreezeAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await svc.GetAllPagedAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var st = await svc.GetByIdAsync(id);
        return st is null ? NotFound() : Ok(st);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTypeRequest req)
    {
        var result = await svc.CreateAsync(req.Name, req.DurationDays, req.VisitsLimit, req.Price, req.IsAllClubs, req.ClubIds);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateTypeRequest req)
    {
        var result = await svc.UpdateAsync(id, req.Name, req.DurationDays, req.VisitsLimit, req.Price, req.IsAllClubs, req.ClubIds);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await svc.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
