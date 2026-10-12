using NextTech.Application.Credentials;

namespace NextTech.Application.Interfaces;

public interface IBuyerCredentialCustomizationStore
{
    Task<CredentialCustomization?> GetAsync(long buyerId, CancellationToken ct);

    Task UpsertAsync(
        long buyerId,
        CredentialCustomization customization,
        CancellationToken ct);
}
