using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Modules.Dashboard;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalOnly")]
[Authorize(Roles = $"{InternalRoles.Admin},{InternalRoles.Supervisor}")]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<IActionResult> Ventas(
        [FromQuery] string? range,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboard.ObtenerVentasAsync(range, cancellationToken));
    }
}
