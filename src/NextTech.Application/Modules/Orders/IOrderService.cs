using NextTech.Application.DTOs.Orders;

namespace NextTech.Application.Modules.Orders;

public interface IOrderService
{
    Task<OrdenDetalleDto> CheckoutEfectivoAsync(
        long idCompradorExterno,
        string nickname,
        string correo,
        string? telefono,
        bool notificaEmail,
        bool notificaWhatsApp,
        CheckoutRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdenResumenDto>> ObtenerMisComprasAsync(
        long idCompradorExterno,
        CancellationToken cancellationToken);

    Task<OrdenDetalleDto> ObtenerSeguimientoAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task MarcarListoParaEntregaAsync(
        int idUsuarioInterno,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task TomarOrdenAsync(
        int idRepartidor,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task ConfirmarEntregaAsync(
        int idRepartidor,
        string codigoOrden,
        string fotoBase64,
        string nombreArchivo,
        string tipoMime,
        CancellationToken cancellationToken);

    Task RegistrarNoEncontradoAsync(
        int idRepartidor,
        string codigoOrden,
        string observacion,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdenColaDto>> ListarEnElaboracionAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdenColaDto>> ListarListosParaEntregaAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdenColaDto>> ListarDisponiblesEntregaAsync(
        CancellationToken cancellationToken);

    Task<OrdenEntregaDto> BuscarParaEntregaAsync(
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<ArchivoDescargaDto> ObtenerConstanciaAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<ArchivoDescargaDto> ObtenerQrConstanciaAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<OrdenProduccionDto> ObtenerParaProduccionAsync(
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<ArchivoDescargaDto> ObtenerArchivoProduccionAsync(
        string codigoOrden,
        int idArchivo,
        CancellationToken cancellationToken);

    Task<ArchivoDescargaDto> ObtenerArchivoProduccionPorIdAsync(
        int idArchivo,
        CancellationToken cancellationToken);

    Task RegistrarPagoNoRealizadoAsync(
        int idRepartidor,
        string codigoOrden,
        string observacion,
        CancellationToken cancellationToken);
}
