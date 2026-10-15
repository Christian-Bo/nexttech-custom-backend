using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Orders;
using NextTech.Domain.Exceptions;

namespace NextTech.Api.Realtime;

[Authorize(Policy = "BuyerOnly")]
public sealed class TrackingHub(
    ICurrentActor actor,
    IOrderService orders) : Hub
{
    public const string PedidoActualizado = "pedidoActualizado";

    public async Task UnirseAPedido(string codigoOrden)
    {
        if (string.IsNullOrWhiteSpace(codigoOrden))
        {
            throw new HubException("El código de la orden es obligatorio.");
        }

        var codigo = codigoOrden.Trim();
        var idComprador = actor.RequireIdCompradorExterno();

        try
        {
            _ = await orders.ObtenerSeguimientoAsync(idComprador, codigo, Context.ConnectionAborted);
        }
        catch (NotFoundException)
        {
            throw new HubException("La orden no existe.");
        }
        catch (ForbiddenException)
        {
            throw new HubException("No puedes seguir esta orden.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TrackingGroups.ForOrder(codigo));
    }
}
