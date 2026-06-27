using System.Security.Claims;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.AccessControl;
using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Subscriptions;

[ApiController]
[Route("clients")]
[Authorize]
public class ClientsController(ClientService clientSvc, SubscriptionService subSvc, VisitService visitSvc, JwtService jwtSvc) : ControllerBase
{
    public record CreateClientRequest(
        string FirstName, string LastName, string? Email, string? Phone,
        DateOnly? BirthDate, string? Password);

    public record UpdateClientRequest(
        string FirstName, string LastName, string? Email, string? Phone, DateOnly? BirthDate);

    public record SellSubscriptionRequest(Guid SubscriptionTypeId);
    public record SetClientCredentialsRequest(string Email, string Password);

    [HttpGet("me")]
    [Authorize(Roles = Roles.Client)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(idStr, out var clientId))
            return Unauthorized();
        var client = await clientSvc.GetByIdAsync(clientId);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await clientSvc.GetAllPagedAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await clientSvc.GetByIdAsync(id);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest req)
    {
        var result = await clientSvc.CreateAsync(
            req.FirstName, req.LastName, req.Email, req.Phone, req.BirthDate, req.Password);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Client}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest req)
    {
        var result = await clientSvc.UpdateAsync(id, req.FirstName, req.LastName, req.Email, req.Phone, req.BirthDate);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await clientSvc.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/credentials")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetCredentials(Guid id, [FromBody] SetClientCredentialsRequest req)
    {
        var result = await clientSvc.SetCredentialsAsync(id, req.Email, req.Password);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpGet("me/qr-token")]
    [Authorize(Roles = Roles.Client)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetQrToken()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(idStr, out var clientId)) return Unauthorized();
        return Ok(new { token = jwtSvc.GenerateQrToken(clientId) });
    }

    [HttpGet("me/visits")]
    [Authorize(Roles = Roles.Client)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyVisits()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(idStr, out var clientId)) return Unauthorized();
        return Ok(await visitSvc.GetVisitsByClientAsync(clientId));
    }

    [HttpGet("{id:guid}/subscriptions")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer},{Roles.Client}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSubscriptions(Guid id) =>
        Ok(await subSvc.GetByClientAsync(id));

    [HttpPost("{id:guid}/subscriptions")]
    [Authorize(Roles = Roles.Admin)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SellSubscription(Guid id, [FromBody] SellSubscriptionRequest req)
    {
        var result = await subSvc.SellAsync(id, req.SubscriptionTypeId);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }
}
