using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Orders;

namespace NextTech.Infrastructure.Realtime;

public sealed class NoOpOrderTrackingNotifier : IOrderTrackingNotifier
{
    public Task NotifyAsync(string codigoOrden, OrdenDetalleDto snapshot, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
