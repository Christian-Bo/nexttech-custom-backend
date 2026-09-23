using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common;
using NextTech.Application.Modules.Credentials;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/credential")]
[Authorize(Policy = "BuyerOnly")]
[EnableRateLimiting("credential")]
public sealed class BuyerCredentialController(BuyerCredentialService credentials) : ControllerBase
{
    [HttpPost("issue")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Issue(CancellationToken ct)
    {
        var document = await credentials.IssueAsync(GetBuyerId(), ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return File(document.Content, document.ContentType, document.FileName);
    }

    private long GetBuyerId()
    {
        var value = User.FindFirstValue("buyer_id");
        return long.TryParse(value, out var id) && id > 0
            ? id
            : throw new AppUnauthorizedException();
    }
}
