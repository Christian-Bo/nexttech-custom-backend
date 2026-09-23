using System.Security.Cryptography;
using System.Text;
using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;

namespace NextTech.Application.Modules.Auth;

public sealed record BuyerRegisterRequest(
    string Email,
    string Phone,
    string Nickname,
    string Password,
    DateTime? BirthDate,
    bool NotifyByEmail,
    bool NotifyByWhatsApp);

public sealed record BuyerRegistrationResult(
    long BuyerId,
    string QrCredential,
    AccessTokenResult Token);

public sealed record LoginRequest(string Identifier, string Password);
public sealed record QrLoginRequest(string QrCredential);
public sealed record RecoveryRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record RecoveryTokenResult(string Token, DateTimeOffset ExpiresAt);
public sealed record InternalLoginRequest(string Email, string Password);
public sealed record ChangeInternalPasswordRequest(string CurrentPassword, string NewPassword);

public sealed class BuyerAuthService(
    ICentralIdentityGateway gateway,
    IPasswordService passwords,
    ITokenService tokens,
    IFaceBiometricService faceBiometrics,
    IBuyerFaceEnrollmentStore enrollmentStore,
    IRegistrationNotificationSender registrationNotifications,
    IRecoveryNotificationSender recoveryNotifications)
{
    public async Task<BuyerRegistrationResult> RegisterAsync(BuyerRegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var nickname = request.Nickname.Trim();
        var phone = request.Phone.Trim();

        PasswordPolicy.Validate(request.Password);
        if (string.IsNullOrWhiteSpace(phone) || phone.Length > 25)
            throw new AppValidationException("El teléfono es obligatorio y no puede exceder 25 caracteres.");
        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 50)
            throw new AppValidationException("El nickname es obligatorio y no puede exceder 50 caracteres.");
        if (!request.NotifyByEmail && !request.NotifyByWhatsApp)
            throw new AppValidationException("Debe habilitar al menos un canal de notificación.");

        if (await gateway.FindByIdentifierAsync(email, ct) is not null)
            throw new AppConflictException("El correo, nickname o teléfono ya está registrado.");
        if (await gateway.FindByIdentifierAsync(nickname, ct) is not null)
            throw new AppConflictException("El correo, nickname o teléfono ya está registrado.");
        if (await gateway.FindByIdentifierAsync(phone, ct) is not null)
            throw new AppConflictException("El correo, nickname o teléfono ya está registrado.");

        var qrCredential = CreateOpaqueToken();
        var data = new BuyerRegistrationData(
            email, phone, request.BirthDate, nickname,
            passwords.Hash(request.Password), HashToken(qrCredential),
            request.NotifyByEmail, request.NotifyByWhatsApp);

        long buyerId;
        try
        {
            buyerId = await gateway.RegisterAsync(data, ct);
        }
        catch (Exception ex) when (IsLikelyUniqueViolation(ex))
        {
            throw new AppConflictException("El correo, nickname o credencial ya existe.");
        }

        var buyer = await gateway.FindByIdAsync(buyerId, ct)
            ?? throw new AppDependencyException("El comprador fue creado, pero no pudo recuperarse desde Oracle.");

        await registrationNotifications.SendRegistrationCredentialAsync(
            buyer.Correo,
            buyer.Nickname,
            qrCredential,
            ct);

        return new BuyerRegistrationResult(buyerId, qrCredential, tokens.CreateBuyerToken(ToProfile(buyer)));
    }

    public async Task<AccessTokenResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var identifier = request.Identifier.Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
            throw new AppValidationException("Identificador y contraseña son obligatorios.");

        var buyer = await gateway.FindByIdentifierAsync(identifier, ct);
        if (buyer is null)
        {
            await gateway.RegisterFailedLoginAsync(null, identifier, "CONTRASENA", "Credenciales inválidas", ct);
            throw new AppUnauthorizedException();
        }
        EnsureBuyerCanLogin(buyer);

        var check = passwords.Verify(buyer.PasswordHash, request.Password);
        var valid = check is PasswordCheckResult.Success or PasswordCheckResult.SuccessRehashNeeded;

        if (check == PasswordCheckResult.UnsupportedLegacyFormat)
        {
            valid = await gateway.VerifyLegacyPasswordAsync(identifier, request.Password, ct);
            if (valid)
                await gateway.SetPasswordHashAsync(buyer.IdUsuario, passwords.Hash(request.Password), unlock: false, ct);
        }

        if (!valid)
        {
            await gateway.RegisterFailedLoginAsync(buyer.IdUsuario, identifier, "CONTRASENA", "Credenciales inválidas", ct);
            throw new AppUnauthorizedException();
        }

        if (check == PasswordCheckResult.SuccessRehashNeeded)
            await gateway.SetPasswordHashAsync(buyer.IdUsuario, passwords.Hash(request.Password), unlock: false, ct);

        await gateway.RegisterSuccessfulLoginAsync(buyer.IdUsuario, identifier, "CONTRASENA", ct);
        var refreshed = await gateway.FindByIdAsync(buyer.IdUsuario, ct) ?? buyer;
        return tokens.CreateBuyerToken(ToProfile(refreshed));
    }

    public async Task<AccessTokenResult> LoginByQrAsync(QrLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.QrCredential))
            throw new AppValidationException("La credencial QR es obligatoria.");

        var buyer = await gateway.FindByQrHashAsync(HashToken(request.QrCredential), ct);
        if (buyer is null)
        {
            await gateway.RegisterFailedLoginAsync(null, "qr", "QR", "QR inválido", ct);
            throw new AppUnauthorizedException("Credencial QR inválida.");
        }

        EnsureBuyerCanLogin(buyer);
        await gateway.RegisterSuccessfulLoginAsync(buyer.IdUsuario, "qr", "QR", ct);
        return tokens.CreateBuyerToken(ToProfile(buyer));
    }

    public async Task<string> RotateQrAsync(long buyerId, CancellationToken ct)
    {
        var buyer = await gateway.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");
        EnsureBuyerCanLogin(buyer);

        var raw = CreateOpaqueToken();
        await gateway.SetQrHashAsync(buyerId, HashToken(raw), ct);
        return raw;
    }

    public async Task<RecoveryTokenResult?> StartRecoveryAsync(RecoveryRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var buyer = await gateway.FindByIdentifierAsync(email, ct);
        if (buyer is null || !buyer.Activo)
            return null; // respuesta pública debe ser siempre neutra

        var raw = CreateOpaqueToken();
        var expires = DateTimeOffset.UtcNow.AddMinutes(30);
        await gateway.CreateRecoveryTokenAsync(buyer.IdUsuario, HashToken(raw), expires, ct);
        await recoveryNotifications.SendPasswordRecoveryAsync(buyer.Correo, raw, expires, ct);
        return new RecoveryTokenResult(raw, expires);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        PasswordPolicy.Validate(request.NewPassword);
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new AppValidationException("El token es obligatorio.");

        var tokenHash = HashToken(request.Token);
        var userId = await gateway.FindValidRecoveryUserAsync(tokenHash, ct)
            ?? throw new AppValidationException("El token no es válido o ya expiró.");

        await gateway.SetPasswordHashAsync(userId, passwords.Hash(request.NewPassword), unlock: true, ct);
        await gateway.ConsumeRecoveryTokenAsync(tokenHash, ct);
    }

    public Task<FaceChallengeResult> CreateFaceLoginChallengeAsync(CancellationToken ct)
        => faceBiometrics.CreateLivenessChallengeAsync(ct);

    public async Task<AccessTokenResult> LoginByFaceAsync(
        string identifier,
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new AppValidationException("El identificador es obligatorio.");
        if (string.IsNullOrWhiteSpace(challengeId) || challengeId.Length > 200)
            throw new AppValidationException("El challengeId es obligatorio y debe ser válido.");

        FaceImageValidator.Validate(neutralImage);
        FaceImageValidator.Validate(challengeImage);

        var normalizedIdentifier = identifier.Trim();
        var buyer = await gateway.FindByIdentifierAsync(normalizedIdentifier, ct);
        if (buyer is null)
        {
            await gateway.RegisterFailedLoginAsync(null, normalizedIdentifier, "FACIAL", "Usuario no encontrado", ct);
            throw new AppUnauthorizedException("No fue posible validar el rostro.");
        }

        EnsureBuyerCanLogin(buyer);
        var enrollment = await enrollmentStore.GetActiveAsync(buyer.IdUsuario, ct);
        if (enrollment is null)
        {
            await gateway.RegisterFailedLoginAsync(buyer.IdUsuario, normalizedIdentifier, "FACIAL", "Sin enrolamiento facial", ct);
            throw new AppUnauthorizedException("No fue posible validar el rostro.");
        }

        var transientTemplate = await faceBiometrics.CreateTemplateAsync(
            enrollment.ReferenceImage,
            ct);

        var result = await faceBiometrics.VerifyTemplateLiveAsync(
            transientTemplate.BiometricTemplate.Value,
            challengeId.Trim(),
            neutralImage,
            challengeImage,
            ct);

        if (!result.AuthenticationPassed || !result.IsLive || result.IsMatch != true)
        {
            await gateway.RegisterFailedLoginAsync(
                buyer.IdUsuario,
                normalizedIdentifier,
                "FACIAL",
                result.LivenessReasonCode ?? result.Decision,
                ct);
            throw new AppUnauthorizedException("No fue posible validar el rostro.");
        }

        await gateway.RegisterSuccessfulLoginAsync(buyer.IdUsuario, normalizedIdentifier, "FACIAL", ct);
        return tokens.CreateBuyerToken(ToProfile(buyer));
    }

    private static BuyerProfile ToProfile(BuyerAuthRecord x) => new(
        x.IdUsuario, x.Correo, x.Telefono, x.FechaNacimiento, x.Nickname,
        x.NotificaEmail, x.NotificaWhatsApp, x.Activo, x.Bloqueado);

    private static void EnsureBuyerCanLogin(BuyerAuthRecord buyer)
    {
        if (!buyer.Activo) throw new AppForbiddenException("La cuenta está inactiva.");
        if (buyer.Bloqueado) throw new AppForbiddenException("La cuenta está bloqueada.");
    }

    private static string NormalizeEmail(string email)
    {
        var value = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 150 || !value.Contains('@'))
            throw new AppValidationException("El correo no es válido.");
        return value;
    }

    private static string CreateOpaqueToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

    private static bool IsLikelyUniqueViolation(Exception ex)
        => ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase);
}

public sealed class InternalAuthService(
    IInternalAuthRepository repository,
    IPasswordService passwords,
    ITokenService tokens)
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    public async Task<InternalSessionResult> LoginAsync(InternalLoginRequest request, string? ip, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new AppValidationException("Correo y contraseña son obligatorios.");

        var user = await repository.FindByEmailAsync(email, ct);
        if (user is null)
        {
            await repository.RegisterUnknownFailedLoginAsync(ip, ct);
            throw new AppUnauthorizedException();
        }

        if (!user.Activo)
        {
            await repository.RegisterRejectedLoginAsync(user.IdUsuarioInterno, "Cuenta desactivada.", ip, ct);
            throw new AppForbiddenException("La cuenta interna está desactivada.");
        }

        var now = DateTime.UtcNow;
        if (user.BloqueadoHasta is not null && user.BloqueadoHasta > now)
        {
            await repository.RegisterRejectedLoginAsync(user.IdUsuarioInterno, "Cuenta temporalmente bloqueada.", ip, ct);
            throw new AppForbiddenException("La cuenta está temporalmente bloqueada.");
        }

        var check = passwords.Verify(user.PasswordHash, request.Password);
        if (check is not (PasswordCheckResult.Success or PasswordCheckResult.SuccessRehashNeeded))
        {
            var attempts = user.IntentosFallidos + 1;
            DateTime? blockedUntil = attempts >= MaxFailedAttempts ? now.Add(Lockout) : null;
            await repository.RegisterFailedLoginAsync(user.IdUsuarioInterno, attempts, blockedUntil, ip, ct);
            throw new AppUnauthorizedException();
        }

        await repository.RegisterSuccessfulLoginAsync(user.IdUsuarioInterno, ip, ct);

        if (check == PasswordCheckResult.SuccessRehashNeeded)
            await repository.UpgradePasswordHashAsync(user.IdUsuarioInterno, passwords.Hash(request.Password), ct);

        return await CreateSessionAsync(user.IdUsuarioInterno, ct);
    }

    public async Task<InternalSessionResult> ChangePasswordAsync(
        int userId,
        ChangeInternalPasswordRequest request,
        string? ip,
        CancellationToken ct)
    {
        PasswordPolicy.Validate(request.NewPassword);

        var user = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        if (!user.Activo)
            throw new AppForbiddenException("La cuenta interna está desactivada.");

        var current = passwords.Verify(user.PasswordHash, request.CurrentPassword);
        if (current is not (PasswordCheckResult.Success or PasswordCheckResult.SuccessRehashNeeded))
            throw new AppUnauthorizedException("La contraseña actual no es correcta.");

        var samePassword = passwords.Verify(user.PasswordHash, request.NewPassword);
        if (samePassword is PasswordCheckResult.Success or PasswordCheckResult.SuccessRehashNeeded)
            throw new AppValidationException("La nueva contraseña debe ser diferente de la contraseña actual.");

        await repository.ChangePasswordAsync(userId, passwords.Hash(request.NewPassword), ip, ct);
        return await CreateSessionAsync(userId, ct);
    }

    public async Task<InternalUserInfo> GetCurrentUserAsync(int userId, CancellationToken ct)
    {
        var user = await repository.FindProfileByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        if (!user.IsActive)
            throw new AppForbiddenException("La cuenta interna está desactivada.");

        return user;
    }

    private async Task<InternalSessionResult> CreateSessionAsync(int userId, CancellationToken ct)
    {
        var auth = await repository.FindByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");
        var profile = await repository.FindProfileByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");
        var token = tokens.CreateInternalToken(auth);

        return new InternalSessionResult(
            token.AccessToken,
            token.ExpiresAtUtc,
            token.ActorType,
            token.MustChangePassword,
            profile);
    }

}
