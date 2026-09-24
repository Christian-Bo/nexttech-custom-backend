using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Face;
using NextTech.Application.Modules.Face;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/face")]
[Authorize(Policy = "BuyerOnly")]
[EnableRateLimiting("biometric")]
public sealed class FaceController(
    FaceApplicationService face,
    ICurrentActor actor) : ControllerBase
{
    [HttpPost("challenge")]
    [ProducesResponseType(typeof(FaceChallengeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<FaceChallengeResult> Challenge(CancellationToken ct)
        => face.CreateChallengeAsync(actor.RequireIdCompradorExterno(), ct);

    [HttpPost("enroll")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(FaceEnrollmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FaceEnrollmentResult>> Enroll(
        [FromForm] FaceChallengeImagesFormRequest request,
        CancellationToken ct)
    {
        var neutral = await FaceRequestMapper.ReadImageAsync(request.NeutralImage, "neutralImage", ct);
        var challenge = await FaceRequestMapper.ReadImageAsync(request.ChallengeImage, "challengeImage", ct);

        var result = await face.EnrollBuyerAsync(
            actor.RequireIdCompradorExterno(),
            request.ChallengeId,
            neutral,
            challenge,
            ct);

        return Ok(result);
    }

    [HttpPost("verify")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(FaceVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FaceVerificationResult>> Verify(
        [FromForm] FaceChallengeImagesFormRequest request,
        CancellationToken ct)
    {
        var neutral = await FaceRequestMapper.ReadImageAsync(request.NeutralImage, "neutralImage", ct);
        var challenge = await FaceRequestMapper.ReadImageAsync(request.ChallengeImage, "challengeImage", ct);

        return Ok(await face.VerifyBuyerAsync(
            actor.RequireIdCompradorExterno(),
            request.ChallengeId,
            neutral,
            challenge,
            ct));
    }

    [HttpPost("card/segment")]
    [Consumes("multipart/form-data")]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SegmentForCard(
        [FromForm] FaceImageFormRequest request,
        CancellationToken ct)
    {
        var image = await FaceRequestMapper.ReadImageAsync(request.Image, "image", ct);
        var result = await face.SegmentForCardAsync(actor.RequireIdCompradorExterno(), image, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }
}

public sealed class FaceChallengeImagesFormRequest
{
    public string ChallengeId { get; init; } = string.Empty;
    public IFormFile? NeutralImage { get; init; }
    public IFormFile? ChallengeImage { get; init; }
}

public sealed class FaceImageFormRequest
{
    public IFormFile? Image { get; init; }
}
