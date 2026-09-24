using NextTech.Application.Common;
using QRCoder;

namespace NextTech.Infrastructure.QR;

/// <summary>
/// QR de entrega: el payload es el CodigoOrden (el mismo valor que el PDF y el scanner del repartidor).
/// </summary>
public static class OrderQrPng
{
    public const string ContentType = "image/png";

    public static byte[] Create(string codigoOrden)
    {
        if (string.IsNullOrWhiteSpace(codigoOrden))
        {
            throw new AppValidationException("El código de orden es obligatorio para el QR.");
        }

        try
        {
            using var qrData = QRCodeGenerator.GenerateQrCode(codigoOrden.Trim(), QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(qrData);
            return qr.GetGraphic(10);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible generar el QR de la orden.");
        }
    }

    public static string FileName(string codigoOrden)
        => $"qr-{codigoOrden.Trim()}.png";
}
