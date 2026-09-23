using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;

namespace NextTech.Application.Modules.Auth;

public sealed class InternalSecurityAuditService(IInternalSecurityAuditRepository repository)
{
    public Task<PagedResult<InternalAuditEntryInfo>> SearchAsync(
        InternalAuditListRequest request,
        CancellationToken ct)
    {
        if (request.Page < 1)
            throw new AppValidationException("La página debe ser mayor o igual a 1.");
        if (request.PageSize is < 1 or > 100)
            throw new AppValidationException("El tamaño de página debe estar entre 1 y 100.");
        if (request.FromUtc.HasValue && request.ToUtc.HasValue && request.FromUtc > request.ToUtc)
            throw new AppValidationException("La fecha inicial no puede ser posterior a la fecha final.");

        var normalized = request with
        {
            Action = NormalizeOptional(request.Action)?.ToUpperInvariant(),
            Result = NormalizeOptional(request.Result)?.ToUpperInvariant()
        };

        return repository.SearchAsync(normalized, ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
