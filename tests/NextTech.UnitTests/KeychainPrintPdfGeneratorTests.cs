using System.Text;
using NextTech.Application.Interfaces;
using NextTech.Domain.Enums;
using NextTech.Infrastructure.Pdf;
using QuestPDF.Infrastructure;

namespace NextTech.UnitTests;

public sealed class KeychainPrintPdfGeneratorTests
{
    private static readonly byte[] PixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void Layout_LadoAyBCabenEnA4ConMargenDeImpresora()
    {
        Assert.Equal(1.6m, LlaveroMedidas.DiametroPulgadas);
        Assert.Equal(40.64m, LlaveroMedidas.DiametroMm);
        Assert.True(KeychainPrintSheetLayout.CabeParDeLadosEnElAncho);
        Assert.True(KeychainPrintSheetLayout.CabeFilasEnElAlto);
        Assert.True(KeychainPrintSheetLayout.AnchoUtilMm - KeychainPrintSheetLayout.AnchoParLadosMm >= 20f);
        Assert.True(KeychainPrintSheetLayout.AltoCuerpoMm - (KeychainPrintSheetLayout.LlaverosPorHoja * KeychainPrintSheetLayout.AltoFilaMm) >= 8f);
        Assert.True(KeychainPrintSheetLayout.SeparacionLadosMm >= 24f);
        Assert.True(KeychainPrintSheetLayout.DistanciaVerticalEntreCirculosMm >= 30f);
        Assert.Equal(3, KeychainPrintSheetLayout.LlaverosPorHoja);
    }

    [Fact]
    public void Generate_CreatesPdfForOneOrder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generator = new KeychainPrintPdfGenerator();

        var document = generator.Generate(new KeychainPrintPdfData(
        [
            new KeychainPrintOrder(
                "ORD-PRINT01",
                "allan",
                [
                    new KeychainPrintItem(
                        "Llavero NFC",
                        "Circular 1.6",
                        [
                            new KeychainPrintSide("Lado A", PixelPng),
                            new KeychainPrintSide("Lado B", PixelPng)
                        ])
                ])
        ]));

        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal("impresion-ORD-PRINT01.pdf", document.FileName);
        Assert.True(document.Content.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(document.Content, 0, 4));
    }

    [Fact]
    public void Generate_CreatesSinglePdfForSeveralKeychains()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generator = new KeychainPrintPdfGenerator();

        var document = generator.Generate(new KeychainPrintPdfData(
        [
            Pedido("ORD-A", "allan"),
            Pedido("ORD-B", "rodrigo"),
            Pedido("ORD-C", "maria"),
            Pedido("ORD-D", "jose")
        ]));

        Assert.Equal("impresion-lote-4-llaveros.pdf", document.FileName);
        Assert.True(document.Content.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(document.Content, 0, 4));
        Assert.Equal(3, KeychainPrintSheetLayout.LlaverosPorHoja);
    }

    private static KeychainPrintOrder Pedido(string codigo, string nickname)
        => new(
            codigo,
            nickname,
            [
                new KeychainPrintItem(
                    "Llavero NFC",
                    "Circular 1.6",
                    [
                        new KeychainPrintSide("Lado A", PixelPng),
                        new KeychainPrintSide("Lado B", PixelPng)
                    ])
            ]);
}
