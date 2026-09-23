using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Face;

namespace NextTech.UnitTests;

public sealed class FaceApplicationServiceTests
{
    [Fact]
    public async Task Enroll_PersistsPhotosOnlyAfterSuccessfulLiveVerification()
    {
        var gateway = new StubGateway(ActiveBuyer());
        var biometrics = new StubBiometrics(authenticated: true);
        var store = new StubStore();
        var service = new FaceApplicationService(gateway, biometrics, store);

        var result = await service.EnrollBuyerAsync(
            21,
            "challenge-1",
            Image("neutral.jpg"),
            Image("challenge.jpg"),
            CancellationToken.None);

        Assert.True(result.Enrolled);
        Assert.NotNull(store.Stored);
        Assert.Equal(21, store.Stored!.BuyerId);
        Assert.Equal("neutral.jpg", store.Stored.ReferenceImage.FileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, store.Stored.PortraitContent);
        Assert.Equal("1", result.TemplateVersion);
        Assert.Equal("SFace", result.Model);
    }

    [Fact]
    public async Task Enroll_DoesNotPersistWhenLivenessOrIdentityFails()
    {
        var gateway = new StubGateway(ActiveBuyer());
        var biometrics = new StubBiometrics(authenticated: false);
        var store = new StubStore();
        var service = new FaceApplicationService(gateway, biometrics, store);

        await Assert.ThrowsAsync<AppUnprocessableException>(() => service.EnrollBuyerAsync(
            21,
            "challenge-1",
            Image("neutral.jpg"),
            Image("challenge.jpg"),
            CancellationToken.None));

        Assert.Null(store.Stored);
    }

    [Fact]
    public async Task Verify_RebuildsTransientTemplateFromStoredReferenceImage()
    {
        var storedReference = Image("stored-reference.jpg");
        var store = new StubStore(new BuyerFaceEnrollment(
            21,
            storedReference,
            [1, 2, 3],
            "image/png",
            DateTimeOffset.UtcNow));
        var biometrics = new StubBiometrics(authenticated: true);
        var service = new FaceApplicationService(new StubGateway(ActiveBuyer()), biometrics, store);

        var result = await service.VerifyBuyerAsync(
            21,
            "challenge-1",
            Image("live-neutral.jpg"),
            Image("live-challenge.jpg"),
            CancellationToken.None);

        Assert.True(result.AuthenticationPassed);
        Assert.Single(biometrics.TemplateSourceImages);
        Assert.Equal("stored-reference.jpg", biometrics.TemplateSourceImages[0].FileName);
        Assert.Equal("protected-template", biometrics.LastVerificationTemplate);
    }

    private static BuyerAuthRecord ActiveBuyer() => new(
        21,
        "buyer@example.test",
        null,
        null,
        "buyer",
        "hash",
        true,
        false,
        true,
        false,
        0,
        null,
        null);

    private static FaceImage Image(string name)
        => new([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3], "image/jpeg", name);

    private sealed class StubBiometrics(bool authenticated) : IFaceBiometricService
    {
        public List<FaceImage> TemplateSourceImages { get; } = [];
        public string? LastVerificationTemplate { get; private set; }

        public Task<FaceChallengeResult> CreateLivenessChallengeAsync(CancellationToken ct)
            => Task.FromResult(new FaceChallengeResult(
                "challenge-1", "TURN_IMAGE_RIGHT", "Turn right", DateTimeOffset.UtcNow.AddMinutes(1), 60));

        public Task<ProtectedFaceEnrollmentResult> CreateTemplateAsync(FaceImage image, CancellationToken ct)
        {
            TemplateSourceImages.Add(image);
            return Task.FromResult(new ProtectedFaceEnrollmentResult(
                new ProtectedBiometricTemplate("protected-template", "1", "prod-v1", "SFace", 128, "ABC"),
                new ProcessedFacePortrait([1, 2, 3], "image/png", 600, 800, "transparent"),
                0.84m,
                "ACCEPTED",
                0,
                "ok"));
        }

        public Task<FaceVerificationResult> VerifyTemplateLiveAsync(
            string biometricTemplate,
            string challengeId,
            FaceImage neutralImage,
            FaceImage challengeImage,
            CancellationToken ct)
        {
            LastVerificationTemplate = biometricTemplate;
            return Task.FromResult(new FaceVerificationResult(
                authenticated,
                authenticated ? "AUTHENTICATED" : "LIVENESS_FAILED",
                authenticated,
                authenticated ? "LIVE" : "NOT_LIVE",
                authenticated ? "LIVENESS_PASSED" : "LIVENESS_ACTION_NOT_SATISFIED",
                "TURN_IMAGE_RIGHT",
                0.2m,
                0.18m,
                0.8m,
                0.3m,
                authenticated,
                authenticated ? 0.9m : null,
                0.393m,
                authenticated ? 0.5m : null,
                null));
        }

        public Task<FaceSegmentationResult> SegmentForCardAsync(FaceImage image, CancellationToken ct)
            => Task.FromResult(new FaceSegmentationResult([1], "image/png", "portrait.png"));
    }

    private sealed class StubStore(BuyerFaceEnrollment? initial = null) : IBuyerFaceEnrollmentStore
    {
        public BuyerFaceEnrollment? Stored { get; private set; } = initial;

        public Task<BuyerFaceEnrollment?> GetActiveAsync(long buyerId, CancellationToken ct)
            => Task.FromResult(Stored?.BuyerId == buyerId ? Stored : null);

        public Task<BuyerFaceEnrollment> UpsertAsync(
            BuyerFaceEnrollment enrollment,
            CancellationToken ct)
        {
            Stored = enrollment;
            return Task.FromResult(enrollment);
        }
    }

    private sealed class StubGateway(BuyerAuthRecord buyer) : ICentralIdentityGateway
    {
        public Task<BuyerAuthRecord?> FindByIdentifierAsync(string identifier, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(buyer);

        public Task<BuyerAuthRecord?> FindByIdAsync(long idUsuario, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(idUsuario == buyer.IdUsuario ? buyer : null);

        public Task<BuyerAuthRecord?> FindByQrHashAsync(string qrHash, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(null);

        public Task<long> RegisterAsync(BuyerRegistrationData data, CancellationToken ct) => throw new NotSupportedException();
        public Task SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct) => throw new NotSupportedException();
        public Task RegisterFailedLoginAsync(long? idUsuario, string identifier, string method, string reason, CancellationToken ct) => Task.CompletedTask;
        public Task RegisterSuccessfulLoginAsync(long idUsuario, string identifier, string method, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> VerifyLegacyPasswordAsync(string identifier, string password, CancellationToken ct) => Task.FromResult(false);
        public Task SetQrHashAsync(long idUsuario, string qrHash, CancellationToken ct) => throw new NotSupportedException();
        public Task CreateRecoveryTokenAsync(long idUsuario, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct) => throw new NotSupportedException();
        public Task<long?> FindValidRecoveryUserAsync(string tokenHash, CancellationToken ct) => Task.FromResult<long?>(null);
        public Task ConsumeRecoveryTokenAsync(string tokenHash, CancellationToken ct) => throw new NotSupportedException();
    }
}
