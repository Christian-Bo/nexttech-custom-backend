namespace NextTech.Application.DTOs.Cart;

public sealed record AgregarItemCarritoRequest(
    int IdVariante,
    int Cantidad,
    int? IdPersonalizacion);

public sealed record CambiarCantidadRequest(int Cantidad);

public sealed record CarritoDto(
    int IdCarrito,
    string Estado,
    decimal Total,
    IReadOnlyList<DetalleCarritoDto> Items);

public sealed record DetalleCarritoDto(
    int IdDetalleCarrito,
    int IdVariante,
    string NombreProducto,
    string NombreVariante,
    int? IdPersonalizacion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);
