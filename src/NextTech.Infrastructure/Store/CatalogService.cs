using Microsoft.EntityFrameworkCore;
using NextTech.Application.DTOs.Catalog;
using NextTech.Application.Modules.Catalog;
using NextTech.Domain.Enums;
using NextTech.Domain.Exceptions;
using NextTech.Infrastructure.Persistence.SqlServer;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Store;

public sealed class CatalogService(NextTechDbContext db) : ICatalogService
{
    public async Task<IReadOnlyList<ProductoResumenDto>> ListarProductosActivosAsync(
        CancellationToken cancellationToken)
    {
        var productos = await (
            from producto in db.Producto.AsNoTracking()
            join categoria in db.Categoria.AsNoTracking()
                on producto.IdCategoria equals categoria.IdCategoria
            where producto.Activo && categoria.Activo
            select new
            {
                producto.CodigoProducto,
                producto.Nombre,
                producto.Descripcion,
                Categoria = categoria.Nombre,
                producto.PrecioBase,
                producto.PermitePersonalizacion,
                producto.IdProducto
            })
            .ToListAsync(cancellationToken);

        var ids = productos.Select(p => p.IdProducto).ToList();

        var adicionales = await db.VarianteProducto.AsNoTracking()
            .Where(v => ids.Contains(v.IdProducto) && v.Activo)
            .GroupBy(v => v.IdProducto)
            .Select(g => new
            {
                IdProducto = g.Key,
                MinAdicional = g.Min(v => v.PrecioAdicional)
            })
            .ToListAsync(cancellationToken);

        var minPorProducto = adicionales.ToDictionary(x => x.IdProducto, x => x.MinAdicional);

        return productos
            .Select(p => new ProductoResumenDto(
                p.CodigoProducto,
                p.Nombre,
                p.Descripcion,
                p.Categoria,
                p.PrecioBase + (minPorProducto.TryGetValue(p.IdProducto, out var extra) ? extra : 0m),
                p.PermitePersonalizacion))
            .ToList();
    }

    public async Task<ProductoDetalleDto> ObtenerProductoAsync(
        string codigoProducto,
        CancellationToken cancellationToken)
    {
        var producto = await (
            from p in db.Producto.AsNoTracking()
            join c in db.Categoria.AsNoTracking()
                on p.IdCategoria equals c.IdCategoria
            where p.CodigoProducto == codigoProducto && p.Activo && c.Activo
            select new { Producto = p, Categoria = c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (producto is null)
        {
            throw new NotFoundException("El producto no existe o no está disponible.");
        }

        var variantes = await db.VarianteProducto.AsNoTracking()
            .Where(v => v.IdProducto == producto.Producto.IdProducto && v.Activo)
            .ToListAsync(cancellationToken);

        var idsVariante = variantes.Select(v => v.IdVariante).ToList();

        var atributos = await (
            from va in db.VarianteAtributo.AsNoTracking()
            join a in db.Atributo.AsNoTracking() on va.IdAtributo equals a.IdAtributo
            join valor in db.ValorAtributo.AsNoTracking() on va.IdValorAtributo equals valor.IdValorAtributo
            join pa in db.ProductoAtributo.AsNoTracking()
                on new { va.IdAtributo, producto.Producto.IdProducto }
                equals new { pa.IdAtributo, pa.IdProducto }
            where idsVariante.Contains(va.IdVariante) && a.Activo && valor.Activo
            orderby pa.OrdenVisual
            select new
            {
                va.IdVariante,
                Atributo = a.Nombre,
                valor.Valor
            })
            .ToListAsync(cancellationToken);

        var zonas = await db.ZonaPersonalizacion.AsNoTracking()
            .Where(z => z.IdProducto == producto.Producto.IdProducto && z.Activo)
            .OrderBy(z => z.OrdenVisual)
            .Select(z => new ZonaDto(z.IdZona, z.Nombre, z.EsObligatoria, z.OrdenVisual))
            .ToListAsync(cancellationToken);

        var plantillas = await db.PlantillaVarianteZona.AsNoTracking()
            .Where(p => idsVariante.Contains(p.IdVariante) && p.Activo)
            .Select(p => new { p.IdVariante, p.IdZona, p.Forma, p.AnchoLienzo, p.AltoLienzo })
            .ToListAsync(cancellationToken);

        var variantesDto = variantes
            .Select(v => new VarianteDto(
                v.IdVariante,
                v.CodigoVariante,
                v.Nombre,
                v.PrecioAdicional,
                producto.Producto.PrecioBase + v.PrecioAdicional,
                atributos
                    .Where(a => a.IdVariante == v.IdVariante)
                    .Select(a => new AtributoValorDto(a.Atributo, a.Valor))
                    .ToList(),
                plantillas
                    .Where(p => p.IdVariante == v.IdVariante)
                    .Select(p => new PlantillaZonaDto(p.IdZona, p.Forma, p.AnchoLienzo, p.AltoLienzo))
                    .ToList()))
            .ToList();

        return new ProductoDetalleDto(
            producto.Producto.CodigoProducto,
            producto.Producto.Nombre,
            producto.Producto.Descripcion,
            producto.Categoria,
            producto.Producto.PrecioBase,
            producto.Producto.PermitePersonalizacion,
            new MedidasFisicasDto(
                LlaveroMedidas.DiametroPulgadas,
                LlaveroMedidas.NfcPulgadas,
                decimal.Round(LlaveroMedidas.DiametroMm, 2),
                decimal.Round(LlaveroMedidas.NfcMm, 2),
                LlaveroMedidas.LienzoPx,
                LlaveroMedidas.NfcLienzoPx),
            variantesDto,
            zonas);
    }

    public async Task<IReadOnlyList<AreaEntregaDto>> ListarAreasEntregaAsync(
        CancellationToken cancellationToken)
    {
        return await db.AreaEntrega.AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.Nombre)
            .Select(a => new AreaEntregaDto(a.IdAreaEntrega, a.Nombre, a.Descripcion))
            .ToListAsync(cancellationToken);
    }
}
