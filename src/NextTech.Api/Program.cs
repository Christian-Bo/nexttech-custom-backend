using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using NextTech.Api.Configuration;
using NextTech.Api.CurrentActor;
using NextTech.Api.Extensions;
using NextTech.Api.Middleware;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Auth;
using NextTech.Application.Modules.Credentials;
using NextTech.Application.Modules.Face;
using NextTech.Infrastructure;

LocalEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<BuyerAuthService>();
builder.Services.AddScoped<InternalAuthService>();
builder.Services.AddScoped<InternalUserAdministrationService>();
builder.Services.AddScoped<InternalSecurityAuditService>();
builder.Services.AddScoped<BuyerCredentialService>();
builder.Services.AddScoped<FaceApplicationService>();
builder.Services.AddNextTechAuthorization();

var allowedOrigins = builder.Configuration
    .GetSection("AllowedClientOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0)
{
    allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];
}

allowedOrigins = allowedOrigins
    .Where(static origin => !string.IsNullOrWhiteSpace(origin))
    .Select(static origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Debe configurar al menos un origen permitido en AllowedClientOrigins. " +
        "En producción use variables como AllowedClientOrigins__0=https://tu-frontend.com.");
}

foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException($"Origen CORS inválido: '{origin}'. Use solo http:// o https:// sin rutas.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = "TooManyRequests",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Demasiadas solicitudes. Intente nuevamente más tarde.",
            traceId = context.HttpContext.TraceIdentifier
        }, ct);
    };

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("recovery", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("biometric", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("credential", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetBuyerOrClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NextTech Custom API",
        Version = "v1",
        Description = "API del proyecto NextTech Custom."
    });

    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", bearer);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearer] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<RequestDiagnosticsMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                debug = app.Environment.IsDevelopment() && entry.Value.Exception is not null
                    ? new
                    {
                        exceptionType = entry.Value.Exception.GetType().FullName,
                        message = entry.Value.Exception.Message,
                        innerMessage = entry.Value.Exception.InnerException?.Message
                    }
                    : null
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.Run();

static string GetClientPartitionKey(HttpContext context)
    => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static string GetBuyerOrClientPartitionKey(HttpContext context)
{
    var buyerId = context.User.FindFirst("buyer_id")?.Value;
    return !string.IsNullOrWhiteSpace(buyerId)
        ? $"buyer:{buyerId}"
        : $"client:{GetClientPartitionKey(context)}";
}

public partial class Program { }
