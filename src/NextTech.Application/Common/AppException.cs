namespace NextTech.Application.Common;

public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public virtual string ErrorCode => GetType().Name;
}

public sealed class AppValidationException(string message) : AppException(message)
{
    public override int StatusCode => 400;
}

public sealed class AppUnauthorizedException(string message = "Credenciales inválidas.") : AppException(message)
{
    public override int StatusCode => 401;
}

public sealed class AppForbiddenException(string message) : AppException(message)
{
    public override int StatusCode => 403;
}

public sealed class AppNotFoundException(string message) : AppException(message)
{
    public override int StatusCode => 404;
}

public sealed class AppConflictException(string message) : AppException(message)
{
    public override int StatusCode => 409;
}

public sealed class AppUnprocessableException(string message) : AppException(message)
{
    public override int StatusCode => 422;
}

public sealed class AppDependencyException(string message) : AppException(message)
{
    public override int StatusCode => 503;
}

public sealed class AppProviderRejectedException(
    string errorCode,
    string message,
    int statusCode = 422) : AppException(message)
{
    public override int StatusCode { get; } = statusCode;
    public override string ErrorCode { get; } = string.IsNullOrWhiteSpace(errorCode)
        ? nameof(AppProviderRejectedException)
        : errorCode;
}

// Authentication errors with a safe provider correlation ID. Never include candidate data.
public sealed class AppFaceIdentificationException(
    string code,
    string message,
    int statusCode,
    string? faceApiTraceId = null) : AppException(message)
{
    public override int StatusCode { get; } = statusCode;
    public override string ErrorCode { get; } = code;
    public string? FaceApiTraceId { get; } =
        faceApiTraceId is { Length: > 0 and <= 128 } &&
        faceApiTraceId.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or ':')
            ? faceApiTraceId
            : null;
}
