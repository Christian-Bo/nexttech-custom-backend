using System.Text;
using NextTech.Application.Common;
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
    public void Layout_DiezPorQuinceEsElPapelPorDefectoYCabeUnLlavero()
    {
        var hoja = KeychainPrintSheetLayout.Parse(null);
        Assert.Equal(1.6m, LlaveroMedidas.DiametroPulgadas);
        Assert.Equal(40.64m, LlaveroMedidas.DiametroMm);
        Assert.Equal("10x15", hoja.Codigo);
        Assert.Equal(150f, hoja.AnchoHojaMm);
        Assert.Equal(100f, hoja.AltoHojaMm);
        Assert.True(hoja.CabeParDeLadosEnElAncho);
        Assert.True(hoja.CabeFilasEnElAlto);
        Assert.Equal(1, hoja.LlaverosPorHoja);
        Assert.Equal(10f, hoja.SeparacionLadosMm);
    }

    [Theory]
    [InlineData("13x18", "13x18")]
    [InlineData("5x7", "5x7")]
    [InlineData("a5", "a5")]
    [InlineData("letter", "letter")]
    [InlineData("carta", "letter")]
    [InlineData("8x10", "8x10")]
    public void Layout_PapelesComunes_MantienenSeparacionDeCorte(string papel, string codigo)
    {
        var hoja = KeychainPrintSheetLayout.Parse(papel);
        Assert.Equal(codigo, hoja.Codigo);
        Assert.Equal(KeychainPrintSheetLayout.SeparacionCorteMm, hoja.SeparacionLadosMm);
        Assert.Equal(KeychainPrintSheetLayout.SeparacionCorteMm, hoja.SeparacionFilaMm);
        Assert.True(hoja.CabeParDeLadosEnElAncho);
        Assert.True(hoja.CabeFilasEnElAlto);
        Assert.True(hoja.LlaverosPorHoja >= 1);
    }

    [Fact]
    public void Layout_A4EmpacaVariosLlaverosSinEncabezado()
    {
        var hoja = KeychainPrintSheetLayout.Parse("a4");
        Assert.Equal("a4", hoja.Codigo);
        Assert.True(hoja.CabeParDeLadosEnElAncho);
        Assert.True(hoja.CabeFilasEnElAlto);
        Assert.InRange(hoja.LlaverosPorHoja, 4, 8);
    }

    [Fact]
    public void Layout_PapelInvalido_Lanza400()
    {
        var error = Assert.Throws<AppValidationException>(() => KeychainPrintSheetLayout.Parse("oficio"));
        Assert.Contains("10x15", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_CreatesPdfForOneOrderOn10x15()
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
        Assert.Equal("impresion-ORD-PRINT01-10x15.pdf", document.FileName);
        Assert.True(document.Content.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(document.Content, 0, 4));
    }

    [Fact]
    public void Generate_CreatesSinglePdfForSeveralKeychainsOnA4()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generator = new KeychainPrintPdfGenerator();

        var document = generator.Generate(new KeychainPrintPdfData(
        [
            Pedido("ORD-A", "allan"),
            Pedido("ORD-B", "rodrigo"),
            Pedido("ORD-C", "maria"),
            Pedido("ORD-D", "jose")
        ], "a4"));

        Assert.Equal("impresion-lote-4-llaveros-a4.pdf", document.FileName);
        Assert.True(document.Content.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(document.Content, 0, 4));
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
