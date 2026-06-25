using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Clubs;

[ApiController]
[Route("clubs")]
[Authorize]
public class ClubsController(ClubService svc) : ControllerBase
{
    public record CreateClubRequest(string Name, string Address, string? Phone);
    public record UpdateClubRequest(string Name, string Address, string? Phone);
    public record CreateHallRequest(string Name, int Capacity);
    public record UpdateHallRequest(string Name, int Capacity);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() =>
        Ok(await svc.GetAllClubsAsync());

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var club = await svc.GetClubByIdAsync(id);
        return club is null ? NotFound() : Ok(club);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateClubRequest req)
    {
        var club = await svc.CreateClubAsync(req.Name, req.Address, req.Phone);
        return CreatedAtAction(nameof(GetById), new { id = club.Id }, club);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClubRequest req)
    {
        var result = await svc.UpdateClubAsync(id, req.Name, req.Address, req.Phone);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await svc.DeleteClubAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    // --- Halls ---

    [HttpGet("{clubId:guid}/halls")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHalls(Guid clubId) =>
        Ok(await svc.GetHallsByClubAsync(clubId));

    [HttpPost("{clubId:guid}/halls")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateHall(Guid clubId, [FromBody] CreateHallRequest req)
    {
        var result = await svc.CreateHallAsync(clubId, req.Name, req.Capacity);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("halls/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateHall(Guid id, [FromBody] UpdateHallRequest req)
    {
        var result = await svc.UpdateHallAsync(id, req.Name, req.Capacity);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("halls/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteHall(Guid id)
    {
        var result = await svc.DeleteHallAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
