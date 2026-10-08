using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Notifications;
using NextTech.Application.Notifications;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "BuyerOnly")]
public sealed class BuyerNotificationsController(
    BuyerNotificationService notifications,
    ICurrentActor actor) : ControllerBase
{
    [HttpPost("welcome/resend")]
    [EnableRateLimiting("profile-sensitive")]
    [ProducesResponseType(typeof(NotificationDispatchResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<NotificationDispatchResult>> ResendWelcome(
        [FromBody] NotificationDeliveryRequest request,
        CancellationToken ct)
    {
        var result = await notifications.ResendWelcomeAsync(
            actor.RequireIdCompradorExterno(),
            request,
            ct);

        return Accepted(result);
    }
}
