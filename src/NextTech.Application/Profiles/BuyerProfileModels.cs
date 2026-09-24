using NextTech.Application.Authentication;
using NextTech.Application.Common;

namespace NextTech.Application.Profiles;

public static class BuyerNotificationPreferenceCodes
{
    public const string Email = "EMAIL";
    public const string WhatsApp = "WHATSAPP";
    public const string EmailAndWhatsApp = "EMAIL_AND_WHATSAPP";

    public static readonly IReadOnlyList<BuyerNotificationPreferenceOption> Options =
    [
        new(Email, "Correo electrónico"),
        new(WhatsApp, "WhatsApp"),
        new(EmailAndWhatsApp, "Correo electrónico y WhatsApp")
    ];

    public static BuyerNotificationPreferenceOption FromFlags(bool email, bool whatsApp)
        => (email, whatsApp) switch
        {
            (true, true) => Options[2],
            (true, false) => Options[0],
            (false, true) => Options[1],
            _ => throw new InvalidOperationException("El comprador no tiene un canal de notificación válido.")
        };

    public static (bool Email, bool WhatsApp) Parse(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new AppValidationException("La preferencia de notificación es obligatoria.");

        return code.Trim().ToUpperInvariant() switch
        {
            Email => (true, false),
            WhatsApp => (false, true),
            EmailAndWhatsApp => (true, true),
            _ => throw new AppValidationException(
                "La preferencia de notificación debe ser EMAIL, WHATSAPP o EMAIL_AND_WHATSAPP.")
        };
    }
}

public sealed record BuyerNotificationPreferenceOption(string Code, string Name);

public sealed record BuyerRoleInfo(string Code, string Name)
{
    public static readonly BuyerRoleInfo Buyer = new("COMPRADOR", "Comprador");
}

public sealed record BuyerAccountState(
    bool IsActive,
    bool IsBlocked,
    bool CanAuthenticate);

public sealed record BuyerPhotoState(
    bool HasOriginalPhoto,
    bool HasDisplayPhoto,
    bool HasFaceEnrollment,
    string? DisplayPhotoContentType,
    DateTimeOffset? UpdatedAtUtc);

public sealed record BuyerProfileView(
    long BuyerId,
    string Email,
    string Phone,
    DateTime? BirthDate,
    string Nickname,
    BuyerRoleInfo Role,
    BuyerNotificationPreferenceOption NotificationPreference,
    BuyerAccountState Account,
    BuyerPhotoState Photo,
    DateTimeOffset? LastAccessUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateBuyerProfileRequest(
    string? Phone,
    string? Nickname,
    DateTime? BirthDate,
    string? NotificationPreferenceCode);

public sealed record ChangeBuyerPasswordRequest(
    string? CurrentPassword,
    string? NewPassword);

public sealed record BuyerProfileMutationResult(
    BuyerProfileView Profile,
    AccessTokenResult RefreshedToken);

public sealed record BuyerProfileData(
    long BuyerId,
    string Email,
    string Phone,
    DateTime? BirthDate,
    string Nickname,
    bool NotifyByEmail,
    bool NotifyByWhatsApp,
    bool IsActive,
    bool IsBlocked,
    bool HasOriginalPhoto,
    bool HasDisplayPhoto,
    string? DisplayPhotoContentType,
    DateTimeOffset? PhotoUpdatedAtUtc,
    DateTimeOffset? LastAccessUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BuyerProfileUpdateData(
    string Phone,
    string Nickname,
    DateTime? BirthDate,
    bool NotifyByEmail,
    bool NotifyByWhatsApp);

public sealed record BuyerDisplayPhoto(
    byte[] Content,
    string ContentType,
    string FileName,
    string? Sha256,
    DateTimeOffset UploadedAtUtc);
