using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextTech.Application.Common.Files;
using NextTech.Application.DTOs.Orders;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Orders;
using NextTech.Domain.Enums;
using NextTech.Domain.Exceptions;
using NextTech.Infrastructure.Persistence.SqlServer;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Store;

public sealed class OrderService(
    NextTechDbContext db,
    IPurchaseReceiptPdfGenerator receiptPdf,
    IOrderMailSender mail) : IOrderService
{
    public async Task<OrdenDetalleDto> CheckoutEfectivoAsync(
        long idCompradorExterno,
        string nickname,
        string correo,
        string? telefono,
        bool notificaEmail,
        bool notificaWhatsApp,
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenciaEntrega))
        {
            throw new BusinessRuleException("La referencia de entrega es obligatoria.");
        }

        if (!string.Equals(request.MetodoPago, "EFECTIVO", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                "Por ahora el checkout de negocio solo admite efectivo. La tarjeta la integra el módulo de pagos.");
        }

        var carrito = await db.Carrito
            .FirstOrDefaultAsync(
                c => c.IdCompradorExterno == idCompradorExterno
                     && c.IdEstadoCarrito == (int)EstadoCarritoId.Activo,
                cancellationToken);

        if (carrito is null)
        {
            throw new NotFoundException("No hay un carrito activo para checkout.");
        }

        var detalles = await db.DetalleCarrito
            .Where(d => d.IdCarrito == carrito.IdCarrito)
            .ToListAsync(cancellationToken);

        if (detalles.Count == 0)
        {
            throw new BusinessRuleException("El carrito está vacío.");
        }

        var area = await db.AreaEntrega
            .FirstOrDefaultAsync(
                a => a.IdAreaEntrega == request.IdAreaEntrega && a.Activo,
                cancellationToken);

        if (area is null)
        {
            throw new NotFoundException("El área de entrega no está disponible.");
        }

        var lineas = new List<(DetalleCarrito Detalle, Producto Producto, VarianteProducto Variante, string AtributosJson)>();

        foreach (var detalle in detalles)
        {
            var variante = await (
                from v in db.VarianteProducto
                join p in db.Producto on v.IdProducto equals p.IdProducto
                join c in db.Categoria on p.IdCategoria equals c.IdCategoria
                where v.IdVariante == detalle.IdVariante && v.Activo && p.Activo && c.Activo
                select new { Variante = v, Producto = p })
                .FirstOrDefaultAsync(cancellationToken);

            if (variante is null)
            {
                throw new BusinessRuleException("Hay un artículo del carrito que ya no está disponible.");
            }

            if (variante.Producto.PermitePersonalizacion)
            {
                if (detalle.IdPersonalizacion is null)
                {
                    throw new BusinessRuleException("Falta la personalización de un artículo.");
                }

                var personalizacion = await db.Personalizacion
                    .FirstAsync(p => p.IdPersonalizacion == detalle.IdPersonalizacion, cancellationToken);

                if (personalizacion.Bloqueada || personalizacion.IdVariante != detalle.IdVariante)
                {
                    throw new BusinessRuleException("La personalización no es válida para checkout.");
                }

                var zonasObligatorias = await db.ZonaPersonalizacion
                    .Where(z => z.IdProducto == variante.Producto.IdProducto && z.Activo && z.EsObligatoria)
                    .Select(z => z.IdZona)
                    .ToListAsync(cancellationToken);

                var zonasGuardadas = await db.PersonalizacionZona
                    .Where(z => z.IdPersonalizacion == personalizacion.IdPersonalizacion)
                    .Select(z => z.IdZona)
                    .ToListAsync(cancellationToken);

                if (zonasObligatorias.Except(zonasGuardadas).Any())
                {
                    throw new BusinessRuleException("La personalización no cubre Lado A y Lado B.");
                }
            }

            var atributos = await (
                from va in db.VarianteAtributo
                join a in db.Atributo on va.IdAtributo equals a.IdAtributo
                join valor in db.ValorAtributo on va.IdValorAtributo equals valor.IdValorAtributo
                where va.IdVariante == variante.Variante.IdVariante
                select new { a.Nombre, valor.Valor })
                .ToListAsync(cancellationToken);

            var atributosJson = JsonSerializer.Serialize(
                atributos.ToDictionary(a => a.Nombre, a => a.Valor));

            lineas.Add((detalle, variante.Producto, variante.Variante, atributosJson));
        }

        var ahora = DateTime.Now;
        decimal total = 0m;
        var lineasConPrecio = new List<(DetalleCarrito Detalle, Producto Producto, VarianteProducto Variante, string AtributosJson, decimal Unitario, decimal Subtotal)>();
        foreach (var linea in lineas)
        {
            var unitario = linea.Producto.PrecioBase + linea.Variante.PrecioAdicional;
            var subtotal = unitario * linea.Detalle.Cantidad;
            total += subtotal;
            lineasConPrecio.Add((linea.Detalle, linea.Producto, linea.Variante, linea.AtributosJson, unitario, subtotal));
        }

        if (total <= 0)
        {
            throw new BusinessRuleException("El total de la orden debe ser mayor que cero.");
        }

        var codigoOrden = OrderCodeGenerator.Nuevo();
        var nicknameAplicado = Truncar(nickname, 50);
        var correoAplicado = Truncar(correo, 200);
        var referencia = request.ReferenciaEntrega.Trim();
        var receipt = receiptPdf.Generate(new PurchaseReceiptPdfData(
            codigoOrden,
            nicknameAplicado,
            area.Nombre,
            referencia,
            total,
            lineasConPrecio
                .Select(linea => new PurchaseReceiptLine(
                    linea.Producto.Nombre,
                    linea.Variante.Nombre,
                    linea.Detalle.Cantidad,
                    linea.Subtotal))
                .ToList(),
            ahora));

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var constancia = new Archivo
            {
                NombreOriginal = receipt.FileName,
                TipoMime = receipt.ContentType,
                Extension = ".pdf",
                Datos = receipt.Content,
                TamanoBytes = receipt.Content.Length,
                FechaCreacion = ahora
            };
            db.Archivo.Add(constancia);
            await db.SaveChangesAsync(cancellationToken);

            var orden = new Orden
            {
                CodigoOrden = codigoOrden,
                IdCompradorExterno = idCompradorExterno,
                NicknameCompradorAplicado = nicknameAplicado,
                CorreoCompradorAplicado = correoAplicado,
                TelefonoCompradorAplicado = string.IsNullOrWhiteSpace(telefono)
                    ? null
                    : Truncar(telefono, 30),
                IdCarritoOrigen = carrito.IdCarrito,
                IdAreaEntrega = area.IdAreaEntrega,
                NombreAreaAplicado = area.Nombre,
                ReferenciaEntrega = referencia,
                IdEstadoOrdenActual = (int)EstadoOrdenId.OrdenGenerada,
                Total = total,
                IdArchivoConstancia = constancia.IdArchivo,
                FechaCreacion = ahora
            };

            db.Orden.Add(orden);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var linea in lineasConPrecio)
            {
                db.DetalleOrden.Add(new DetalleOrden
                {
                    IdOrden = orden.IdOrden,
                    IdVariante = linea.Variante.IdVariante,
                    IdPersonalizacion = linea.Detalle.IdPersonalizacion,
                    Cantidad = linea.Detalle.Cantidad,
                    NombreProductoAplicado = linea.Producto.Nombre,
                    NombreVarianteAplicada = linea.Variante.Nombre,
                    AtributosAplicadosJson = linea.AtributosJson,
                    PrecioBaseAplicado = linea.Producto.PrecioBase,
                    PrecioVarianteAplicado = linea.Variante.PrecioAdicional,
                    PrecioUnitario = linea.Unitario,
                    Subtotal = linea.Subtotal
                });

                if (linea.Detalle.IdPersonalizacion is not null)
                {
                    var personalizacion = await db.Personalizacion
                        .FirstAsync(
                            p => p.IdPersonalizacion == linea.Detalle.IdPersonalizacion,
                            cancellationToken);
                    personalizacion.Bloqueada = true;
                    personalizacion.FechaActualizacion = ahora;
                }
            }

            db.Pago.Add(new Pago
            {
                IdOrden = orden.IdOrden,
                IdMetodoPago = (int)MetodoPagoId.Efectivo,
                IdEstadoPago = (int)EstadoPagoId.Pendiente,
                Monto = total,
                FechaCreacion = ahora
            });

            db.HistorialEstadoOrden.Add(CrearHistorial(
                orden.IdOrden,
                EstadoOrdenId.OrdenGenerada,
                TipoActorId.Comprador,
                idUsuarioInterno: null,
                idCompradorExterno,
                ahora,
                "Checkout en efectivo"));

            orden.IdEstadoOrdenActual = (int)EstadoOrdenId.EnElaboracion;
            orden.FechaActualizacion = ahora;

            db.HistorialEstadoOrden.Add(CrearHistorial(
                orden.IdOrden,
                EstadoOrdenId.EnElaboracion,
                TipoActorId.Sistema,
                idUsuarioInterno: null,
                idCompradorExterno: null,
                ahora,
                "Elaboración iniciada"));

            EncolarNotificaciones(
                orden.IdOrden,
                TipoNotificacionId.ConfirmacionCompra,
                notificaEmail,
                notificaWhatsApp,
                correo,
                telefono,
                ahora);

            db.BitacoraAuditoria.Add(CrearBitacora(
                TipoActorId.Comprador,
                idUsuarioInterno: null,
                idCompradorExterno,
                "CHECKOUT_EFECTIVO",
                "Orden",
                orden.IdOrden,
                "EXITOSO",
                ahora,
                orden.CodigoOrden));

            carrito.IdEstadoCarrito = (int)EstadoCarritoId.Procesado;
            carrito.FechaActualizacion = ahora;
            carrito.UltimaActividad = ahora;

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await IntentarEnviarCorreoAsync(
                orden.IdOrden,
                TipoNotificacionId.ConfirmacionCompra,
                ct => mail.SendPurchaseConfirmationAsync(
                    correoAplicado,
                    nicknameAplicado,
                    orden.CodigoOrden,
                    receipt.Content,
                    ct),
                cancellationToken);

            return await ObtenerSeguimientoAsync(
                idCompradorExterno,
                orden.CodigoOrden,
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new ConflictException("No se pudo generar la orden. Revisa si el carrito ya fue procesado.");
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<OrdenResumenDto>> ObtenerMisComprasAsync(
        long idCompradorExterno,
        CancellationToken cancellationToken)
    {
        var ordenes = await db.Orden.AsNoTracking()
            .Where(o => o.IdCompradorExterno == idCompradorExterno)
            .OrderByDescending(o => o.FechaCreacion)
            .ToListAsync(cancellationToken);

        var estados = await db.EstadoOrden.AsNoTracking().ToListAsync(cancellationToken);
        var nombres = estados.ToDictionary(e => e.IdEstadoOrden, e => e.Nombre);

        return ordenes
            .Select(o => new OrdenResumenDto(
                o.CodigoOrden,
                o.FechaCreacion,
                o.Total,
                nombres[o.IdEstadoOrdenActual]))
            .ToList();
    }

    public async Task<OrdenDetalleDto> ObtenerSeguimientoAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.CodigoOrden == codigoOrden && o.IdCompradorExterno == idCompradorExterno,
                cancellationToken);

        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        return await MapearOrdenAsync(orden, cancellationToken);
    }

    public Task<IReadOnlyList<OrdenColaDto>> ListarEnElaboracionAsync(
        CancellationToken cancellationToken)
    {
        return ListarColaAsync(
            (int)EstadoOrdenId.EnElaboracion,
            soloSinRepartidor: false,
            cancellationToken);
    }

    public Task<IReadOnlyList<OrdenColaDto>> ListarListosParaEntregaAsync(
        CancellationToken cancellationToken)
    {
        return ListarColaAsync(
            (int)EstadoOrdenId.ListoParaEntrega,
            soloSinRepartidor: false,
            cancellationToken);
    }

    public Task<IReadOnlyList<OrdenColaDto>> ListarDisponiblesEntregaAsync(
        CancellationToken cancellationToken)
    {
        return ListarColaAsync(
            (int)EstadoOrdenId.ListoParaEntrega,
            soloSinRepartidor: true,
            cancellationToken);
    }

    public async Task<OrdenEntregaDto> BuscarParaEntregaAsync(
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var codigo = codigoOrden.Trim();
        var orden = await db.Orden.AsNoTracking()
            .FirstOrDefaultAsync(o => o.CodigoOrden == codigo, cancellationToken);

        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        var estado = await db.EstadoOrden.AsNoTracking()
            .FirstAsync(e => e.IdEstadoOrden == orden.IdEstadoOrdenActual, cancellationToken);

        var pago = await (
            from p in db.Pago.AsNoTracking()
            join e in db.EstadoPago.AsNoTracking() on p.IdEstadoPago equals e.IdEstadoPago
            where p.IdOrden == orden.IdOrden
            select e.Nombre)
            .FirstAsync(cancellationToken);

        return new OrdenEntregaDto(
            orden.CodigoOrden,
            orden.FechaCreacion,
            orden.Total,
            estado.Nombre,
            orden.NombreAreaAplicado,
            orden.ReferenciaEntrega,
            orden.NicknameCompradorAplicado,
            pago);
    }

    public async Task MarcarListoParaEntregaAsync(
        int idUsuarioInterno,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.FirstOrDefaultAsync(o => o.CodigoOrden == codigoOrden, cancellationToken);
        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        if (orden.IdEstadoOrdenActual != (int)EstadoOrdenId.EnElaboracion)
        {
            throw new BusinessRuleException("Solo se puede marcar listo un pedido en elaboración.");
        }

        var ahora = DateTime.Now;
        orden.IdEstadoOrdenActual = (int)EstadoOrdenId.ListoParaEntrega;
        orden.FechaActualizacion = ahora;

        db.HistorialEstadoOrden.Add(CrearHistorial(
            orden.IdOrden,
            EstadoOrdenId.ListoParaEntrega,
            TipoActorId.UsuarioInterno,
            idUsuarioInterno,
            idCompradorExterno: null,
            ahora,
            "Pedido listo para entrega"));

        EncolarNotificaciones(
            orden.IdOrden,
            TipoNotificacionId.PedidoListo,
            notificaEmail: true,
            notificaWhatsApp: !string.IsNullOrWhiteSpace(orden.TelefonoCompradorAplicado),
            orden.CorreoCompradorAplicado,
            orden.TelefonoCompradorAplicado,
            ahora);

        db.BitacoraAuditoria.Add(CrearBitacora(
            TipoActorId.UsuarioInterno,
            idUsuarioInterno,
            idCompradorExterno: null,
            "ORDEN_LISTA",
            "Orden",
            orden.IdOrden,
            "EXITOSO",
            ahora,
            orden.CodigoOrden));

        await db.SaveChangesAsync(cancellationToken);

        await IntentarEnviarCorreoAsync(
            orden.IdOrden,
            TipoNotificacionId.PedidoListo,
            ct => mail.SendOrderReadyAsync(
                orden.CorreoCompradorAplicado,
                orden.NicknameCompradorAplicado,
                orden.CodigoOrden,
                ct),
            cancellationToken);
    }

    public async Task TomarOrdenAsync(
        int idRepartidor,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.FirstOrDefaultAsync(o => o.CodigoOrden == codigoOrden, cancellationToken);
        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        var ahora = DateTime.Now;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var filas = await db.Orden
                .Where(o => o.IdOrden == orden.IdOrden
                            && o.IdEstadoOrdenActual == (int)EstadoOrdenId.ListoParaEntrega
                            && o.IdRepartidorAsignado == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(o => o.IdRepartidorAsignado, idRepartidor)
                        .SetProperty(o => o.IdEstadoOrdenActual, (int)EstadoOrdenId.EnEntrega)
                        .SetProperty(o => o.FechaActualizacion, ahora),
                    cancellationToken);

            if (filas == 0)
            {
                throw new ConflictException("La orden ya fue tomada o no está lista para entrega.");
            }

            db.IntentoEntrega.Add(new IntentoEntrega
            {
                IdOrden = orden.IdOrden,
                IdRepartidor = idRepartidor,
                FechaHoraInicio = ahora
            });

            db.HistorialEstadoOrden.Add(CrearHistorial(
                orden.IdOrden,
                EstadoOrdenId.EnEntrega,
                TipoActorId.UsuarioInterno,
                idRepartidor,
                idCompradorExterno: null,
                ahora,
                "Orden tomada por repartidor"));

            db.BitacoraAuditoria.Add(CrearBitacora(
                TipoActorId.UsuarioInterno,
                idRepartidor,
                idCompradorExterno: null,
                "ORDEN_TOMADA",
                "Orden",
                orden.IdOrden,
                "EXITOSO",
                ahora,
                orden.CodigoOrden));

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ConfirmarEntregaAsync(
        int idRepartidor,
        string codigoOrden,
        string fotoBase64,
        string nombreArchivo,
        string tipoMime,
        CancellationToken cancellationToken)
    {
        var orden = await ObtenerOrdenDeRepartidorAsync(idRepartidor, codigoOrden, cancellationToken);
        var intento = await ObtenerIntentoAbiertoAsync(orden.IdOrden, idRepartidor, cancellationToken);
        var pago = await db.Pago.FirstAsync(p => p.IdOrden == orden.IdOrden, cancellationToken);

        var imagen = ImagePayloadParser.Parse(fotoBase64, nombreArchivo, tipoMime);
        var ahora = DateTime.Now;

        var archivo = new Archivo
        {
            NombreOriginal = string.IsNullOrWhiteSpace(nombreArchivo)
                ? $"entrega-{orden.CodigoOrden}{imagen.Extension}"
                : Path.GetFileName(nombreArchivo),
            TipoMime = imagen.Mime,
            Extension = imagen.Extension,
            Datos = imagen.Datos,
            TamanoBytes = imagen.Datos.Length,
            FechaCreacion = ahora
        };

        db.Archivo.Add(archivo);
        await db.SaveChangesAsync(cancellationToken);

        if (pago.IdMetodoPago == (int)MetodoPagoId.Efectivo)
        {
            pago.IdEstadoPago = (int)EstadoPagoId.Pagado;
            pago.FechaPago = ahora;
        }

        intento.FechaHoraFin = ahora;
        intento.IdResultadoEntrega = (int)ResultadoEntregaId.Entregado;
        intento.IdArchivoFotoEntrega = archivo.IdArchivo;

        orden.IdEstadoOrdenActual = (int)EstadoOrdenId.Entregado;
        orden.QrUtilizado = true;
        orden.FechaUsoQr = ahora;
        orden.FechaActualizacion = ahora;

        db.HistorialEstadoOrden.Add(CrearHistorial(
            orden.IdOrden,
            EstadoOrdenId.Entregado,
            TipoActorId.UsuarioInterno,
            idRepartidor,
            idCompradorExterno: null,
            ahora,
            "Entrega confirmada"));

        EncolarNotificaciones(
            orden.IdOrden,
            TipoNotificacionId.EntregaConfirmada,
            notificaEmail: true,
            notificaWhatsApp: !string.IsNullOrWhiteSpace(orden.TelefonoCompradorAplicado),
            orden.CorreoCompradorAplicado,
            orden.TelefonoCompradorAplicado,
            ahora);

        db.BitacoraAuditoria.Add(CrearBitacora(
            TipoActorId.UsuarioInterno,
            idRepartidor,
            idCompradorExterno: null,
            "ENTREGA_CONFIRMADA",
            "Orden",
            orden.IdOrden,
            "EXITOSO",
            ahora,
            orden.CodigoOrden));

        await db.SaveChangesAsync(cancellationToken);

        await IntentarEnviarCorreoAsync(
            orden.IdOrden,
            TipoNotificacionId.EntregaConfirmada,
            ct => mail.SendDeliveryConfirmedAsync(
                orden.CorreoCompradorAplicado,
                orden.NicknameCompradorAplicado,
                orden.CodigoOrden,
                ct),
            cancellationToken);
    }

    public async Task RegistrarNoEncontradoAsync(
        int idRepartidor,
        string codigoOrden,
        string observacion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            throw new BusinessRuleException("La observación es obligatoria si el comprador no fue encontrado.");
        }

        var orden = await ObtenerOrdenDeRepartidorAsync(idRepartidor, codigoOrden, cancellationToken);
        var intento = await ObtenerIntentoAbiertoAsync(orden.IdOrden, idRepartidor, cancellationToken);
        var ahora = DateTime.Now;

        intento.FechaHoraFin = ahora;
        intento.IdResultadoEntrega = (int)ResultadoEntregaId.CompradorNoEncontrado;
        intento.Observacion = observacion.Trim();

        orden.IdEstadoOrdenActual = (int)EstadoOrdenId.CompradorNoEncontrado;
        orden.IdRepartidorAsignado = null;
        orden.FechaActualizacion = ahora;

        db.HistorialEstadoOrden.Add(CrearHistorial(
            orden.IdOrden,
            EstadoOrdenId.CompradorNoEncontrado,
            TipoActorId.UsuarioInterno,
            idRepartidor,
            idCompradorExterno: null,
            ahora,
            observacion.Trim()));

        orden.IdEstadoOrdenActual = (int)EstadoOrdenId.ListoParaEntrega;
        db.HistorialEstadoOrden.Add(CrearHistorial(
            orden.IdOrden,
            EstadoOrdenId.ListoParaEntrega,
            TipoActorId.Sistema,
            idUsuarioInterno: null,
            idCompradorExterno: null,
            ahora,
            "Orden liberada para reintento de entrega"));

        db.BitacoraAuditoria.Add(CrearBitacora(
            TipoActorId.UsuarioInterno,
            idRepartidor,
            idCompradorExterno: null,
            "COMPRADOR_NO_ENCONTRADO",
            "Orden",
            orden.IdOrden,
            "EXITOSO",
            ahora,
            orden.CodigoOrden));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RegistrarPagoNoRealizadoAsync(
        int idRepartidor,
        string codigoOrden,
        string observacion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            throw new BusinessRuleException("La observación es obligatoria si el pago no se realizó.");
        }

        var orden = await ObtenerOrdenDeRepartidorAsync(idRepartidor, codigoOrden, cancellationToken);
        var intento = await ObtenerIntentoAbiertoAsync(orden.IdOrden, idRepartidor, cancellationToken);
        var ahora = DateTime.Now;
        var motivo = observacion.Trim();

        intento.FechaHoraFin = ahora;
        intento.IdResultadoEntrega = (int)ResultadoEntregaId.PagoNoRealizado;
        intento.Observacion = motivo;

        orden.IdRepartidorAsignado = null;
        orden.IdEstadoOrdenActual = (int)EstadoOrdenId.ListoParaEntrega;
        orden.FechaActualizacion = ahora;

        db.HistorialEstadoOrden.Add(CrearHistorial(
            orden.IdOrden,
            EstadoOrdenId.ListoParaEntrega,
            TipoActorId.UsuarioInterno,
            idRepartidor,
            idCompradorExterno: null,
            ahora,
            $"Pago no realizado: {motivo}"));

        db.BitacoraAuditoria.Add(CrearBitacora(
            TipoActorId.UsuarioInterno,
            idRepartidor,
            idCompradorExterno: null,
            "PAGO_NO_REALIZADO",
            "Orden",
            orden.IdOrden,
            "EXITOSO",
            ahora,
            orden.CodigoOrden));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ArchivoDescargaDto> ObtenerConstanciaAsync(
        long idCompradorExterno,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.CodigoOrden == codigoOrden && o.IdCompradorExterno == idCompradorExterno,
                cancellationToken);

        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        if (orden.IdArchivoConstancia is not int idArchivo)
        {
            throw new NotFoundException("La orden no tiene constancia PDF.");
        }

        return await CargarArchivoAsync(idArchivo, cancellationToken);
    }

    public async Task<OrdenProduccionDto> ObtenerParaProduccionAsync(
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.AsNoTracking()
            .FirstOrDefaultAsync(o => o.CodigoOrden == codigoOrden, cancellationToken);

        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        var estado = await db.EstadoOrden.AsNoTracking()
            .FirstAsync(e => e.IdEstadoOrden == orden.IdEstadoOrdenActual, cancellationToken);

        var detalles = await db.DetalleOrden.AsNoTracking()
            .Where(d => d.IdOrden == orden.IdOrden)
            .ToListAsync(cancellationToken);

        var items = new List<ItemProduccionDto>();
        foreach (var detalle in detalles)
        {
            IReadOnlyList<ZonaProduccionDto> zonas = Array.Empty<ZonaProduccionDto>();
            if (detalle.IdPersonalizacion is int idPersonalizacion)
            {
                var filas = await (
                    from pz in db.PersonalizacionZona.AsNoTracking()
                    join z in db.ZonaPersonalizacion.AsNoTracking() on pz.IdZona equals z.IdZona
                    join a in db.Archivo.AsNoTracking() on pz.IdArchivoImagenFinal equals a.IdArchivo
                    where pz.IdPersonalizacion == idPersonalizacion
                    orderby z.OrdenVisual
                    select new
                    {
                        z.IdZona,
                        z.Nombre,
                        pz.IdArchivoImagenFinal,
                        a.TipoMime,
                        a.Datos
                    })
                    .ToListAsync(cancellationToken);

                zonas = filas
                    .Select(fila => new ZonaProduccionDto(
                        fila.IdZona,
                        fila.Nombre,
                        fila.IdArchivoImagenFinal,
                        fila.TipoMime,
                        Convert.ToBase64String(fila.Datos)))
                    .ToList();
            }

            items.Add(new ItemProduccionDto(
                detalle.NombreProductoAplicado,
                detalle.NombreVarianteAplicada,
                detalle.Cantidad,
                detalle.AtributosAplicadosJson,
                detalle.IdPersonalizacion,
                zonas));
        }

        return new OrdenProduccionDto(
            orden.CodigoOrden,
            orden.FechaCreacion,
            orden.Total,
            estado.Nombre,
            orden.NombreAreaAplicado,
            orden.ReferenciaEntrega,
            orden.NicknameCompradorAplicado,
            orden.IdArchivoConstancia,
            items);
    }

    public async Task<ArchivoDescargaDto> ObtenerArchivoProduccionAsync(
        string codigoOrden,
        int idArchivo,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.AsNoTracking()
            .FirstOrDefaultAsync(o => o.CodigoOrden == codigoOrden, cancellationToken);

        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        var esConstancia = orden.IdArchivoConstancia == idArchivo;
        var esImagenZona = await (
            from d in db.DetalleOrden.AsNoTracking()
            join pz in db.PersonalizacionZona.AsNoTracking()
                on d.IdPersonalizacion equals pz.IdPersonalizacion
            where d.IdOrden == orden.IdOrden && pz.IdArchivoImagenFinal == idArchivo
            select pz.IdArchivoImagenFinal)
            .AnyAsync(cancellationToken);

        if (!esConstancia && !esImagenZona)
        {
            throw new NotFoundException("El archivo no pertenece a esta orden.");
        }

        return await CargarArchivoAsync(idArchivo, cancellationToken);
    }

    public async Task<ArchivoDescargaDto> ObtenerArchivoProduccionPorIdAsync(
        int idArchivo,
        CancellationToken cancellationToken)
    {
        var esConstancia = await db.Orden.AsNoTracking()
            .AnyAsync(o => o.IdArchivoConstancia == idArchivo, cancellationToken);

        var esImagenZonaDeOrden = await (
            from d in db.DetalleOrden.AsNoTracking()
            join pz in db.PersonalizacionZona.AsNoTracking()
                on d.IdPersonalizacion equals pz.IdPersonalizacion
            where pz.IdArchivoImagenFinal == idArchivo
            select pz.IdArchivoImagenFinal)
            .AnyAsync(cancellationToken);

        if (!esConstancia && !esImagenZonaDeOrden)
        {
            throw new NotFoundException("El archivo no existe.");
        }

        return await CargarArchivoAsync(idArchivo, cancellationToken);
    }

    private async Task<Orden> ObtenerOrdenDeRepartidorAsync(
        int idRepartidor,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var orden = await db.Orden.FirstOrDefaultAsync(o => o.CodigoOrden == codigoOrden, cancellationToken);
        if (orden is null)
        {
            throw new NotFoundException("La orden no existe.");
        }

        if (orden.IdRepartidorAsignado != idRepartidor
            || orden.IdEstadoOrdenActual != (int)EstadoOrdenId.EnEntrega)
        {
            throw new ForbiddenException("La orden no está asignada a este repartidor.");
        }

        return orden;
    }

    private async Task<IntentoEntrega> ObtenerIntentoAbiertoAsync(
        int idOrden,
        int idRepartidor,
        CancellationToken cancellationToken)
    {
        var intento = await db.IntentoEntrega
            .FirstOrDefaultAsync(
                i => i.IdOrden == idOrden && i.IdRepartidor == idRepartidor && i.FechaHoraFin == null,
                cancellationToken);

        if (intento is null)
        {
            throw new NotFoundException("No hay un intento de entrega abierto.");
        }

        return intento;
    }

    private async Task<OrdenDetalleDto> MapearOrdenAsync(Orden orden, CancellationToken cancellationToken)
    {
        var estado = await db.EstadoOrden.AsNoTracking()
            .FirstAsync(e => e.IdEstadoOrden == orden.IdEstadoOrdenActual, cancellationToken);

        var pago = await (
            from p in db.Pago.AsNoTracking()
            join m in db.MetodoPago.AsNoTracking() on p.IdMetodoPago equals m.IdMetodoPago
            join e in db.EstadoPago.AsNoTracking() on p.IdEstadoPago equals e.IdEstadoPago
            where p.IdOrden == orden.IdOrden
            select new { Metodo = m.Nombre, Estado = e.Nombre })
            .FirstAsync(cancellationToken);

        var items = await db.DetalleOrden.AsNoTracking()
            .Where(d => d.IdOrden == orden.IdOrden)
            .Select(d => new DetalleOrdenDto(
                d.NombreProductoAplicado,
                d.NombreVarianteAplicada,
                d.Cantidad,
                d.PrecioUnitario,
                d.Subtotal,
                d.IdPersonalizacion))
            .ToListAsync(cancellationToken);

        var historial = await (
            from h in db.HistorialEstadoOrden.AsNoTracking()
            join e in db.EstadoOrden.AsNoTracking() on h.IdEstadoOrden equals e.IdEstadoOrden
            where h.IdOrden == orden.IdOrden
            orderby h.FechaHora
            select new TrackingEventoDto(e.Nombre, h.FechaHora, h.Observacion))
            .ToListAsync(cancellationToken);

        var catalogoEstados = await db.EstadoOrden.AsNoTracking().ToListAsync(cancellationToken);

        return new OrdenDetalleDto(
            orden.CodigoOrden,
            orden.FechaCreacion,
            orden.Total,
            estado.Nombre,
            orden.NicknameCompradorAplicado,
            orden.NombreAreaAplicado,
            orden.ReferenciaEntrega,
            pago.Metodo,
            pago.Estado,
            items,
            historial,
            ConstruirPasos(catalogoEstados, orden.IdEstadoOrdenActual),
            orden.IdArchivoConstancia);
    }

    private async Task<IReadOnlyList<OrdenColaDto>> ListarColaAsync(
        int idEstado,
        bool soloSinRepartidor,
        CancellationToken cancellationToken)
    {
        var query = db.Orden.AsNoTracking()
            .Where(o => o.IdEstadoOrdenActual == idEstado);

        if (soloSinRepartidor)
        {
            query = query.Where(o => o.IdRepartidorAsignado == null);
        }

        var ordenes = await query
            .OrderBy(o => o.FechaCreacion)
            .ToListAsync(cancellationToken);

        var estado = await db.EstadoOrden.AsNoTracking()
            .FirstAsync(e => e.IdEstadoOrden == idEstado, cancellationToken);

        return ordenes
            .Select(o => new OrdenColaDto(
                o.CodigoOrden,
                o.FechaCreacion,
                o.Total,
                estado.Nombre,
                o.NombreAreaAplicado))
            .ToList();
    }

    private static IReadOnlyList<TrackingPasoDto> ConstruirPasos(
        IReadOnlyList<EstadoOrden> catalogo,
        int idEstadoActual)
    {
        var pasoActual = idEstadoActual switch
        {
            (int)EstadoOrdenId.OrdenGenerada => 1,
            (int)EstadoOrdenId.EnElaboracion => 2,
            (int)EstadoOrdenId.ListoParaEntrega => 3,
            (int)EstadoOrdenId.EnEntrega => 4,
            (int)EstadoOrdenId.Entregado => 5,
            (int)EstadoOrdenId.CompradorNoEncontrado => 3,
            _ => 1
        };

        EstadoOrdenId[] ids =
        [
            EstadoOrdenId.OrdenGenerada,
            EstadoOrdenId.EnElaboracion,
            EstadoOrdenId.ListoParaEntrega,
            EstadoOrdenId.EnEntrega,
            EstadoOrdenId.Entregado
        ];

        return ids
            .Select((id, index) =>
            {
                var ordenPaso = index + 1;
                var estado = catalogo.First(e => e.IdEstadoOrden == (int)id);
                return new TrackingPasoDto(
                    ordenPaso,
                    estado.Codigo,
                    estado.Nombre,
                    Completado: ordenPaso < pasoActual || (pasoActual == 5 && ordenPaso == 5),
                    Actual: ordenPaso == pasoActual);
            })
            .ToList();
    }

    private static HistorialEstadoOrden CrearHistorial(
        int idOrden,
        EstadoOrdenId estado,
        TipoActorId actor,
        int? idUsuarioInterno,
        long? idCompradorExterno,
        DateTime fecha,
        string? observacion)
    {
        return new HistorialEstadoOrden
        {
            IdOrden = idOrden,
            IdEstadoOrden = (int)estado,
            IdTipoActor = (int)actor,
            IdUsuarioInterno = idUsuarioInterno,
            IdCompradorExterno = idCompradorExterno,
            FechaHora = fecha,
            Observacion = observacion
        };
    }

    private void EncolarNotificaciones(
        int idOrden,
        TipoNotificacionId tipo,
        bool notificaEmail,
        bool notificaWhatsApp,
        string correo,
        string? telefono,
        DateTime ahora)
    {
        if (notificaEmail && !string.IsNullOrWhiteSpace(correo))
        {
            db.Notificacion.Add(new Notificacion
            {
                IdOrden = idOrden,
                IdTipoNotificacion = (int)tipo,
                IdCanalNotificacion = (int)CanalNotificacionId.Correo,
                IdEstadoNotificacion = (int)EstadoNotificacionId.Pendiente,
                Destino = Truncar(correo, 200),
                FechaCreacion = ahora
            });
        }

        if (notificaWhatsApp && !string.IsNullOrWhiteSpace(telefono))
        {
            db.Notificacion.Add(new Notificacion
            {
                IdOrden = idOrden,
                IdTipoNotificacion = (int)tipo,
                IdCanalNotificacion = (int)CanalNotificacionId.WhatsApp,
                IdEstadoNotificacion = (int)EstadoNotificacionId.Pendiente,
                Destino = Truncar(telefono, 200),
                FechaCreacion = ahora
            });
        }
    }

    private static BitacoraAuditoria CrearBitacora(
        TipoActorId actor,
        int? idUsuarioInterno,
        long? idCompradorExterno,
        string accion,
        string entidad,
        int idEntidad,
        string resultado,
        DateTime fecha,
        string? detalle)
    {
        return new BitacoraAuditoria
        {
            IdTipoActor = (int)actor,
            IdUsuarioInterno = idUsuarioInterno,
            IdCompradorExterno = idCompradorExterno,
            Accion = accion,
            Entidad = entidad,
            IdEntidad = idEntidad,
            Resultado = resultado,
            Detalle = detalle,
            FechaHora = fecha
        };
    }

    private static string Truncar(string valor, int max)
    {
        var limpio = valor.Trim();
        if (string.IsNullOrWhiteSpace(limpio))
        {
            throw new BusinessRuleException("El dato del comprador es obligatorio.");
        }

        return limpio.Length <= max ? limpio : limpio[..max];
    }

    private async Task<ArchivoDescargaDto> CargarArchivoAsync(
        int idArchivo,
        CancellationToken cancellationToken)
    {
        var archivo = await db.Archivo.AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdArchivo == idArchivo, cancellationToken);

        if (archivo is null)
        {
            throw new NotFoundException("El archivo no existe.");
        }

        return new ArchivoDescargaDto(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }

    private async Task IntentarEnviarCorreoAsync(
        int idOrden,
        TipoNotificacionId tipo,
        Func<CancellationToken, Task<bool>> enviar,
        CancellationToken cancellationToken)
    {
        var notificacion = await db.Notificacion
            .FirstOrDefaultAsync(
                n => n.IdOrden == idOrden
                     && n.IdTipoNotificacion == (int)tipo
                     && n.IdCanalNotificacion == (int)CanalNotificacionId.Correo
                     && n.IdEstadoNotificacion == (int)EstadoNotificacionId.Pendiente,
                cancellationToken);

        if (notificacion is null)
        {
            return;
        }

        notificacion.Intentos += 1;
        var enviado = false;
        try
        {
            enviado = await enviar(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            enviado = false;
        }

        if (enviado)
        {
            notificacion.IdEstadoNotificacion = (int)EstadoNotificacionId.Enviada;
            notificacion.FechaEnvio = DateTime.Now;
            notificacion.MensajeError = null;
        }
        else
        {
            notificacion.IdEstadoNotificacion = (int)EstadoNotificacionId.Fallida;
            notificacion.MensajeError = "El correo no pudo enviarse.";
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
