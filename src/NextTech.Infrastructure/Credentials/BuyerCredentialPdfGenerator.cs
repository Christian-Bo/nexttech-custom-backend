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
                    page.DefaultTextStyle(style => style.FontSize(11).FontColor(Colors.Grey.Darken3));

                    page.Content()
                        .AlignCenter()
                        .AlignMiddle()
                        .Width(390)
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten5)
                        .Padding(24)
                        .Column(column =>
                        {
                            column.Spacing(14);

                            column.Item()
                                .AlignCenter()
                                .Text("NEXTTECH CUSTOM")
                                .SemiBold()
                                .FontSize(22)
                                .FontColor(Colors.Blue.Darken2);

                            column.Item()
                                .AlignCenter()
                                .Text("Credencial digital de comprador")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .AlignCenter()
                                .Width(170)
                                .Height(170)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Background(Colors.White)
                                .Padding(4)
                                .Image(data.PortraitContent)
                                .FitArea();

                            column.Item().AlignCenter().Text(data.Nickname)
                                .SemiBold()
                                .FontSize(18);

                            column.Item().AlignCenter().Text(data.Role)
                                .SemiBold()
                                .FontSize(11)
                                .FontColor(Colors.Blue.Darken1);

                            column.Item()
                                .AlignCenter()
                                .Width(190)
                                .Height(190)
                                .Background(Colors.White)
                                .Padding(6)
                                .Image(qrImage)
                                .FitArea();

                            column.Item()
                                .AlignCenter()
                                .Text("Escanea este código para iniciar sesión. No compartas esta credencial.")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .PaddingTop(4)
                                .BorderTop(1)
                                .BorderColor(Colors.Grey.Lighten2)
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

            return new BuyerCredentialDocument(
                pdf,
                "application/pdf",
                $"nexttech-credential-{data.BuyerId}.pdf",
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

    private static byte[] CreateQrPng(string qrCredential)
    {
        using var qrData = QRCodeGenerator.GenerateQrCode(
            qrCredential,
            QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(qrData);
        return qr.GetGraphic(12);
    }
}
