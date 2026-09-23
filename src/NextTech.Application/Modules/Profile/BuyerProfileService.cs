using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Application.Profiles;

namespace NextTech.Application.Modules.Profile;

public sealed class BuyerProfileService(
    IBuyerProfileRepository profiles,
    ICentralIdentityGateway identity,
    IPasswordService passwords,
    ITokenService tokens)
{
    public IReadOnlyList<BuyerNotificationPreferenceOption> GetNotificationOptions()
        => BuyerNotificationPreferenceCodes.Options;

    public async Task<BuyerProfileView> GetAsync(long buyerId, CancellationToken ct)
    {
        var data = await profiles.GetAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");

        EnsureProfileAvailable(data);
        return ToView(data);
    }

    public async Task<BuyerProfileMutationResult> UpdateAsync(
        long buyerId,
        UpdateBuyerProfileRequest request,
        CancellationToken ct)
    {
        var current = await identity.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");
        EnsureBuyerCanManageProfile(current);

        var phone = ValidatePhone(request.Phone);
        var nickname = ValidateNickname(request.Nickname);
        ValidateBirthDate(request.BirthDate);
        var notification = BuyerNotificationPreferenceCodes.Parse(request.NotificationPreferenceCode);

        if (!string.Equals(phone, current.Telefono, StringComparison.Ordinal) &&
            await profiles.IsIdentifierInUseByOtherAsync(phone, buyerId, ct))
        {
            throw new AppConflictException("El teléfono ya está asociado a otra cuenta.");
        }

        if (!string.Equals(nickname, current.Nickname, StringComparison.OrdinalIgnoreCase) &&
            await profiles.IsIdentifierInUseByOtherAsync(nickname, buyerId, ct))
        {
            throw new AppConflictException("El nickname ya está asociado a otra cuenta.");
        }

        try
        {
            await profiles.UpdateAsync(
                buyerId,
                new BuyerProfileUpdateData(
                    phone,
                    nickname,
                    request.BirthDate?.Date,
                    notification.Email,
                    notification.WhatsApp),
                ct);
        }
        catch (Exception ex) when (IsLikelyUniqueViolation(ex))
        {
            throw new AppConflictException("El nickname ya está asociado a otra cuenta.");
        }

        return await CreateMutationResultAsync(buyerId, ct);
    }

    public async Task<BuyerProfileMutationResult> ChangePasswordAsync(
        long buyerId,
        ChangeBuyerPasswordRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new AppValidationException("La contraseña actual es obligatoria.");

        PasswordPolicy.Validate(request.NewPassword);
        var currentPassword = request.CurrentPassword!;
        var newPassword = request.NewPassword!;

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            throw new AppValidationException("La nueva contraseña debe ser diferente de la contraseña actual.");

        var buyer = await identity.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");
        EnsureBuyerCanManageProfile(buyer);

        var verification = passwords.Verify(buyer.PasswordHash, currentPassword);
        var valid = verification is PasswordCheckResult.Success or PasswordCheckResult.SuccessRehashNeeded;

        if (verification == PasswordCheckResult.UnsupportedLegacyFormat)
            valid = await identity.VerifyLegacyPasswordAsync(buyer.Correo, currentPassword, ct);

        if (!valid)
            throw new AppUnauthorizedException("La contraseña actual no es correcta.");

        await identity.SetPasswordHashAsync(
            buyerId,
            passwords.Hash(newPassword),
            unlock: false,
            ct);

        return await CreateMutationResultAsync(buyerId, ct);
    }

    public async Task<BuyerDisplayPhoto> GetDisplayPhotoAsync(long buyerId, CancellationToken ct)
    {
        var buyer = await identity.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");
        EnsureBuyerCanManageProfile(buyer);

        return await profiles.GetDisplayPhotoAsync(buyerId, ct)
            ?? throw new AppNotFoundException("El comprador no tiene una fotografía de perfil activa.");
    }

    private async Task<BuyerProfileMutationResult> CreateMutationResultAsync(
        long buyerId,
        CancellationToken ct)
    {
        var profile = await GetAsync(buyerId, ct);
        var auth = await identity.FindByIdAsync(buyerId, ct)
            ?? throw new AppDependencyException("El perfil se actualizó, pero no pudo recuperarse desde Oracle.");

        var token = tokens.CreateBuyerToken(new BuyerProfile(
            auth.IdUsuario,
            auth.Correo,
            auth.Telefono,
            auth.FechaNacimiento,
            auth.Nickname,
            auth.NotificaEmail,
            auth.NotificaWhatsApp,
            auth.Activo,
            auth.Bloqueado));

        return new BuyerProfileMutationResult(profile, token);
    }

    private static BuyerProfileView ToView(BuyerProfileData data)
        => new(
            data.BuyerId,
            data.Email,
            data.Phone,
            data.BirthDate,
            data.Nickname,
            BuyerRoleInfo.Buyer,
            BuyerNotificationPreferenceCodes.FromFlags(data.NotifyByEmail, data.NotifyByWhatsApp),
            new BuyerAccountState(
                data.IsActive,
                data.IsBlocked,
                data.IsActive && !data.IsBlocked),
            new BuyerPhotoState(
                data.HasOriginalPhoto,
                data.HasDisplayPhoto,
                data.HasOriginalPhoto && data.HasDisplayPhoto,
                data.DisplayPhotoContentType,
                data.PhotoUpdatedAtUtc),
            data.LastAccessUtc,
            data.CreatedAtUtc,
            data.UpdatedAtUtc);

    private static void EnsureProfileAvailable(BuyerProfileData data)
    {
        if (!data.IsActive)
            throw new AppForbiddenException("La cuenta está inactiva.");
        if (data.IsBlocked)
            throw new AppForbiddenException("La cuenta está bloqueada.");
    }

    private static void EnsureBuyerCanManageProfile(BuyerAuthRecord buyer)
    {
        if (!buyer.Activo)
            throw new AppForbiddenException("La cuenta está inactiva.");
        if (buyer.Bloqueado)
            throw new AppForbiddenException("La cuenta está bloqueada.");
    }

    private static string ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new AppValidationException("El teléfono es obligatorio y no puede exceder 25 caracteres.");

        var value = phone.Trim();
        if (value.Length > 25)
            throw new AppValidationException("El teléfono es obligatorio y no puede exceder 25 caracteres.");
        return value;
    }

    private static string ValidateNickname(string? nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            throw new AppValidationException("El nickname es obligatorio y no puede exceder 50 caracteres.");

        var value = nickname.Trim();
        if (value.Length > 50)
            throw new AppValidationException("El nickname es obligatorio y no puede exceder 50 caracteres.");
        return value;
    }

    private static void ValidateBirthDate(DateTime? birthDate)
    {
        if (birthDate is not null && birthDate.Value.Date > DateTime.UtcNow.Date)
            throw new AppValidationException("La fecha de nacimiento no puede estar en el futuro.");
    }

    private static bool IsLikelyUniqueViolation(Exception ex)
        => ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase);
}
