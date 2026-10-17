using NextTech.Domain.Enums;

namespace NextTech.Infrastructure.Pdf;

/// <summary>
/// Hoja A4 con círculo de 1.6 in y holgura de corte entre llaveros
/// para no recortar el de al lado. Papel normal o fotográfico A4.
/// </summary>
public static class KeychainPrintSheetLayout
{
    public const float AnchoHojaMm = 210f;
    public const float AltoHojaMm = 297f;
    public const float MargenImpresoraMm = 12f;
    public const float AltoEncabezadoMm = 38f;
    public const float AltoPieMm = 8f;
    public const float SeparacionLadosMm = 26f;
    public const float PaddingCajaMm = 4f;
    public const float AltoEtiquetaMm = 5f;
    public const float AltoMetaMm = 8f;
    public const float SeparacionFilaMm = 11f;

    public static float DiametroMm => (float)LlaveroMedidas.DiametroMm;

    public static float AnchoUtilMm => AnchoHojaMm - (2f * MargenImpresoraMm);

    public static float AltoUtilMm => AltoHojaMm - (2f * MargenImpresoraMm);

    public static float AltoCuerpoMm => AltoUtilMm - AltoEncabezadoMm - AltoPieMm;

    public static float AltoFilaMm =>
        PaddingCajaMm
        + AltoEtiquetaMm
        + DiametroMm
        + AltoMetaMm
        + PaddingCajaMm
        + SeparacionFilaMm;

    public static float AnchoParLadosMm => (2f * DiametroMm) + SeparacionLadosMm;

    public static float DistanciaVerticalEntreCirculosMm =>
        AltoMetaMm + PaddingCajaMm + SeparacionFilaMm + PaddingCajaMm + AltoEtiquetaMm;

    public static int LlaverosPorHoja
    {
        get
        {
            var cantidad = (int)Math.Floor(AltoCuerpoMm / AltoFilaMm);
            return Math.Max(1, cantidad);
        }
    }

    public static bool CabeParDeLadosEnElAncho => AnchoParLadosMm <= AnchoUtilMm;

    public static bool CabeFilasEnElAlto => (LlaverosPorHoja * AltoFilaMm) <= AltoCuerpoMm;
}
