using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Auth;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/internal/auth")]
public sealed class InternalAuthController(
    InternalAuthService auth,
    ICurrentActor actor) : ControllerBase
{
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(InternalSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<InternalSessionResult> Login(InternalLoginRequest request, CancellationToken ct)
        => auth.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

    [Authorize(Policy = "InternalAuthenticated")]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(InternalSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<InternalSessionResult> ChangePassword(
        ChangeInternalPasswordRequest request,
        CancellationToken ct)
        => auth.ChangePasswordAsync(
            actor.RequireIdUsuarioInterno(),
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

    [Authorize(Policy = "InternalAuthenticated")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<InternalUserInfo> Me(CancellationToken ct)
        => auth.GetCurrentUserAsync(actor.RequireIdUsuarioInterno(), ct);
}
