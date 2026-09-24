using System.Text;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Pdf;
using QuestPDF.Infrastructure;

namespace NextTech.UnitTests;

public sealed class PurchaseReceiptPdfGeneratorTests
{
    [Fact]
    public void Generate_CreatesReadablePdfWithOrderCode()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generator = new PurchaseReceiptPdfGenerator();

        var document = generator.Generate(new PurchaseReceiptPdfData(
            "ORD-TESTQR01",
            "tiendadev",
            "Edificio Central",
            "Entrada principal",
            20.00m,
            [
                new PurchaseReceiptLine("Llavero NFC", "Circular 1.6", 1, 20.00m)
            ],
            new DateTime(2026, 9, 23, 12, 0, 0)));

        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal("constancia-ORD-TESTQR01.pdf", document.FileName);
        Assert.True(document.Content.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(document.Content, 0, 4));
    }
}
