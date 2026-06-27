using System.Security.Claims;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Scheduling;

[ApiController]
[Route("class-types")]
[Authorize]
public class ClassTypesController(SchedulingService svc) : ControllerBase
{
    public record ClassTypeRequest(string Name, string? Description);

    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await svc.GetAllClassTypesPagedAsync(page, pageSize));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] ClassTypeRequest req)
    {
        var result = await svc.CreateClassTypeAsync(req.Name, req.Description);
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClassTypeRequest req)
    {
        var result = await svc.UpdateClassTypeAsync(id, req.Name, req.Description);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await svc.DeleteClassTypeAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}

[ApiController]
[Route("schedule")]
[Authorize]
public class ScheduleController(SchedulingService svc) : ControllerBase
{
    public record CreateScheduleRequest(
        Guid ClassTypeId, Guid HallId, Guid TrainerId,
        DateTime StartsAt, DateTime EndsAt, int Capacity);

    public record UpdateStatusRequest(string Status);

    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(
        [FromQuery] Guid? clubId,
        [FromQuery] Guid? trainerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await svc.GetSchedulePagedAsync(page, pageSize, clubId, trainerId, from, to));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await svc.GetScheduleItemAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest req)
    {
        var result = await svc.CreateScheduleAsync(
            req.ClassTypeId, req.HallId, req.TrainerId, req.StartsAt, req.EndsAt, req.Capacity);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateScheduleRequest req)
    {
        var result = await svc.UpdateScheduleAsync(
            id, req.ClassTypeId, req.HallId, req.TrainerId, req.StartsAt, req.EndsAt, req.Capacity);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
    {
        if (!Enum.TryParse<ClassStatus>(req.Status, true, out var status))
            return BadRequest(new { error = "Invalid status. Use 'scheduled', 'cancelled', or 'completed'." });

        var result = await svc.UpdateScheduleStatusAsync(id, status);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingsController(SchedulingService svc) : ControllerBase
{
    public record BookRequest(Guid ClientSubscriptionId, Guid ClassScheduleId);

    [HttpGet("my")]
    [Authorize(Roles = Roles.Client)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBookings()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(idStr, out var clientId))
            return Unauthorized();
        var bookings = await svc.GetBookingsByClientAsync(clientId);
        return Ok(bookings);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Client}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Book([FromBody] BookRequest req)
    {
        var result = await svc.BookAsync(req.ClientSubscriptionId, req.ClassScheduleId);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Client}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await svc.CancelBookingAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
