using System.Security.Cryptography;
using System.Text;
using NextTech.Infrastructure.Payments;

namespace NextTech.UnitTests;

public sealed class RecurrenteWebhookSignatureTests
{
    [Fact]
    public void IsValid_AcceptsMatchingSignature()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var secret = "whsec_" + Convert.ToBase64String(key);
        var id = "msg_test";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var body = "{\"checkout\":{\"id\":\"ch_test\"}}";
        var signed = $"{id}.{timestamp}.{body}";
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signed));
        var header = "v1," + Convert.ToBase64String(hash);

        Assert.True(RecurrenteWebhookSignature.IsValid(
            body,
            id,
            timestamp,
            header,
            secret,
            TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void IsValid_RejectsTamperedBody()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var secret = "whsec_" + Convert.ToBase64String(key);
        var id = "msg_test";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var body = "{\"checkout\":{\"id\":\"ch_test\"}}";
        var signed = $"{id}.{timestamp}.{body}";
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signed));
        var header = "v1," + Convert.ToBase64String(hash);

        Assert.False(RecurrenteWebhookSignature.IsValid(
            "{\"checkout\":{\"id\":\"ch_other\"}}",
            id,
            timestamp,
            header,
            secret,
            TimeSpan.FromMinutes(5)));
    }
}
