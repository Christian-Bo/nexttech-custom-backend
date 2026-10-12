using NextTech.Application.Common;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NextTech.Infrastructure.Credentials;

public sealed class BuyerCredentialPdfGenerator : IBuyerCredentialPdfGenerator
{
    public BuyerCredentialDocument Generate(BuyerCredentialPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.PortraitContent.Length == 0)
            throw new AppValidationException("La fotografía de la credencial no puede estar vacía.");
        if (string.IsNullOrWhiteSpace(data.QrCredential))
            throw new AppValidationException("La credencial QR no puede estar vacía.");

        var customization = data.Customization ?? BuyerCredentialDesignCatalog.Default;
        var theme = ResolveTheme(customization.TemplateId);

        try
        {
            var qrImage = CreateQrPng(data.QrCredential);
            var pdf = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(style => style.FontSize(11).FontColor(theme.BodyText));

                    page.Content()
                        .AlignCenter()
                        .AlignMiddle()
                        .Width(410)
                        .Border(1)
                        .BorderColor(theme.Border)
                        .Background(theme.Background)
                        .Padding(24)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            if (data.IsPreview)
                            {
                                column.Item()
                                    .AlignCenter()
                                    .Text("VISTA PREVIA · QR NO ACTIVO")
                                    .SemiBold()
                                    .FontSize(10)
                                    .FontColor(Colors.Red.Darken1);
                            }

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(StickerText(customization, "TOP_LEFT"))
                                    .SemiBold().FontSize(9).FontColor(theme.Accent);
                                row.RelativeItem().AlignRight().Text(StickerText(customization, "TOP_RIGHT"))
                                    .SemiBold().FontSize(9).FontColor(theme.Accent);
                            });

                            column.Item()
                                .AlignCenter()
                                .Text("NEXTTECH CUSTOM")
                                .SemiBold()
                                .FontSize(22)
                                .FontColor(theme.Primary);

                            column.Item()
                                .AlignCenter()
                                .Text("Credencial digital de comprador")
                                .FontSize(12)
                                .FontColor(theme.SecondaryText);

                            column.Item()
                                .AlignCenter()
                                .Width(176)
                                .Height(176)
                                .Border(2)
                                .BorderColor(theme.Accent)
                                .Background(Colors.White)
                                .Padding(5)
                                .Image(data.PortraitContent)
                                .FitArea();

                            column.Item().AlignCenter().Text(data.Nickname)
                                .SemiBold()
                                .FontSize(18)
                                .FontColor(theme.BodyText);

                            column.Item().AlignCenter().Text(data.Role)
                                .SemiBold()
                                .FontSize(11)
                                .FontColor(theme.Primary);

                            column.Item()
                                .AlignCenter()
                                .Width(188)
                                .Height(188)
                                .Background(Colors.White)
                                .Border(1)
                                .BorderColor(theme.Border)
                                .Padding(6)
                                .Image(qrImage)
                                .FitArea();

                            column.Item()
                                .AlignCenter()
                                .Text(data.IsPreview
                                    ? "Este QR pertenece únicamente a la vista previa y no permite iniciar sesión."
                                    : "Escanea este código para iniciar sesión. No compartas esta credencial.")
                                .FontSize(9)
                                .FontColor(theme.SecondaryText);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(StickerText(customization, "BOTTOM_LEFT"))
                                    .SemiBold().FontSize(9).FontColor(theme.Accent);
                                row.RelativeItem().AlignRight().Text(StickerText(customization, "BOTTOM_RIGHT"))
                                    .SemiBold().FontSize(9).FontColor(theme.Accent);
                            });

                            column.Item()
                                .PaddingTop(3)
                                .BorderTop(1)
                                .BorderColor(theme.Border)
                                .PaddingTop(10)
                                .Row(row =>
                                {
                                    row.RelativeItem().Text($"ID comprador: {data.BuyerId}").FontSize(9);
                                    row.RelativeItem().AlignRight()
                                        .Text($"Emitida: {data.IssuedAtUtc:yyyy-MM-dd HH:mm} UTC")
                                        .FontSize(9);
                                });
                        });
                });
            }).GeneratePdf();

            var filePrefix = data.IsPreview ? "nexttech-credential-preview" : "nexttech-credential";
            return new BuyerCredentialDocument(
                pdf,
                "application/pdf",
                $"{filePrefix}-{data.BuyerId}.pdf",
                data.IssuedAtUtc);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible renderizar la credencial PDF.");
        }
    }

    private static CredentialTheme ResolveTheme(string? templateId)
        => templateId?.Trim().ToUpperInvariant() switch
        {
            "NEXTTECH_DARK" => new CredentialTheme(
                Colors.Grey.Darken4,
                Colors.Cyan.Lighten1,
                Colors.Cyan.Darken1,
                Colors.White,
                Colors.Grey.Lighten2,
                Colors.Grey.Darken1),
            "NEXTTECH_PURPLE" => new CredentialTheme(
                Colors.Purple.Lighten5,
                Colors.Purple.Darken2,
                Colors.Purple.Medium,
                Colors.Grey.Darken4,
                Colors.Grey.Darken1,
                Colors.Purple.Lighten2),
            _ => new CredentialTheme(
                Colors.Blue.Lighten5,
                Colors.Blue.Darken2,
                Colors.Blue.Medium,
                Colors.Grey.Darken4,
                Colors.Grey.Darken1,
                Colors.Blue.Lighten3)
        };

    private static string StickerText(CredentialCustomization customization, string slot)
    {
        var sticker = customization.Stickers.FirstOrDefault(
            x => string.Equals(x.Slot, slot, StringComparison.Ordinal));

        return sticker?.StickerId switch
        {
            "TECH" => "[ TECH ]",
            "GAMER" => "[ GAMER ]",
            "CODE" => "[ CODE ]",
            "STAR" => "[ STAR ]",
            _ => " "
        };
    }

    private static byte[] CreateQrPng(string qrCredential)
    {
        using var qrData = QRCodeGenerator.GenerateQrCode(
            qrCredential,
            QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(qrData);
        return qr.GetGraphic(12);
    }

    private sealed record CredentialTheme(
        string Background,
        string Primary,
        string Accent,
        string BodyText,
        string SecondaryText,
        string Border);
}
