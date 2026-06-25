using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessNetwork.Api.Modules.AccessControl;

[ApiController]
[Route("turnstile")]
[AllowAnonymous]
public class TurnstileController(TurnstileService svc) : ControllerBase
{
    public record ScanRequest(string QrToken, Guid ClubId, string Mode = "entry");

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequest req) =>
        Ok(await svc.ScanAsync(req.QrToken, req.ClubId, req.Mode));
}
