using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Nfc;

namespace NextTech.UnitTests;

public sealed class NfcLinksTests
{
    [Fact]
    public void WriteUrl_BuildsShortChipLink()
    {
        var url = NfcLinks.WriteUrl("https", "nexttech.136-113-48-173.sslip.io", "ORD-656E6098", tieneDestino: true);

        Assert.Equal("https://nexttech.136-113-48-173.sslip.io/n/ORD-656E6098", url);
    }

    [Fact]
    public void WriteUrl_ReturnsNullWhenThereIsNoDestination()
    {
        Assert.Null(NfcLinks.WriteUrl("https", "example.test", "ORD-1", tieneDestino: false));
    }

    [Fact]
    public void Mapper_FillsWriteUrlOnOrderAndItems()
    {
        var dto = new OrdenProduccionDto(
            "ORD-656E6098",
            DateTime.UtcNow,
            20m,
            "En elaboración",
            "Campus",
            "Aula 1",
            "allan",
            null,
            [
                new ItemProduccionDto("Llavero", "Circular", 1, "{}", 9, [], "https://open.spotify.com/playlist/abc", null)
            ],
            "https://open.spotify.com/playlist/abc",
            null);

        var mapped = NfcWriteUrlMapper.Apply(dto, "https", "nexttech.136-113-48-173.sslip.io");

        Assert.Equal("https://nexttech.136-113-48-173.sslip.io/n/ORD-656E6098", mapped.NfcWriteUrl);
        Assert.Equal(mapped.NfcWriteUrl, mapped.Items[0].NfcWriteUrl);
        Assert.Equal("https://open.spotify.com/playlist/abc", mapped.NfcUrl);
    }
}
