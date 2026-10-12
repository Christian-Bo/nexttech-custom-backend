using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Face;
using NextTech.Application.Modules.Onboarding;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize(Policy = "BuyerOnly")]
public sealed class BuyerOnboardingController(
    BuyerOnboardingService onboarding,
    ICurrentActor actor) : ControllerBase
{
    [EnableRateLimiting("biometric")]
    [HttpPost("complete")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(FaceEnrollmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FaceEnrollmentResult>> Complete(
        [FromForm] CompleteBuyerOnboardingFormRequest request,
        CancellationToken ct)
    {
        var neutral = await FaceRequestMapper.ReadImageAsync(
            request.NeutralImage,
            "neutralImage",
            ct);
        var challenge = await FaceRequestMapper.ReadImageAsync(
            request.ChallengeImage,
            "challengeImage",
            ct);

        var result = await onboarding.CompleteAsync(
            actor.RequireIdCompradorExterno(),
            request.ChallengeId,
            neutral,
            challenge,
            ct);

        return Ok(result);
    }
}

public sealed class CompleteBuyerOnboardingFormRequest
{
    public string ChallengeId { get; init; } = string.Empty;
    public IFormFile? NeutralImage { get; init; }
    public IFormFile? ChallengeImage { get; init; }
}
