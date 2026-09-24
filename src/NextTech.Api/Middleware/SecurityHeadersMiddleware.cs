namespace NextTech.Api.Middleware;

/// <summary>
/// Cabeceras defensivas para la API. No intenta sustituir TLS, CORS ni autorización.
/// Las rutas con autenticación/identidad se marcan como no-cache para evitar que
/// respuestas sensibles queden almacenadas por navegadores o proxies intermedios.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
        context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        if (IsSensitiveApiPath(context.Request.Path))
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
        }

        await next(context);
    }

    private static bool IsSensitiveApiPath(PathString path)
        => path.StartsWithSegments("/api/auth") ||
           path.StartsWithSegments("/api/internal") ||
           path.StartsWithSegments("/api/profile") ||
           path.StartsWithSegments("/api/face") ||
           path.StartsWithSegments("/api/credential") ||
           path.StartsWithSegments("/api/cart") ||
           path.StartsWithSegments("/api/personalizations") ||
           path.StartsWithSegments("/api/checkout") ||
           path.StartsWithSegments("/api/orders") ||
           path.StartsWithSegments("/api/delivery") ||
           path.StartsWithSegments("/api/dashboard") ||
           path.StartsWithSegments("/api/supervisor");
}
