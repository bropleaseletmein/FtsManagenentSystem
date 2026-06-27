using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Reporting;

[ApiController]
[Route("reporting")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
public class ReportingController(ReportingService svc) : ControllerBase
{
    [HttpGet("attendance")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? clubId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to) =>
        Ok(await svc.GetAttendanceAsync(clubId, from, to));

    [HttpGet("trainers/workload")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTrainerWorkload(
        [FromQuery] Guid? clubId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to) =>
        Ok(await svc.GetTrainerWorkloadAsync(clubId, from, to));

    [HttpGet("classes/occupancy")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetClassOccupancy(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to) =>
        Ok(await svc.GetClassOccupancyAsync(from, to));

    [HttpGet("current-occupancy")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentOccupancy() =>
        Ok(await svc.GetCurrentOccupancyAsync());
}
