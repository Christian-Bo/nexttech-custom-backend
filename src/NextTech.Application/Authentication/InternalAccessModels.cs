namespace NextTech.Application.Authentication;

public sealed record InternalRoleInfo(
    int Id,
    string Code,
    string Name);

public sealed record InternalUserInfo(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    InternalRoleInfo Role,
    bool IsActive,
    bool MustChangePassword,
    int FailedAttempts,
    DateTime? LockedUntilUtc,
    bool IsLocked,
    DateTime? LastAccessUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? DeactivatedAtUtc);

public sealed record InternalSessionResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string ActorType,
    bool MustChangePassword,
    InternalUserInfo User);

public sealed record InternalUserListRequest(
    string? Search = null,
    string? RoleCode = null,
    bool? IsActive = null,
    bool? IsLocked = null,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record CreateInternalUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string RoleCode,
    string TemporaryPassword);

public sealed record UpdateInternalUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string RoleCode);

public sealed record ResetInternalPasswordRequest(string TemporaryPassword);

public sealed record CreateInternalUserData(
    string FirstName,
    string LastName,
    string Email,
    int RoleId,
    string PasswordHash);

public sealed record UpdateInternalUserData(
    string FirstName,
    string LastName,
    string Email,
    int RoleId);

public sealed record InternalAuditEntryInfo(
    int Id,
    string ActorType,
    int? InternalUserId,
    string? InternalUserName,
    string? RoleCode,
    string? RoleName,
    long? BuyerId,
    string Action,
    string Entity,
    int? EntityId,
    string? EntityDisplayName,
    string Result,
    string? IpAddress,
    string? Detail,
    DateTime OccurredAtUtc);

public sealed record InternalAuditListRequest(
    int? InternalUserId = null,
    string? Action = null,
    string? Result = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50);
