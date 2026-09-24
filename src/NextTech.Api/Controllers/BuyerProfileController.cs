using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common;
using NextTech.Application.Modules.Profile;
using NextTech.Application.Profiles;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Policy = "BuyerOnly")]
public sealed class BuyerProfileController(BuyerProfileService profile) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(BuyerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuyerProfileView>> GetMe(CancellationToken ct)
        => Ok(await profile.GetAsync(GetBuyerId(), ct));

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
        => Ok(await profile.UpdateAsync(GetBuyerId(), request, ct));

    [HttpPost("change-password")]
    [EnableRateLimiting("profile-sensitive")]
    [ProducesResponseType(typeof(BuyerProfileMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BuyerProfileMutationResult>> ChangePassword(
        [FromBody] ChangeBuyerPasswordRequest request,
        CancellationToken ct)
        => Ok(await profile.ChangePasswordAsync(GetBuyerId(), request, ct));

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
        var photo = await profile.GetDisplayPhotoAsync(GetBuyerId(), ct);
        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["Pragma"] = "no-cache";
        return File(photo.Content, photo.ContentType);
    }

    private long GetBuyerId()
    {
        var value = User.FindFirstValue("buyer_id");
        return long.TryParse(value, out var id) && id > 0
            ? id
            : throw new AppUnauthorizedException();
    }
}
