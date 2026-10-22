using NextTech.Application.Common;
using NextTech.Domain.Enums;

namespace NextTech.Infrastructure.Pdf;

/// <summary>
/// Hoja de impresión: solo círculos de 1.6 in, sin textos.
/// Separación mínima de corte 10 mm entre A/B y entre llaveros.
/// </summary>
public sealed class KeychainPrintSheetLayout
{
    public const string PapelPorDefecto = "10x15";
    public const float SeparacionCorteMm = 10f;
    public const string PapelesSoportados = "10x15, 13x18, 15x21, 20x25, 4x6, 5x7, 8x10, a6, a5, a4, letter";

    private static readonly IReadOnlyDictionary<string, (string Codigo, float AnchoMm, float AltoMm, float MargenMm)> Papeles =
        new Dictionary<string, (string, float, float, float)>(StringComparer.OrdinalIgnoreCase)
        {
            ["10x15"] = ("10x15", 150f, 100f, 4f),
            ["15x10"] = ("10x15", 150f, 100f, 4f),
            ["4x6"] = ("4x6", 152.4f, 101.6f, 4f),
            ["6x4"] = ("4x6", 152.4f, 101.6f, 4f),
            ["13x18"] = ("13x18", 180f, 130f, 5f),
            ["18x13"] = ("13x18", 180f, 130f, 5f),
            ["5x7"] = ("5x7", 177.8f, 127f, 5f),
            ["7x5"] = ("5x7", 177.8f, 127f, 5f),
            ["15x21"] = ("15x21", 210f, 150f, 6f),
            ["21x15"] = ("15x21", 210f, 150f, 6f),
            ["20x25"] = ("20x25", 250f, 200f, 8f),
            ["25x20"] = ("20x25", 250f, 200f, 8f),
            ["8x10"] = ("8x10", 254f, 203.2f, 8f),
            ["10x8"] = ("8x10", 254f, 203.2f, 8f),
            ["a6"] = ("a6", 105f, 148f, 4f),
            ["a5"] = ("a5", 148f, 210f, 6f),
            ["a4"] = ("a4", 210f, 297f, 8f),
            ["letter"] = ("letter", 215.9f, 279.4f, 8f),
            ["carta"] = ("letter", 215.9f, 279.4f, 8f)
        };

    private KeychainPrintSheetLayout(
        string codigo,
        float anchoHojaMm,
        float altoHojaMm,
        float margenMm)
    {
        Codigo = codigo;
        AnchoHojaMm = anchoHojaMm;
        AltoHojaMm = altoHojaMm;
        MargenMm = margenMm;

        var diametro = DiametroMm;
        AnchoUtilMm = AnchoHojaMm - (2f * MargenMm);
        AltoUtilMm = AltoHojaMm - (2f * MargenMm);
        SeparacionLadosMm = SeparacionCorteMm;
        SeparacionFilaMm = SeparacionCorteMm;

        if ((2f * diametro) + SeparacionLadosMm > AnchoUtilMm + 0.01f)
        {
            throw new AppValidationException(
                $"El papel {codigo} es demasiado estrecho para Lado A y Lado B de 1.6 in con 1 cm de corte.");
        }

        const float holguraMm = 4f;
        var cantidad = 1;
        while (true)
        {
            var siguiente = cantidad + 1;
            var altoNecesario = (siguiente * diametro) + ((siguiente - 1) * SeparacionFilaMm);
            if (altoNecesario + holguraMm <= AltoUtilMm)
            {
                cantidad = siguiente;
                continue;
            }

            break;
        }

        LlaverosPorHoja = cantidad;
    }

    public string Codigo { get; }

    public float AnchoHojaMm { get; }

    public float AltoHojaMm { get; }

    public float MargenMm { get; }

    public float AnchoUtilMm { get; }

    public float AltoUtilMm { get; }

    public float SeparacionLadosMm { get; }

    public float SeparacionFilaMm { get; }

    public int LlaverosPorHoja { get; }

    public static float DiametroMm => (float)LlaveroMedidas.DiametroMm;

    public float AnchoParLadosMm => (2f * DiametroMm) + SeparacionLadosMm;

    public bool CabeParDeLadosEnElAncho => AnchoParLadosMm <= AnchoUtilMm;

    public bool CabeFilasEnElAlto
    {
        get
        {
            var alto = (LlaverosPorHoja * DiametroMm)
                + (Math.Max(0, LlaverosPorHoja - 1) * SeparacionFilaMm);
            return alto <= AltoUtilMm;
        }
    }

    public static KeychainPrintSheetLayout Parse(string? papel)
    {
        var clave = Normalizar(papel);
        if (!Papeles.TryGetValue(clave, out var spec))
        {
            throw new AppValidationException($"Papel no soportado. Usa {PapelesSoportados}.");
        }

        return new KeychainPrintSheetLayout(spec.Codigo, spec.AnchoMm, spec.AltoMm, spec.MargenMm);
    }

    private static string Normalizar(string? papel)
    {
        if (string.IsNullOrWhiteSpace(papel))
        {
            return PapelPorDefecto;
        }

        return papel.Trim()
            .ToLowerInvariant()
            .Replace('×', 'x')
            .Replace(" ", string.Empty)
            .Replace("cm", string.Empty)
            .Replace("in", string.Empty);
    }
}
