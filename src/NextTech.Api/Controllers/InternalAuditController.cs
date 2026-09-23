using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Modules.Auth;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/internal/audit")]
public sealed class InternalAuditController(InternalSecurityAuditService audit) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InternalAuditEntryInfo>), StatusCodes.Status200OK)]
    public Task<PagedResult<InternalAuditEntryInfo>> Search(
        [FromQuery] int? internalUserId,
        [FromQuery] string? action,
        [FromQuery] string? result,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => audit.SearchAsync(
            new InternalAuditListRequest(
                internalUserId,
                action,
                result,
                fromUtc,
                toUtc,
                page,
                pageSize),
            ct);
}
