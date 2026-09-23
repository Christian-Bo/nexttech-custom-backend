using NextTech.Application.DTOs.Cart;

namespace NextTech.Application.Modules.Cart;

public interface ICartService
{
    Task<CarritoDto> ObtenerActivoAsync(
        long idCompradorExterno,
        CancellationToken cancellationToken);

    Task<CarritoDto> AgregarItemAsync(
        long idCompradorExterno,
        AgregarItemCarritoRequest request,
        CancellationToken cancellationToken);

    Task<CarritoDto> CambiarCantidadAsync(
        long idCompradorExterno,
        int idDetalleCarrito,
        int cantidad,
        CancellationToken cancellationToken);

    Task<CarritoDto> EliminarItemAsync(
        long idCompradorExterno,
        int idDetalleCarrito,
        CancellationToken cancellationToken);
}
