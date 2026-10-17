using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Orders;
using NextTech.Domain.Exceptions;

namespace NextTech.IntegrationTests;

/// <summary>
/// CI no tiene SQL Server. El redirect NFC no debe abrir la base para comprobar que es anónimo.
/// </summary>
internal sealed class IntegrationOrderServiceStub : IOrderService
{
    public Task<string> ObtenerUrlNfcDestinoAsync(string codigoOrden, CancellationToken cancellationToken)
        => Task.FromException<string>(new NotFoundException("La orden no existe."));

    public Task<OrdenDetalleDto> CheckoutEfectivoAsync(
        long idCompradorExterno,
        string nickname,
        string correo,
        string? telefono,
        bool notificaEmail,
        bool notificaWhatsApp,
        CheckoutRequest request,
        CancellationToken cancellationToken)
        => Unsupported<OrdenDetalleDto>();

    public Task<CheckoutTarjetaDto> IniciarCheckoutTarjetaAsync(
        long idCompradorExterno,
        string correo,
        string nickname,
        string? telefono,
        CheckoutRequest request,
        CancellationToken cancellationToken)
        => Unsupported<CheckoutTarjetaDto>();

    public Task<OrdenDetalleDto> ConfirmarCheckoutTarjetaAsync(
        long? idCompradorEsperado,
        string checkoutId,
        CancellationToken cancellationToken)
        => Unsupported<OrdenDetalleDto>();

    public Task<IReadOnlyList<OrdenResumenDto>> ObtenerMisComprasAsync(
        long idCompradorExterno,
        CancellationToken cancellationToken)
        => Unsupported<IReadOnlyList<OrdenResumenDto>>();

    public Task<OrdenDetalleDto> ObtenerSeguimientoAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported<OrdenDetalleDto>();

    public Task MarcarListoParaEntregaAsync(
        int idUsuarioInterno,
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported();

    public Task TomarOrdenAsync(
        int idRepartidor,
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported();

    public Task ConfirmarEntregaAsync(
        int idRepartidor,
        string codigoOrden,
        string fotoBase64,
        string nombreArchivo,
        string tipoMime,
        CancellationToken cancellationToken)
        => Unsupported();

    public Task RegistrarNoEncontradoAsync(
        int idRepartidor,
        string codigoOrden,
        string observacion,
        CancellationToken cancellationToken)
        => Unsupported();

    public Task<IReadOnlyList<OrdenColaDto>> ListarEnElaboracionAsync(CancellationToken cancellationToken)
        => Unsupported<IReadOnlyList<OrdenColaDto>>();

    public Task<IReadOnlyList<OrdenColaDto>> ListarListosParaEntregaAsync(CancellationToken cancellationToken)
        => Unsupported<IReadOnlyList<OrdenColaDto>>();

    public Task<IReadOnlyList<OrdenColaDto>> ListarDisponiblesEntregaAsync(
        int idRepartidor,
        CancellationToken cancellationToken)
        => Unsupported<IReadOnlyList<OrdenColaDto>>();

    public Task<OrdenEntregaDto> BuscarParaEntregaAsync(
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported<OrdenEntregaDto>();

    public Task<ArchivoDescargaDto> ObtenerConstanciaAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported<ArchivoDescargaDto>();

    public Task<ArchivoDescargaDto> ObtenerQrConstanciaAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported<ArchivoDescargaDto>();

    public Task<OrdenProduccionDto> ObtenerParaProduccionAsync(
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported<OrdenProduccionDto>();

    public Task<ArchivoDescargaDto> ObtenerHojaImpresionAsync(
        string codigoOrden,
        CancellationToken cancellationToken)
        => Unsupported<ArchivoDescargaDto>();

    public Task<ArchivoDescargaDto> ObtenerHojaImpresionLoteAsync(
        IReadOnlyList<string>? codigos,
        CancellationToken cancellationToken)
        => Unsupported<ArchivoDescargaDto>();

    public Task<ArchivoDescargaDto> ObtenerArchivoProduccionAsync(
        string codigoOrden,
        int idArchivo,
        CancellationToken cancellationToken)
        => Unsupported<ArchivoDescargaDto>();

    public Task<ArchivoDescargaDto> ObtenerArchivoProduccionPorIdAsync(
        int idArchivo,
        CancellationToken cancellationToken)
        => Unsupported<ArchivoDescargaDto>();

    public Task RegistrarPagoNoRealizadoAsync(
        int idRepartidor,
        string codigoOrden,
        string observacion,
        CancellationToken cancellationToken)
        => Unsupported();

    private static Task<T> Unsupported<T>()
        => Task.FromException<T>(new InvalidOperationException("IOrderService está aislado en tests de integración."));

    private static Task Unsupported()
        => Task.FromException(new InvalidOperationException("IOrderService está aislado en tests de integración."));
}
