namespace NextTech.Application.Modules.Nfc;

public static class NfcLinks
{
    public static string PathFor(string codigoOrden)
        => $"/n/{Uri.EscapeDataString(codigoOrden)}";

    public static string? WriteUrl(string scheme, string host, string codigoOrden, bool tieneDestino)
    {
        if (!tieneDestino || string.IsNullOrWhiteSpace(scheme) || string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        return $"{scheme}://{host.Trim().TrimEnd('/')}{PathFor(codigoOrden)}";
    }
}
