using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Profile;
using NextTech.Application.Profiles;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Policy = "BuyerOnly")]
public sealed class BuyerProfileController(
    BuyerProfileService profile,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(BuyerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuyerProfileView>> GetMe(CancellationToken ct)
        => Ok(await profile.GetAsync(actor.RequireIdCompradorExterno(), ct));

    [HttpPut("me")]
    [EnableRateLimiting("profile")]
    [ProducesResponseType(typeof(BuyerProfileMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BuyerProfileMutationResult>> UpdateMe(
        [FromBody] UpdateBuyerProfileRequest request,
        CancellationToken ct)
        => Ok(await profile.UpdateAsync(actor.RequireIdCompradorExterno(), request, ct));

    [HttpPost("change-password")]
    [EnableRateLimiting("profile-sensitive")]
    [ProducesResponseType(typeof(BuyerProfileMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BuyerProfileMutationResult>> ChangePassword(
        [FromBody] ChangeBuyerPasswordRequest request,
        CancellationToken ct)
        => Ok(await profile.ChangePasswordAsync(actor.RequireIdCompradorExterno(), request, ct));

    [HttpGet("notification-options")]
    [ProducesResponseType(typeof(IReadOnlyList<BuyerNotificationPreferenceOption>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BuyerNotificationPreferenceOption>> GetNotificationOptions()
        => Ok(profile.GetNotificationOptions());

    [HttpGet("photo")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisplayPhoto(CancellationToken ct)
    {
        var photo = await profile.GetDisplayPhotoAsync(actor.RequireIdCompradorExterno(), ct);
        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["Pragma"] = "no-cache";
        return File(photo.Content, photo.ContentType);
    }
}
