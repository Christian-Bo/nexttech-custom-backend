using System.Text.Json;
using NextTech.Application.Modules.Personalization;

namespace NextTech.UnitTests;

public sealed class NfcUrlJsonTests
{
    [Fact]
    public void Inyectar_AddsNfcUrlWithoutDroppingVisualFields()
    {
        const string json = """{"imagen":"demo","texto":"hola","stickers":["a"]}""";

        var resultado = NfcUrlJson.Inyectar(json, "https://open.spotify.com/playlist/abc");
        using var document = JsonDocument.Parse(resultado);
        var root = document.RootElement;

        Assert.Equal("https://open.spotify.com/playlist/abc", root.GetProperty("nfcUrl").GetString());
        Assert.Equal("demo", root.GetProperty("imagen").GetString());
        Assert.Equal("hola", root.GetProperty("texto").GetString());
        PersonalizationJsonValidator.Validar(resultado);
    }

    [Fact]
    public void ExtraerPrimera_ReadsInjectedUrl()
    {
        var zonaA = NfcUrlJson.Inyectar("""{"imagen":"a"}""", "https://www.youtube.com/watch?v=x");
        const string zonaB = """{"texto":"b"}""";

        var url = NfcUrlJson.ExtraerPrimera([zonaB, zonaA]);

        Assert.Equal("https://www.youtube.com/watch?v=x", url);
    }

    [Fact]
    public void ExtraerPrimera_ReturnsNullWhenMissing()
    {
        Assert.Null(NfcUrlJson.ExtraerPrimera(["""{"imagen":"a"}"""]));
    }
}
