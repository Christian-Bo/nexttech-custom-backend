namespace NextTech.Application.Payments;

public sealed record RecurrenteCheckoutSession(
    string Id,
    string CheckoutUrl,
    string Status,
    int AmountInCents,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata);

public interface IRecurrenteCheckoutClient
{
    bool Enabled { get; }

    Task<RecurrenteCheckoutSession> CreateCheckoutAsync(
        RecurrenteCheckoutCreateRequest request,
        CancellationToken cancellationToken);

    Task<RecurrenteCheckoutSession> GetCheckoutAsync(
        string checkoutId,
        CancellationToken cancellationToken);
}

public sealed record RecurrenteCheckoutCreateRequest(
    string CustomerEmail,
    string ItemName,
    int AmountInCents,
    IReadOnlyDictionary<string, string> Metadata);
