using NextTech.Application.Face;

namespace NextTech.Application.Interfaces;

public interface IBuyerBiometricStore
{
    Task<BuyerBiometricCredential?> GetActiveAsync(long buyerId, CancellationToken ct);

    Task<BuyerBiometricCredential> UpsertAsync(
        BuyerBiometricCredential credential,
        CancellationToken ct);
}
