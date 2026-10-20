using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Api.Middleware;
using NextTech.Application.Common;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Face;
using NextTech.Application.Modules.Auth;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    BuyerAuthService auth,
    IConfiguration configuration,
    IHostEnvironment environment,
    ICurrentActor actor,
    ILogger<AuthController> logger) : ControllerBase
{
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(BuyerRegistrationResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(BuyerRegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public Task<NextTech.Application.Authentication.AccessTokenResult> Login(LoginRequest request, CancellationToken ct)
        => auth.LoginAsync(request, ct);

    [EnableRateLimiting("auth")]
    [HttpPost("qr-login")]
    public Task<NextTech.Application.Authentication.AccessTokenResult> QrLogin(QrLoginRequest request, CancellationToken ct)
        => auth.LoginByQrAsync(request, ct);

    [Authorize(Policy = "BuyerOnly")]
    [HttpPost("qr/rotate")]
    public async Task<object> RotateQr(CancellationToken ct)
    {
        return new { qrCredential = await auth.RotateQrAsync(actor.RequireIdCompradorExterno(), ct) };
    }

    [EnableRateLimiting("recovery")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(RecoveryRequest request, CancellationToken ct)
    {
        var token = await auth.StartRecoveryAsync(request, ct);
        var exposeDevelopmentToken = environment.IsDevelopment() &&
            !configuration.GetValue<bool>("Smtp:Enabled");

        return Ok(new
        {
            message = "Si la cuenta existe, se inició el proceso de recuperación.",
            developmentToken = exposeDevelopmentToken ? token : null
        });
    }

    [EnableRateLimiting("recovery")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(request, ct);
        return NoContent();
    }

    [EnableRateLimiting("biometric")]
    [HttpPost("face-challenge")]
    [ProducesResponseType(typeof(FaceChallengeResult), StatusCodes.Status200OK)]
    public Task<FaceChallengeResult> FaceChallenge(CancellationToken ct)
        => auth.CreateFaceLoginChallengeAsync(ct);

    /// <summary>Identifica un comprador 1:N usando solo una captura frontal limpia (sin challenge).</summary>
    /// <remarks>Modo rápido sin prueba de vida; retorna el mismo JWT que el login tradicional.
    /// Solo admite un campo multipart llamado image. Requiere FACE_API_IDENTIFICATION_ENABLED=true.</remarks>
    [EnableRateLimiting("biometric")]
    [HttpPost("face-identify-login")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(NextTech.Application.Authentication.AccessTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<NextTech.Application.Authentication.AccessTokenResult> FaceIdentifyLogin(
        [FromForm] FaceIdentifyLoginFormRequest request,
        CancellationToken ct)
    {
        // Started before MVC parses the multipart request, so this includes upload handling.
        var total = HttpContext.Items[RequestDiagnosticsMiddleware.RequestStopwatchKey] as Stopwatch
            ?? Stopwatch.StartNew();
        var image = await FaceRequestMapper.ReadImageAsync(request.Image, "image", ct);
        var outcome = await auth.LoginByFaceIdentificationAsync(image, ct);

        var faceApiHttpMs = outcome.Identification.FaceApiHttpMs;
        var customProcessingMs = Math.Max(0, total.Elapsed.TotalMilliseconds - faceApiHttpMs);
        logger.LogInformation(
            "FaceIdentifyLogin completed. CustomProcessingMs={customProcessingMs} " +
            "FaceApiProcessingMs={faceApiProcessingMs} FaceApiSearchMs={faceApiSearchMs} " +
            "FaceApiHttpMs={faceApiHttpMs} TotalLoginMs={totalLoginMs} " +
            "FaceApiTraceId={faceApiTraceId} TraceId={traceId}",
            Math.Round(customProcessingMs, 2),
            outcome.Identification.FaceApiProcessingMs,
            outcome.Identification.FaceApiSearchMs,
            Math.Round(faceApiHttpMs, 2),
            Math.Round(total.Elapsed.TotalMilliseconds, 2),
            outcome.Identification.FaceApiTraceId,
            HttpContext.TraceIdentifier);

        return outcome.Token;
    }

    [EnableRateLimiting("biometric")]
    [HttpPost("face-login")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(NextTech.Application.Authentication.AccessTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<NextTech.Application.Authentication.AccessTokenResult> FaceLogin(
        [FromForm] FaceLoginFormRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
            throw new AppValidationException("El identificador es obligatorio.");

        var neutral = await FaceRequestMapper.ReadImageAsync(request.NeutralImage, "neutralImage", ct);
        var challenge = await FaceRequestMapper.ReadImageAsync(request.ChallengeImage, "challengeImage", ct);

        return await auth.LoginByFaceAsync(
            request.Identifier.Trim(),
            request.ChallengeId,
            neutral,
            challenge,
            ct);
    }
}

public sealed class FaceLoginFormRequest
{
    public string Identifier { get; init; } = string.Empty;
    public string ChallengeId { get; init; } = string.Empty;
    public IFormFile? NeutralImage { get; init; }
    public IFormFile? ChallengeImage { get; init; }
}

public sealed class FaceIdentifyLoginFormRequest
{
    /// <summary>Fotografía frontal limpia (JPEG, PNG o WebP; máximo 5 MB).</summary>
    public IFormFile? Image { get; init; }
}
