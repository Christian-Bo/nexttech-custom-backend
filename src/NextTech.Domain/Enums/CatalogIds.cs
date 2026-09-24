namespace NextTech.Domain.Enums;

/// <summary>
/// IDs y códigos del seed oficial de NextTechCustomDB.
/// No usar números mágicos en Application ni en controllers.
/// </summary>
public enum RolId
{
    Admin = 1,
    Supervisor = 2,
    Repartidor = 3
}

public enum EstadoOrdenId
{
    OrdenGenerada = 1,
    EnElaboracion = 2,
    ListoParaEntrega = 3,
    EnEntrega = 4,
    Entregado = 5,
    CompradorNoEncontrado = 6
}

public enum EstadoCarritoId
{
    Activo = 1,
    Procesado = 2
}

public enum MetodoPagoId
{
    Efectivo = 1,
    Tarjeta = 2
}

public enum EstadoPagoId
{
    Pendiente = 1,
    Pagado = 2,
    Rechazado = 3
}

public enum ResultadoEntregaId
{
    Entregado = 1,
    CompradorNoEncontrado = 2,
    PagoNoRealizado = 3
}

public enum TipoActorId
{
    Sistema = 1,
    UsuarioInterno = 2,
    Comprador = 3
}

public enum TipoNotificacionId
{
    ConfirmacionCompra = 1,
    PedidoListo = 2,
    EntregaConfirmada = 3
}

public enum CanalNotificacionId
{
    Correo = 1,
    WhatsApp = 2
}

public enum EstadoNotificacionId
{
    Pendiente = 1,
    Enviada = 2,
    Fallida = 3
}

public static class RolCodigo
{
    public const string Admin = "ADMIN";
    public const string Supervisor = "SUPERVISOR";
    public const string Repartidor = "REPARTIDOR";
}

public static class ActorTipo
{
    public const string Comprador = "buyer";
    public const string Interno = "internal";
}

/// <summary>
/// Medidas físicas del llavero NFC del equipo.
/// El lienzo 800x800 del seed equivale a 500 DPI sobre 1.6 pulgadas.
/// </summary>
public static class LlaveroMedidas
{
    public const decimal DiametroPulgadas = 1.6m;
    public const decimal NfcPulgadas = 0.98m;
    public const int LienzoPx = 800;

    public static decimal DiametroMm => DiametroPulgadas * 25.4m;

    public static decimal NfcMm => NfcPulgadas * 25.4m;

    public static int NfcLienzoPx => (int)decimal.Round(NfcPulgadas / DiametroPulgadas * LienzoPx);
}
