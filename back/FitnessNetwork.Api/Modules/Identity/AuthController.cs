using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.Identity;

[ApiController]
[Route("auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    public record LoginRequest(string Email, string Password);
    public record TokenResponse(string Token);

    [HttpPost("staff/login")]
    public async Task<IActionResult> StaffLogin([FromBody] LoginRequest req)
    {
        var result = await authService.LoginStaffAsync(req.Email, req.Password);
        if (!result.IsSuccess) return Unauthorized(new { error = result.Error });
        return Ok(new TokenResponse(result.Value!));
    }

    [HttpPost("client/login")]
    public async Task<IActionResult> ClientLogin([FromBody] LoginRequest req)
    {
        var result = await authService.LoginClientAsync(req.Email, req.Password);
        if (!result.IsSuccess) return Unauthorized(new { error = result.Error });
        return Ok(new TokenResponse(result.Value!));
    }
}
