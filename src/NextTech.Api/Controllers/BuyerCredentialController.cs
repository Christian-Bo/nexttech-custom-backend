using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Common;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Credentials;
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
    [HttpGet("customization-context")]
    [ProducesResponseType(typeof(CredentialCustomizationContext), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CredentialCustomizationContext>> GetCustomizationContext(
        CancellationToken ct)
        => Ok(await credentials.GetCustomizationContextAsync(
            actor.RequireIdCompradorExterno(),
            ct));

    [HttpPut("customization")]
    [ProducesResponseType(typeof(CredentialCustomization), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CredentialCustomization>> SaveCustomization(
        [FromBody] CredentialCustomizationRequest request,
        CancellationToken ct)
        => Ok(await credentials.SaveCustomizationAsync(
            actor.RequireIdCompradorExterno(),
            request,
            ct));

    [HttpPost("preview")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Preview(
        [FromBody] CredentialCustomizationRequest request,
        CancellationToken ct)
    {
        var document = await credentials.PreviewAsync(
            actor.RequireIdCompradorExterno(),
            request,
            ct);

        SetNoStoreHeaders();
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPost("confirm")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Confirm(
        [FromBody] CredentialCustomizationRequest request,
        CancellationToken ct)
    {
        var document = await credentials.ConfirmCustomizationAndIssueAsync(
            actor.RequireIdCompradorExterno(),
            request,
            ct);

        SetNoStoreHeaders();
        return File(document.Content, document.ContentType, document.FileName);
    }

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

    // No crea un QR nuevo. Recibe el PDF visual del frontend y reutiliza
    // las notificaciones configuradas para el comprador autenticado.
    [HttpPost("send-personalized")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8 * 1024 * 1024 + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024 + 64 * 1024)]
    [ProducesResponseType(typeof(NotificationDispatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<NotificationDispatchResult>> SendPersonalized(
        [FromForm] PersonalizedCredentialUploadForm request,
        CancellationToken ct)
    {
        var pdf = request.Pdf;
        if (pdf is null || pdf.Length == 0 || pdf.Length > 8 * 1024 * 1024 ||
            !string.Equals(pdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new AppValidationException("Adjunta el PDF de la credencial (máximo 8 MB).");

        await using var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer, ct);
        var result = await credentials.SendPersonalizedAsync(
            actor.RequireIdCompradorExterno(), request.QrCredential ?? string.Empty, buffer.ToArray(), ct);
        SetNoStoreHeaders();
        return Ok(result);
    }

    private void SetNoStoreHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }
}

public sealed class PersonalizedCredentialUploadForm
{
    public IFormFile? Pdf { get; set; }
    public string? QrCredential { get; set; }
}
