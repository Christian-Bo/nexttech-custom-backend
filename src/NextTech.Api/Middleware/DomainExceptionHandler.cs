using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NextTech.Domain.Exceptions;

namespace NextTech.Api.Middleware;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "No encontrado"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Regla de negocio"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "No autorizado"),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
