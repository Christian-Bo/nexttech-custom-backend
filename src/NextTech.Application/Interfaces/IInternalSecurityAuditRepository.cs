using NextTech.Application.Authentication;

namespace NextTech.Application.Interfaces;

public interface IInternalSecurityAuditRepository
{
    Task<PagedResult<InternalAuditEntryInfo>> SearchAsync(InternalAuditListRequest request, CancellationToken ct);
}
