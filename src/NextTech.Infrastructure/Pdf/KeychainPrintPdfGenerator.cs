using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NextTech.Infrastructure.Pdf;

public sealed class KeychainPrintPdfGenerator : IKeychainPrintPdfGenerator
{
    public PurchaseReceiptDocument Generate(KeychainPrintPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Ordenes.Count == 0 || data.Ordenes.All(orden => orden.Items.Count == 0))
        {
            throw new AppValidationException("No hay diseños de Lado A y Lado B para imprimir.");
        }

        if (!KeychainPrintSheetLayout.CabeParDeLadosEnElAncho || !KeychainPrintSheetLayout.CabeFilasEnElAlto)
        {
            throw new AppDependencyException("La hoja A4 no tiene espacio para el llavero de 1.6 pulgadas.");
        }

        try
        {
            var filas = data.Ordenes
                .SelectMany(orden => orden.Items.Select(item => (orden, item)))
                .ToList();
            var porHoja = KeychainPrintSheetLayout.LlaverosPorHoja;
            var diametroMm = KeychainPrintSheetLayout.DiametroMm;

            var pdf = Document.Create(document =>
            {
                for (var indice = 0; indice < filas.Count; indice += porHoja)
                {
                    var hoja = filas.Skip(indice).Take(porHoja).ToList();
                    document.Page(page => DibujarHoja(page, hoja, diametroMm));
                }
            }).GeneratePdf();

            var nombre = data.Ordenes.Count == 1
                ? $"impresion-{data.Ordenes[0].CodigoOrden}.pdf"
                : $"impresion-lote-{data.Ordenes.Count}-llaveros.pdf";

            return new PurchaseReceiptDocument(pdf, "application/pdf", nombre);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible generar la hoja de impresión del llavero.");
        }
    }

    private static void DibujarHoja(
        PageDescriptor page,
        IReadOnlyList<(KeychainPrintOrder Orden, KeychainPrintItem Item)> hoja,
        float diametroMm)
    {
        page.Size(PageSizes.A4);
        page.Margin(KeychainPrintSheetLayout.MargenImpresoraMm, Unit.Millimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken3));

        page.Header().Height(KeychainPrintSheetLayout.AltoEncabezadoMm, Unit.Millimetre)
            .Element(encabezado => DibujarEncabezado(encabezado, diametroMm));

        page.Footer().Height(KeychainPrintSheetLayout.AltoPieMm, Unit.Millimetre)
            .AlignCenter().AlignMiddle().Text(texto =>
            {
                texto.Span("Página ");
                texto.CurrentPageNumber();
                texto.Span(" de ");
                texto.TotalPages();
                texto.Span("  ·  Cortar solo el círculo  ·  Si se corta mal, reimprimir ese código");
            });

        page.Content().Column(columna =>
        {
            foreach (var (orden, item) in hoja)
            {
                columna.Item().Height(KeychainPrintSheetLayout.AltoFilaMm, Unit.Millimetre)
                    .Element(fila => DibujarLlavero(fila, orden, item, diametroMm));
            }
        });
    }

    private static void DibujarEncabezado(IContainer contenedor, float diametroMm)
    {
        contenedor.Column(columna =>
        {
            columna.Item().AlignCenter().Text("NEXTTECH CUSTOM — impresión de llaveros")
                .SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
            columna.Item().AlignCenter().Text(
                    "Papel A4 · 100% / tamaño real · no ajustar a la página · no cortar el llavero de al lado")
                .FontSize(8);
            columna.Item().AlignCenter().Text(
                    "Si un corte sale mal, vuelve a imprimir solo ese código. Las imágenes se conservan.")
                .FontSize(8);
            columna.Item().PaddingTop(3).AlignCenter()
                .Width(diametroMm, Unit.Millimetre)
                .Height(3, Unit.Millimetre)
                .Background(Colors.Grey.Darken3);
            columna.Item().AlignCenter().Text(
                    "Esta barra debe medir 4.06 cm (1.6 in). Si no, la impresora está escalando.")
                .FontSize(8).SemiBold();
        });
    }

    private static void DibujarLlavero(
        IContainer contenedor,
        KeychainPrintOrder orden,
        KeychainPrintItem item,
        float diametroMm)
    {
        contenedor.Column(columna =>
        {
            columna.Item()
                .Border(0.6f)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(KeychainPrintSheetLayout.PaddingCajaMm, Unit.Millimetre)
                .Column(caja =>
                {
                    caja.Item().AlignCenter().Row(fila =>
                    {
                        fila.Spacing(KeychainPrintSheetLayout.SeparacionLadosMm, Unit.Millimetre);
                        foreach (var zona in item.Zonas.Take(2))
                        {
                            fila.AutoItem().Width(diametroMm, Unit.Millimetre).Column(lado =>
                            {
                                lado.Item()
                                    .Height(KeychainPrintSheetLayout.AltoEtiquetaMm, Unit.Millimetre)
                                    .AlignCenter().AlignMiddle()
                                    .Text(zona.Nombre).SemiBold().FontSize(10);
                                lado.Item()
                                    .Width(diametroMm, Unit.Millimetre)
                                    .Height(diametroMm, Unit.Millimetre)
                                    .Border(0.7f)
                                    .BorderColor(Colors.Grey.Darken2)
                                    .CornerRadius(diametroMm / 2f, Unit.Millimetre)
                                    .Image(zona.Imagen)
                                    .FitArea();
                            });
                        }
                    });

                    caja.Item()
                        .Height(KeychainPrintSheetLayout.AltoMetaMm, Unit.Millimetre)
                        .AlignCenter().AlignMiddle()
                        .Text($"{orden.CodigoOrden}  ·  {orden.NicknameComprador}  ·  reimprimir este código si se corta mal")
                        .FontSize(8);
                });

            columna.Item()
                .Height(KeychainPrintSheetLayout.SeparacionFilaMm, Unit.Millimetre)
                .AlignCenter().AlignMiddle()
                .Text("— espacio de corte: no hay otro llavero aquí —")
                .FontSize(7)
                .FontColor(Colors.Grey.Medium);
        });
    }
}
