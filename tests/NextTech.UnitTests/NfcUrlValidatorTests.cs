using NextTech.Application.Modules.Personalization;
using NextTech.Domain.Exceptions;

namespace NextTech.UnitTests;

public sealed class NfcUrlValidatorTests
{
    [Theory]
    [InlineData("https://open.spotify.com/playlist/37i9dQZF1DX4WYDNvnm2LO")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.facebook.com/allan.profile")]
    public void Normalizar_AcceptsHttpsUrls(string url)
    {
        var normalizada = NfcUrlValidator.Normalizar(url);

        Assert.StartsWith("https://", normalizada, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("://", normalizada);
    }

    [Theory]
    [InlineData("www.google.com", "https://www.google.com/")]
    [InlineData("google.com", "https://google.com/")]
    [InlineData("http://www.google.com", "https://www.google.com/")]
    [InlineData("http://open.spotify.com/playlist/abc", "https://open.spotify.com/playlist/abc")]
    public void Normalizar_CompletaHttpsCuandoFalta(string entrada, string esperada)
    {
        var normalizada = NfcUrlValidator.Normalizar(entrada);

        Assert.Equal(esperada, normalizada);
    }

    [Fact]
    public void Normalizar_TrimsWhitespace()
    {
        var url = NfcUrlValidator.Normalizar("  https://open.spotify.com/playlist/abc  ");

        Assert.Equal("https://open.spotify.com/playlist/abc", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_EmptyIsOptional(string? url)
    {
        Assert.Null(NfcUrlValidator.Normalizar(url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://files.example/a")]
    [InlineData("https://user:pass@example.com/secret")]
    public void Normalizar_RejectsInvalidUrls(string? url)
    {
        var error = Assert.Throws<BusinessRuleException>(() => NfcUrlValidator.Normalizar(url));
        Assert.Equal(NfcUrlValidator.MensajeObligatoria, error.Message);
    }

    [Fact]
    public void Normalizar_RejectsTooLongUrl()
    {
        var larga = "https://open.spotify.com/playlist/" + new string('a', NfcUrlValidator.MaxLength);

        var error = Assert.Throws<BusinessRuleException>(() => NfcUrlValidator.Normalizar(larga));
        Assert.Equal(NfcUrlValidator.MensajeDemasiadoLarga, error.Message);
    }
}
