using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Auth;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/internal/users")]
public sealed class InternalUsersController(
    InternalUserAdministrationService users,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InternalUserInfo>), StatusCodes.Status200OK)]
    public Task<PagedResult<InternalUserInfo>> Search(
        [FromQuery] string? search,
        [FromQuery] string? roleCode,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLocked,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => users.SearchAsync(
            new InternalUserListRequest(search, roleCode, isActive, isLocked, page, pageSize),
            ct);

    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InternalUserInfo> GetById(int userId, CancellationToken ct)
        => users.GetByIdAsync(userId, ct);

    [HttpPost]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateInternalUserRequest request, CancellationToken ct)
    {
        var created = await users.CreateAsync(
            request,
            actor.RequireIdUsuarioInterno(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        return CreatedAtAction(nameof(GetById), new { userId = created.Id }, created);
    }

    [HttpPut("{userId:int}")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<InternalUserInfo> Update(
        int userId,
        UpdateInternalUserRequest request,
        CancellationToken ct)
        => users.UpdateAsync(
            userId,
            request,
            actor.RequireIdUsuarioInterno(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

    [HttpDelete("{userId:int}")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<InternalUserInfo> Deactivate(int userId, CancellationToken ct)
        => users.DeactivateAsync(
            userId,
            actor.RequireIdUsuarioInterno(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

    [HttpPost("{userId:int}/activate")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InternalUserInfo> Activate(int userId, CancellationToken ct)
        => users.ActivateAsync(
            userId,
            actor.RequireIdUsuarioInterno(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

    [HttpPost("{userId:int}/unlock")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InternalUserInfo> Unlock(int userId, CancellationToken ct)
        => users.UnlockAsync(
            userId,
            actor.RequireIdUsuarioInterno(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

    [HttpPost("{userId:int}/reset-password")]
    [ProducesResponseType(typeof(InternalUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InternalUserInfo> ResetPassword(
        int userId,
        ResetInternalPasswordRequest request,
        CancellationToken ct)
        => users.ResetPasswordAsync(
            userId,
            request,
            actor.RequireIdUsuarioInterno(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);
}
