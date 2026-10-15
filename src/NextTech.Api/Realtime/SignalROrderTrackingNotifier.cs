using Microsoft.AspNetCore.SignalR;
using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Orders;

namespace NextTech.Api.Realtime;

public sealed class SignalROrderTrackingNotifier(IHubContext<TrackingHub> hub) : IOrderTrackingNotifier
{
    public Task NotifyAsync(string codigoOrden, OrdenDetalleDto snapshot, CancellationToken cancellationToken)
        => hub.Clients
            .Group(TrackingGroups.ForOrder(codigoOrden))
            .SendAsync(TrackingHub.PedidoActualizado, snapshot, cancellationToken);
}
