using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NextTech.Infrastructure.Pdf;

public sealed class PurchaseReceiptPdfGenerator : IPurchaseReceiptPdfGenerator
{
    public PurchaseReceiptDocument Generate(PurchaseReceiptPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (string.IsNullOrWhiteSpace(data.CodigoOrden))
        {
            throw new AppValidationException("El código de orden es obligatorio para la constancia.");
        }

        try
        {
            var qrImage = CreateQrPng(data.CodigoOrden.Trim());
            var pdf = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(28);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken3));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("NEXTTECH CUSTOM")
                            .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                        column.Item().AlignCenter().Text("Constancia de compra")
                            .FontSize(12).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Width(150).Height(150)
                            .Background(Colors.White)
                            .Padding(4)
                            .Image(qrImage)
                            .FitArea();

                        column.Item().AlignCenter().Text(data.CodigoOrden)
                            .SemiBold().FontSize(14);

                        column.Item().Text($"Comprador: {data.Nickname}");
                        column.Item().Text($"Área: {data.AreaEntrega}");
                        column.Item().Text($"Referencia: {data.ReferenciaEntrega}");
                        column.Item().Text($"Fecha: {data.FechaCreacion:yyyy-MM-dd HH:mm}");

                        column.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(def =>
                            {
                                def.RelativeColumn(3);
                                def.RelativeColumn(2);
                                def.ConstantColumn(36);
                                def.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Producto").SemiBold();
                                header.Cell().Text("Variante").SemiBold();
                                header.Cell().AlignRight().Text("Cant.").SemiBold();
                                header.Cell().AlignRight().Text("Subtotal").SemiBold();
                            });

                            foreach (var line in data.Items)
                            {
                                table.Cell().Text(line.NombreProducto);
                                table.Cell().Text(line.NombreVariante);
                                table.Cell().AlignRight().Text(line.Cantidad.ToString());
                                table.Cell().AlignRight().Text(line.Subtotal.ToString("0.00"));
                            }
                        });

                        column.Item().AlignRight().Text($"Total: {data.Total:0.00}")
                            .SemiBold().FontSize(12);

                        column.Item().PaddingTop(8).Text(
                            "El repartidor escanea este QR para localizar y entregar el pedido. Pago en efectivo al recibir.")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();

            return new PurchaseReceiptDocument(
                pdf,
                "application/pdf",
                $"constancia-{data.CodigoOrden}.pdf");
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible generar la constancia PDF de la compra.");
        }
    }

    private static byte[] CreateQrPng(string codigoOrden)
    {
        using var qrData = QRCodeGenerator.GenerateQrCode(codigoOrden, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(qrData);
        return qr.GetGraphic(10);
    }
}
