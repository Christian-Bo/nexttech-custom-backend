using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Common;

namespace NextTech.Api.Middleware;

/// <summary>
/// Converts exceptions to RFC 7807 ProblemDetails.
/// Production responses never expose stack traces or provider details.
/// Development responses include a debug section to speed up local diagnosis.
/// </summary>
public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var appException = exception as AppException;
        var category = GetCategory(exception);
        var status = appException?.StatusCode ?? GetInfrastructureStatus(category);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "API error. Category={Category} Method={Method} Path={Path} TraceId={TraceId}",
                category,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                "API request rejected. Status={Status} Code={Code} Method={Method} Path={Path} TraceId={TraceId}",
                status,
                appException?.ErrorCode,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }

        var isDevelopment = environment.IsDevelopment();
        var title = ResolveTitle(status, appException, category, isDevelopment);
        var detail = ResolveDetail(status, exception, appException, category, isDevelopment);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = "about:blank"
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["method"] = context.Request.Method;

        if (isDevelopment)
        {
            var root = GetRootCause(exception);
            problem.Extensions["debug"] = new
            {
                category,
                exceptionType = exception.GetType().FullName,
                exceptionMessage = exception.Message,
                rootCauseType = root.GetType().FullName,
                rootCauseMessage = root.Message,
                innerExceptions = GetInnerExceptions(exception),
                developerHint = GetDeveloperHint(exception, category),
                stackTrace = exception.StackTrace
            };
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }

    private static int GetInfrastructureStatus(string category)
        => category is "OracleDatabase" or "SqlServerDatabase" or "ExternalHttp" or "Timeout"
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status500InternalServerError;

    private static string ResolveTitle(
        int status,
        AppException? appException,
        string category,
        bool isDevelopment)
    {
        if (appException is not null)
            return appException.ErrorCode;

        if (isDevelopment && status >= 500)
            return category + "Error";

        return status == StatusCodes.Status503ServiceUnavailable
            ? "Servicio temporalmente no disponible"
            : "Error interno del servidor";
    }

    private static string ResolveDetail(
        int status,
        Exception exception,
        AppException? appException,
        string category,
        bool isDevelopment)
    {
        if (appException is not null)
            return appException.Message;

        if (isDevelopment)
            return exception.Message;

        return status == StatusCodes.Status503ServiceUnavailable
            ? category switch
            {
                "OracleDatabase" => "No fue posible completar la operación con Oracle.",
                "SqlServerDatabase" => "No fue posible completar la operación con SQL Server.",
                "ExternalHttp" => "Un servicio externo no está disponible.",
                _ => "Una dependencia requerida no está disponible."
            }
            : "Ocurrió un error inesperado.";
    }

    private static string GetCategory(Exception exception)
    {
        foreach (var current in EnumerateExceptionChain(exception))
        {
            var fullName = current.GetType().FullName ?? current.GetType().Name;

            if (fullName.Contains("OracleException", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("ORA-", StringComparison.OrdinalIgnoreCase))
                return "OracleDatabase";

            if (fullName.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
                return "SqlServerDatabase";

            if (current is HttpRequestException)
                return "ExternalHttp";

            if (current is TimeoutException ||
                current is TaskCanceledException && !current.Message.Contains("canceled", StringComparison.OrdinalIgnoreCase))
                return "Timeout";
        }

        return exception is AppException ? "Application" : "Unexpected";
    }

    private static object[] GetInnerExceptions(Exception exception)
        => EnumerateExceptionChain(exception)
            .Skip(1)
            .Take(5)
            .Select(static ex => (object)new
            {
                type = ex.GetType().FullName,
                message = ex.Message
            })
            .ToArray();

    private static Exception GetRootCause(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null)
            current = current.InnerException;
        return current;
    }

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    private static string GetDeveloperHint(Exception exception, string category)
    {
        var allMessages = string.Join(" | ", EnumerateExceptionChain(exception).Select(static x => x.Message));

        if (category == "OracleDatabase")
        {
            if (Contains(allMessages, "ORA-01017"))
                return "Oracle rechazó usuario/password. Revise ORACLE_USER y ORACLE_PASSWORD del .env.";
            if (ContainsAny(allMessages, "ORA-12154", "ORA-12162"))
                return "Oracle no pudo resolver Data Source/TNS. Revise ORACLE_DATA_SOURCE.";
            if (Contains(allMessages, "ORA-12514"))
                return "El listener respondió, pero el servicio Oracle solicitado no está registrado. Revise el service name de ORACLE_DATA_SOURCE.";
            if (ContainsAny(allMessages, "ORA-12541", "ORA-12545", "ORA-12543"))
                return "No se puede alcanzar el listener/host Oracle. Revise host, puerto, VPN/red y firewall.";
            if (Contains(allMessages, "ORA-00942"))
                return "La tabla/vista no existe para este usuario o no tiene permisos. Verifique TIENDA_APP.USUARIO y grants.";
            if (Contains(allMessages, "ORA-00904") && Contains(allMessages, "\"FALSE\""))
                return "El proveedor Oracle generó un literal booleano SQL no soportado. Materialice valores S/N y conviértalos a bool en C# antes de proyectarlos.";
            if (Contains(allMessages, "ORA-00904"))
                return "El modelo/SQL está usando un identificador Oracle inválido. Compare el nombre reportado con el esquema real.";
            if (Contains(allMessages, "ORA-01031"))
                return "El usuario Oracle no posee privilegios suficientes para esta operación.";
            if (Contains(allMessages, "ORA-00001"))
                return "Se violó una restricción UNIQUE. Para registro debe traducirse a 409 Conflict; revise el constraint reportado.";
            if (Contains(allMessages, "ORA-12899"))
                return "Un valor excede la longitud máxima de una columna Oracle. Revise la columna reportada.";
            if (Contains(allMessages, "ORA-01400"))
                return "Se intentó insertar NULL en una columna obligatoria. Revise la columna reportada.";
            if (ContainsAny(allMessages, "ORA-02290", "ORA-02291", "ORA-02292"))
                return "Falló una regla CHECK/FK de Oracle. Revise el constraint reportado y el payload enviado.";

            return "Revise rootCauseMessage y el código ORA-xxxx. También pruebe GET /api/dev/diagnostics/oracle.";
        }

        if (category == "SqlServerDatabase")
            return "SQL Server todavía puede estar fuera de servicio. Para Backend 1 comprador use Oracle; deje InternalAuth pendiente hasta que Infra termine SQL Server.";

        if (category == "ExternalHttp")
            return "Falló una llamada HTTP externa. Revise URL, DNS, TLS, API key y timeout del proveedor.";

        return "Use traceId para correlacionar esta respuesta con la ventana Output/Console de la API.";
    }

    private static bool Contains(string source, string value)
        => source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string source, params string[] values)
        => values.Any(value => Contains(source, value));
}
