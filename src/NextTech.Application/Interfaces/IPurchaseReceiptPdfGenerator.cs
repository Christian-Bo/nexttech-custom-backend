namespace NextTech.Application.Interfaces;

public sealed record PurchaseReceiptLine(
    string NombreProducto,
    string NombreVariante,
    int Cantidad,
    decimal Subtotal);

public sealed record PurchaseReceiptPdfData(
    string CodigoOrden,
    string Nickname,
    string AreaEntrega,
    string ReferenciaEntrega,
    decimal Total,
    IReadOnlyList<PurchaseReceiptLine> Items,
    DateTime FechaCreacion);

public sealed record PurchaseReceiptDocument(
    byte[] Content,
    string ContentType,
    string FileName);

public interface IPurchaseReceiptPdfGenerator
{
    PurchaseReceiptDocument Generate(PurchaseReceiptPdfData data);
}
