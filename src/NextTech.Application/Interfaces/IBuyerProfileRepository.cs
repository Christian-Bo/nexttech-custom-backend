using NextTech.Application.Profiles;

namespace NextTech.Application.Interfaces;

public interface IBuyerProfileRepository
{
    Task<BuyerProfileData?> GetAsync(long buyerId, CancellationToken ct);

    Task<bool> IsIdentifierInUseByOtherAsync(
        string identifier,
        long buyerId,
        CancellationToken ct);

    Task UpdateAsync(
        long buyerId,
        BuyerProfileUpdateData data,
        CancellationToken ct);

    Task<BuyerDisplayPhoto?> GetDisplayPhotoAsync(long buyerId, CancellationToken ct);
}
