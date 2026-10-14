using NextTech.Application.DTOs.Orders;

namespace NextTech.Application.Modules.Nfc;

public static class NfcWriteUrlMapper
{
    public static OrdenProduccionDto Apply(OrdenProduccionDto dto, string scheme, string host)
    {
        var writeUrl = NfcLinks.WriteUrl(
            scheme,
            host,
            dto.CodigoOrden,
            !string.IsNullOrWhiteSpace(dto.NfcUrl) || dto.Items.Any(item => !string.IsNullOrWhiteSpace(item.NfcUrl)));

        var items = dto.Items
            .Select(item => item with
            {
                NfcWriteUrl = string.IsNullOrWhiteSpace(item.NfcUrl) ? null : writeUrl
            })
            .ToList();

        return dto with
        {
            Items = items,
            NfcWriteUrl = string.IsNullOrWhiteSpace(dto.NfcUrl) ? null : writeUrl
        };
    }
}
