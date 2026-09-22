using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Authentication;
using NextTech.Application.Modules.Auth;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/internal/auth")]
public sealed class InternalAuthController(InternalAuthService auth) : ControllerBase
{
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public Task<AccessTokenResult> Login(InternalLoginRequest request, CancellationToken ct)
        => auth.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

    [Authorize(Policy = "InternalAuthenticated")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangeInternalPasswordRequest request, CancellationToken ct)
    {
        var raw = User.FindFirstValue("internal_user_id");
        if (!int.TryParse(raw, out var userId)) return Unauthorized();
        await auth.ChangePasswordAsync(userId, request, ct);
        return NoContent();
    }
}
