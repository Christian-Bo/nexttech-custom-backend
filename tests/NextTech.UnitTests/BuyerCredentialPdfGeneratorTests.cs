using System.Text;
using NextTech.Application.Credentials;
using NextTech.Infrastructure.Credentials;
using QuestPDF.Infrastructure;

namespace NextTech.UnitTests;

public sealed class BuyerCredentialPdfGeneratorTests
{
    [Fact]
    public void Generate_CreatesReadablePdfWithExpectedMetadata()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generator = new BuyerCredentialPdfGenerator();
        var portrait = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAIAAAACUFjqAAAAFUlEQVR4nGP88OEDA27AhEduBEsDAG+/AuRcfbvbAAAAAElFTkSuQmCC");

        var document = generator.Generate(new BuyerCredentialPdfData(
            21,
            "buyer",
            "COMPRADOR",
            portrait,
            "image/png",
            "sample-qr-credential",
            new DateTimeOffset(2026, 9, 21, 18, 30, 0, TimeSpan.Zero)));

        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal("nexttech-credential-21.pdf", document.FileName);
        Assert.True(document.Content.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(document.Content, 0, 4));
    }
}
