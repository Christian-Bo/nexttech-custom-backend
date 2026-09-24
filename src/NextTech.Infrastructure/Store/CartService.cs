using Microsoft.EntityFrameworkCore;
using NextTech.Application.DTOs.Cart;
using NextTech.Application.Modules.Cart;
using NextTech.Domain.Enums;
using NextTech.Domain.Exceptions;
using NextTech.Infrastructure.Persistence.SqlServer;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Store;

public sealed class CartService(NextTechDbContext db) : ICartService
{
    public async Task<CarritoDto> ObtenerActivoAsync(
        long idCompradorExterno,
        CancellationToken cancellationToken)
    {
        var carrito = await ObtenerOCrearActivoAsync(idCompradorExterno, cancellationToken);
        return await MapearAsync(carrito, cancellationToken);
    }

    public async Task<CarritoDto> AgregarItemAsync(
        long idCompradorExterno,
        AgregarItemCarritoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Cantidad <= 0)
        {
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");
        }

        var variante = await (
            from v in db.VarianteProducto
            join p in db.Producto on v.IdProducto equals p.IdProducto
            join c in db.Categoria on p.IdCategoria equals c.IdCategoria
            where v.IdVariante == request.IdVariante && v.Activo && p.Activo && c.Activo
            select new { Variante = v, Producto = p })
            .FirstOrDefaultAsync(cancellationToken);

        if (variante is null)
        {
            throw new NotFoundException("La variante no está disponible.");
        }

        if (variante.Producto.PermitePersonalizacion && request.IdPersonalizacion is null)
        {
            throw new BusinessRuleException("Este producto requiere una personalización antes de ir al carrito.");
        }

        if (!variante.Producto.PermitePersonalizacion && request.IdPersonalizacion is not null)
        {
            throw new BusinessRuleException("Este producto no admite personalización.");
        }

        if (request.IdPersonalizacion is not null)
        {
            var personalizacion = await db.Personalizacion
                .FirstOrDefaultAsync(
                    p => p.IdPersonalizacion == request.IdPersonalizacion,
                    cancellationToken);

            if (personalizacion is null)
            {
                throw new NotFoundException("La personalización no existe.");
            }

            if (personalizacion.IdVariante != request.IdVariante)
            {
                throw new BusinessRuleException("La personalización no corresponde a la variante.");
            }

            if (personalizacion.Bloqueada)
            {
                throw new BusinessRuleException("La personalización ya está bloqueada y no puede usarse de nuevo.");
            }

            var yaUsada = await db.DetalleCarrito
                .AnyAsync(d => d.IdPersonalizacion == personalizacion.IdPersonalizacion, cancellationToken);

            if (yaUsada)
            {
                throw new ConflictException("Esa personalización ya está en un carrito.");
            }
        }

        var carrito = await ObtenerOCrearActivoAsync(idCompradorExterno, cancellationToken);
        var ahora = DateTime.Now;

        if (request.IdPersonalizacion is null)
        {
            var existente = await db.DetalleCarrito
                .FirstOrDefaultAsync(
                    d => d.IdCarrito == carrito.IdCarrito
                         && d.IdVariante == request.IdVariante
                         && d.IdPersonalizacion == null,
                    cancellationToken);

            if (existente is not null)
            {
                existente.Cantidad += request.Cantidad;
                existente.FechaActualizacion = ahora;
            }
            else
            {
                db.DetalleCarrito.Add(new DetalleCarrito
                {
                    IdCarrito = carrito.IdCarrito,
                    IdVariante = request.IdVariante,
                    Cantidad = request.Cantidad,
                    IdPersonalizacion = null,
                    FechaCreacion = ahora
                });
            }
        }
        else
        {
            db.DetalleCarrito.Add(new DetalleCarrito
            {
                IdCarrito = carrito.IdCarrito,
                IdVariante = request.IdVariante,
                Cantidad = request.Cantidad,
                IdPersonalizacion = request.IdPersonalizacion,
                FechaCreacion = ahora
            });
        }

        carrito.UltimaActividad = ahora;
        carrito.FechaActualizacion = ahora;
        await db.SaveChangesAsync(cancellationToken);

        return await MapearAsync(carrito, cancellationToken);
    }

    public async Task<CarritoDto> CambiarCantidadAsync(
        long idCompradorExterno,
        int idDetalleCarrito,
        int cantidad,
        CancellationToken cancellationToken)
    {
        if (cantidad <= 0)
        {
            throw new BusinessRuleException("Para quitar un artículo elimínalo; la cantidad no puede ser 0.");
        }

        var (carrito, detalle) = await ObtenerDetallePropioAsync(
            idCompradorExterno,
            idDetalleCarrito,
            cancellationToken);

        detalle.Cantidad = cantidad;
        detalle.FechaActualizacion = DateTime.Now;
        carrito.UltimaActividad = DateTime.Now;
        carrito.FechaActualizacion = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);

        return await MapearAsync(carrito, cancellationToken);
    }

    public async Task<CarritoDto> EliminarItemAsync(
        long idCompradorExterno,
        int idDetalleCarrito,
        CancellationToken cancellationToken)
    {
        var (carrito, detalle) = await ObtenerDetallePropioAsync(
            idCompradorExterno,
            idDetalleCarrito,
            cancellationToken);

        db.DetalleCarrito.Remove(detalle);
        carrito.UltimaActividad = DateTime.Now;
        carrito.FechaActualizacion = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);

        return await MapearAsync(carrito, cancellationToken);
    }

    private async Task<Carrito> ObtenerOCrearActivoAsync(
        long idCompradorExterno,
        CancellationToken cancellationToken)
    {
        var existente = await db.Carrito
            .FirstOrDefaultAsync(
                c => c.IdCompradorExterno == idCompradorExterno
                     && c.IdEstadoCarrito == (int)EstadoCarritoId.Activo,
                cancellationToken);

        if (existente is not null)
        {
            return existente;
        }

        var ahora = DateTime.Now;
        var carrito = new Carrito
        {
            IdCompradorExterno = idCompradorExterno,
            IdEstadoCarrito = (int)EstadoCarritoId.Activo,
            FechaCreacion = ahora,
            UltimaActividad = ahora
        };

        db.Carrito.Add(carrito);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return carrito;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            return await db.Carrito
                .FirstAsync(
                    c => c.IdCompradorExterno == idCompradorExterno
                         && c.IdEstadoCarrito == (int)EstadoCarritoId.Activo,
                    cancellationToken);
        }
    }

    private async Task<(Carrito Carrito, DetalleCarrito Detalle)> ObtenerDetallePropioAsync(
        long idCompradorExterno,
        int idDetalleCarrito,
        CancellationToken cancellationToken)
    {
        var carrito = await db.Carrito
            .FirstOrDefaultAsync(
                c => c.IdCompradorExterno == idCompradorExterno
                     && c.IdEstadoCarrito == (int)EstadoCarritoId.Activo,
                cancellationToken);

        if (carrito is null)
        {
            throw new NotFoundException("No hay un carrito activo.");
        }

        var detalle = await db.DetalleCarrito
            .FirstOrDefaultAsync(
                d => d.IdDetalleCarrito == idDetalleCarrito && d.IdCarrito == carrito.IdCarrito,
                cancellationToken);

        if (detalle is null)
        {
            throw new NotFoundException("El artículo no está en el carrito.");
        }

        return (carrito, detalle);
    }

    private async Task<CarritoDto> MapearAsync(Carrito carrito, CancellationToken cancellationToken)
    {
        var detalles = await db.DetalleCarrito.AsNoTracking()
            .Where(d => d.IdCarrito == carrito.IdCarrito)
            .ToListAsync(cancellationToken);

        var idsVariante = detalles.Select(d => d.IdVariante).Distinct().ToList();

        var variantes = await (
            from v in db.VarianteProducto.AsNoTracking()
            join p in db.Producto.AsNoTracking() on v.IdProducto equals p.IdProducto
            where idsVariante.Contains(v.IdVariante)
            select new { v.IdVariante, Variante = v.Nombre, Producto = p.Nombre, p.PrecioBase, v.PrecioAdicional })
            .ToListAsync(cancellationToken);

        var porVariante = variantes.ToDictionary(v => v.IdVariante);

        var items = detalles.Select(d =>
        {
            var info = porVariante[d.IdVariante];
            var unitario = info.PrecioBase + info.PrecioAdicional;
            return new DetalleCarritoDto(
                d.IdDetalleCarrito,
                d.IdVariante,
                info.Producto,
                info.Variante,
                d.IdPersonalizacion,
                d.Cantidad,
                unitario,
                unitario * d.Cantidad);
        }).ToList();

        return new CarritoDto(
            carrito.IdCarrito,
            "ACTIVO",
            items.Sum(i => i.Subtotal),
            items);
    }
}
