using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Modules.Auth;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/internal/roles")]
public sealed class InternalRolesController(InternalUserAdministrationService users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InternalRoleInfo>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<InternalRoleInfo>> GetAll(CancellationToken ct)
        => users.GetRolesAsync(ct);
}
