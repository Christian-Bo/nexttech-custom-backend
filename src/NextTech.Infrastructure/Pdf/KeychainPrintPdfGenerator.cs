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

        var layout = KeychainPrintSheetLayout.Parse(data.Papel);
        if (!layout.CabeParDeLadosEnElAncho || !layout.CabeFilasEnElAlto)
        {
            throw new AppDependencyException(
                $"La hoja {layout.Codigo} no tiene espacio para el llavero de 1.6 pulgadas.");
        }

        try
        {
            var filas = data.Ordenes
                .SelectMany(orden => orden.Items.Select(item => item))
                .ToList();
            var porHoja = layout.LlaverosPorHoja;
            var diametroMm = KeychainPrintSheetLayout.DiametroMm;

            var pdf = Document.Create(document =>
            {
                for (var indice = 0; indice < filas.Count; indice += porHoja)
                {
                    var hoja = filas.Skip(indice).Take(porHoja).ToList();
                    document.Page(page => DibujarHoja(page, hoja, layout, diametroMm));
                }
            }).GeneratePdf();

            var sufijo = layout.Codigo;
            var nombre = data.Ordenes.Count == 1
                ? $"impresion-{data.Ordenes[0].CodigoOrden}-{sufijo}.pdf"
                : $"impresion-lote-{data.Ordenes.Count}-llaveros-{sufijo}.pdf";

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
        IReadOnlyList<KeychainPrintItem> hoja,
        KeychainPrintSheetLayout layout,
        float diametroMm)
    {
        page.Size(layout.AnchoHojaMm, layout.AltoHojaMm, Unit.Millimetre);
        page.Margin(layout.MargenMm, Unit.Millimetre);
        page.PageColor(Colors.White);

        page.Content().AlignCenter().AlignMiddle().Column(columna =>
        {
            columna.Spacing(layout.SeparacionFilaMm, Unit.Millimetre);
            foreach (var item in hoja)
            {
                columna.Item().AlignCenter().Row(fila =>
                {
                    fila.Spacing(layout.SeparacionLadosMm, Unit.Millimetre);
                    foreach (var zona in item.Zonas.Take(2))
                    {
                        fila.AutoItem()
                            .Width(diametroMm, Unit.Millimetre)
                            .Height(diametroMm, Unit.Millimetre)
                            .Border(0.4f)
                            .BorderColor(Colors.Grey.Medium)
                            .CornerRadius(diametroMm / 2f, Unit.Millimetre)
                            .Image(zona.Imagen)
                            .FitArea()
                            .UseOriginalImage();
                    }
                });
            }
        });
    }
}
