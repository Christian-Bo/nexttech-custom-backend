using NextTech.Application.Face;

namespace NextTech.Application.Interfaces;

public interface IBuyerFaceEnrollmentStore
{
    Task<BuyerFaceEnrollment?> GetActiveAsync(long buyerId, CancellationToken ct);

    Task<BuyerFaceEnrollment> UpsertAsync(
        BuyerFaceEnrollment enrollment,
        CancellationToken ct);
}
