using System.Security.Claims;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Staff;

[ApiController]
[Route("staff")]
[Authorize(Roles = Roles.Admin)]
public class StaffController(StaffService svc) : ControllerBase
{
    public record CreateStaffRequest(
        Guid ClubId, string FirstName, string LastName, string Email,
        List<string> Roles, string? Password);

    public record UpdateStaffRequest(string FirstName, string LastName, string Email);
    public record SetCredentialsRequest(string Email, string Password);
    public record AddRoleRequest(string Role);

    [HttpGet("me")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    public async Task<IActionResult> GetMe()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(idStr, out var staffId)) return Unauthorized();
        var staff = await svc.GetByIdAsync(staffId);
        return staff is null ? NotFound() : Ok(staff);
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    public async Task<IActionResult> GetAll() => Ok(await svc.GetAllAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var staff = await svc.GetByIdAsync(id);
        return staff is null ? NotFound() : Ok(staff);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest req)
    {
        var roles = req.Roles
            .Select(r => Enum.TryParse<StaffRoleType>(r, true, out var val) ? val : (StaffRoleType?)null)
            .Where(r => r is not null)
            .Select(r => r!.Value);

        var result = await svc.CreateAsync(req.ClubId, req.FirstName, req.LastName, req.Email, roles, req.Password);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffRequest req)
    {
        var result = await svc.UpdateAsync(id, req.FirstName, req.LastName, req.Email);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await svc.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AddRole(Guid id, [FromBody] AddRoleRequest req)
    {
        if (!Enum.TryParse<StaffRoleType>(req.Role, true, out var role))
            return BadRequest(new { error = "Invalid role. Use 'admin' or 'trainer'." });

        var result = await svc.AddRoleAsync(id, role);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(Guid id, string role)
    {
        if (!Enum.TryParse<StaffRoleType>(role, true, out var roleType))
            return BadRequest(new { error = "Invalid role." });

        var result = await svc.RemoveRoleAsync(id, roleType);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/credentials")]
    public async Task<IActionResult> SetCredentials(Guid id, [FromBody] SetCredentialsRequest req)
    {
        var result = await svc.SetCredentialsAsync(id, req.Email, req.Password);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
