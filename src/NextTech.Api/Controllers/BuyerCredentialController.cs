using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Credentials;
using NextTech.Application.Notifications;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/credential")]
[Authorize(Policy = "BuyerOnly")]
[EnableRateLimiting("credential")]
public sealed class BuyerCredentialController(
    BuyerCredentialService credentials,
    ICurrentActor actor) : ControllerBase
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
        var document = await credentials.IssueAsync(actor.RequireIdCompradorExterno(), ct);
        SetNoStoreHeaders();
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPost("reissue")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Reissue(
        [FromBody] NotificationDeliveryRequest request,
        CancellationToken ct)
    {
        var document = await credentials.ReissueAsync(
            actor.RequireIdCompradorExterno(),
            request,
            ct);

        SetNoStoreHeaders();
        return File(document.Content, document.ContentType, document.FileName);
    }

    private void SetNoStoreHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }
}
