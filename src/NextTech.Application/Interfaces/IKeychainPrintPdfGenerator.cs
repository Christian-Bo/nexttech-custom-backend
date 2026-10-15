namespace NextTech.Application.Interfaces;

public sealed record KeychainPrintSide(string Nombre, byte[] Imagen);

public sealed record KeychainPrintItem(
    string NombreProducto,
    string NombreVariante,
    IReadOnlyList<KeychainPrintSide> Zonas);

public sealed record KeychainPrintOrder(
    string CodigoOrden,
    string NicknameComprador,
    IReadOnlyList<KeychainPrintItem> Items);

public sealed record KeychainPrintPdfData(IReadOnlyList<KeychainPrintOrder> Ordenes);

public interface IKeychainPrintPdfGenerator
{
    PurchaseReceiptDocument Generate(KeychainPrintPdfData data);
}
