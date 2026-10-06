using NextTech.Infrastructure.Payments;

namespace NextTech.UnitTests;

public sealed class RecurrenteCheckoutParserTests
{
    [Fact]
    public void TryParse_UsesTotalInCentsFromGetCheckout()
    {
        const string json = """
            {
              "id": "ch_paid",
              "status": "paid",
              "currency": "GTQ",
              "total_in_cents": 2000,
              "metadata": { "buyer_id": "92", "cart_id": "1013", "area_id": 1, "referencia": "Aula 12" }
            }
            """;

        var session = RecurrenteCheckoutParser.TryParse(json);

        Assert.NotNull(session);
        Assert.Equal("ch_paid", session.Id);
        Assert.Equal("paid", session.Status);
        Assert.Equal(2000, session.AmountInCents);
        Assert.Equal("92", session.Metadata["buyer_id"]);
        Assert.Equal("1", session.Metadata["area_id"]);
        Assert.Equal("Aula 12", session.Metadata["referencia"]);
    }

    [Fact]
    public void TryParse_KeepsCheckoutUrlFromCreate()
    {
        const string json = """
            {
              "id": "ch_new",
              "status": "unpaid",
              "checkout_url": "https://app.recurrente.com/checkout-session/ch_new",
              "amount_in_cents": 2000,
              "currency": "GTQ"
            }
            """;

        var session = RecurrenteCheckoutParser.TryParse(json);

        Assert.NotNull(session);
        Assert.Equal("https://app.recurrente.com/checkout-session/ch_new", session.CheckoutUrl);
        Assert.Equal(2000, session.AmountInCents);
    }
}
