namespace NextTech.Application.Modules.Orders;

public static class TrackingGroups
{
    public static string ForOrder(string codigoOrden)
        => $"order:{codigoOrden.Trim()}";
}
