using NextTech.Application.Authentication;

namespace NextTech.Application.Interfaces;

public interface IInternalUserAdministrationRepository
{
    Task<IReadOnlyList<InternalRoleInfo>> GetRolesAsync(CancellationToken ct);
    Task<InternalRoleInfo?> FindRoleByCodeAsync(string roleCode, CancellationToken ct);
    Task<InternalUserInfo?> FindByIdAsync(int userId, CancellationToken ct);
    Task<bool> EmailExistsAsync(string normalizedEmail, int? excludingUserId, CancellationToken ct);
    Task<int> CountActiveAdminsAsync(CancellationToken ct);
    Task<PagedResult<InternalUserInfo>> SearchAsync(InternalUserListRequest request, CancellationToken ct);
    Task<InternalUserInfo> CreateAsync(CreateInternalUserData data, int actorUserId, string? ip, CancellationToken ct);
    Task<InternalUserInfo> UpdateAsync(int userId, UpdateInternalUserData data, int actorUserId, string? ip, CancellationToken ct);
    Task<InternalUserInfo> SetActiveAsync(int userId, bool active, int actorUserId, string? ip, CancellationToken ct);
    Task<InternalUserInfo> UnlockAsync(int userId, int actorUserId, string? ip, CancellationToken ct);
    Task<InternalUserInfo> ResetPasswordAsync(int userId, string passwordHash, int actorUserId, string? ip, CancellationToken ct);
}
