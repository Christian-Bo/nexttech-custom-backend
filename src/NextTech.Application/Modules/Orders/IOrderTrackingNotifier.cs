using NextTech.Application.DTOs.Orders;

namespace NextTech.Application.Modules.Orders;

public interface IOrderTrackingNotifier
{
    Task NotifyAsync(string codigoOrden, OrdenDetalleDto snapshot, CancellationToken cancellationToken);
}
