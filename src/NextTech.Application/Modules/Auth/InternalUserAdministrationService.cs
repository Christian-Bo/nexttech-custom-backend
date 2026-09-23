using System.Net.Mail;
using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;

namespace NextTech.Application.Modules.Auth;

public sealed class InternalUserAdministrationService(
    IInternalUserAdministrationRepository repository,
    IPasswordService passwords)
{
    public Task<IReadOnlyList<InternalRoleInfo>> GetRolesAsync(CancellationToken ct)
        => repository.GetRolesAsync(ct);

    public Task<PagedResult<InternalUserInfo>> SearchAsync(InternalUserListRequest request, CancellationToken ct)
    {
        if (request.Page < 1)
            throw new AppValidationException("La página debe ser mayor o igual a 1.");
        if (request.PageSize is < 1 or > 100)
            throw new AppValidationException("El tamaño de página debe estar entre 1 y 100.");

        if (!string.IsNullOrWhiteSpace(request.RoleCode))
            EnsureSupportedRole(request.RoleCode);

        var normalized = request with
        {
            Search = NormalizeOptional(request.Search),
            RoleCode = NormalizeOptional(request.RoleCode)?.ToUpperInvariant()
        };

        return repository.SearchAsync(normalized, ct);
    }

    public async Task<InternalUserInfo> GetByIdAsync(int userId, CancellationToken ct)
        => await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

    public async Task<InternalUserInfo> CreateAsync(
        CreateInternalUserRequest request,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var firstName = ValidateName(request.FirstName, "nombres");
        var lastName = ValidateName(request.LastName, "apellidos");
        var email = ValidateEmail(request.Email);
        var roleCode = NormalizeRole(request.RoleCode);
        PasswordPolicy.Validate(request.TemporaryPassword);

        var role = await repository.FindRoleByCodeAsync(roleCode, ct)
            ?? throw new AppValidationException("El rol interno indicado no existe.");

        if (await repository.EmailExistsAsync(email, null, ct))
            throw new AppConflictException("Ya existe un usuario interno con ese correo.");

        var data = new CreateInternalUserData(
            firstName,
            lastName,
            email,
            role.Id,
            passwords.Hash(request.TemporaryPassword));

        return await repository.CreateAsync(data, actorUserId, ip, ct);
    }

    public async Task<InternalUserInfo> UpdateAsync(
        int userId,
        UpdateInternalUserRequest request,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var current = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        var firstName = ValidateName(request.FirstName, "nombres");
        var lastName = ValidateName(request.LastName, "apellidos");
        var email = ValidateEmail(request.Email);
        var roleCode = NormalizeRole(request.RoleCode);
        var role = await repository.FindRoleByCodeAsync(roleCode, ct)
            ?? throw new AppValidationException("El rol interno indicado no existe.");

        if (await repository.EmailExistsAsync(email, userId, ct))
            throw new AppConflictException("Ya existe un usuario interno con ese correo.");

        if (current.Role.Code == InternalRoles.Admin && role.Code != InternalRoles.Admin)
            await EnsureNotRemovingLastActiveAdminAsync(current, ct);

        var data = new UpdateInternalUserData(firstName, lastName, email, role.Id);
        return await repository.UpdateAsync(userId, data, actorUserId, ip, ct);
    }

    public async Task<InternalUserInfo> DeactivateAsync(
        int userId,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        if (userId == actorUserId)
            throw new AppConflictException("No puede desactivar su propia cuenta administrativa.");

        var current = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        if (!current.IsActive)
            return current;

        await EnsureNotRemovingLastActiveAdminAsync(current, ct);
        return await repository.SetActiveAsync(userId, false, actorUserId, ip, ct);
    }

    public async Task<InternalUserInfo> ActivateAsync(
        int userId,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var current = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        return current.IsActive
            ? current
            : await repository.SetActiveAsync(userId, true, actorUserId, ip, ct);
    }

    public async Task<InternalUserInfo> UnlockAsync(
        int userId,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        _ = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        return await repository.UnlockAsync(userId, actorUserId, ip, ct);
    }

    public async Task<InternalUserInfo> ResetPasswordAsync(
        int userId,
        ResetInternalPasswordRequest request,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        if (userId == actorUserId)
            throw new AppConflictException("Para cambiar su propia contraseña use /api/internal/auth/change-password.");

        _ = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        PasswordPolicy.Validate(request.TemporaryPassword);
        return await repository.ResetPasswordAsync(
            userId,
            passwords.Hash(request.TemporaryPassword),
            actorUserId,
            ip,
            ct);
    }

    private async Task EnsureNotRemovingLastActiveAdminAsync(InternalUserInfo current, CancellationToken ct)
    {
        if (!current.IsActive || current.Role.Code != InternalRoles.Admin)
            return;

        if (await repository.CountActiveAdminsAsync(ct) <= 1)
            throw new AppConflictException("Debe existir al menos un ADMIN activo en el sistema.");
    }

    private static string ValidateName(string value, string field)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 100)
            throw new AppValidationException($"El campo {field} es obligatorio y no puede exceder 100 caracteres.");
        return normalized;
    }

    private static string ValidateEmail(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 200 || !MailAddress.TryCreate(normalized, out _))
            throw new AppValidationException("El correo interno no tiene un formato válido o excede 200 caracteres.");
        return normalized;
    }

    private static string NormalizeRole(string value)
    {
        var roleCode = value.Trim().ToUpperInvariant();
        EnsureSupportedRole(roleCode);
        return roleCode;
    }

    private static void EnsureSupportedRole(string roleCode)
    {
        if (!InternalRoles.All.Contains(roleCode.Trim().ToUpperInvariant()))
            throw new AppValidationException("El rol interno debe ser ADMIN, SUPERVISOR o REPARTIDOR.");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
