using Microsoft.EntityFrameworkCore;
using NextTech.Application.DTOs.Catalog;
using NextTech.Application.Modules.Catalog;
using NextTech.Domain.Enums;
using NextTech.Domain.Exceptions;
using NextTech.Infrastructure.Persistence.SqlServer;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Store;

public sealed class CatalogAdminService(NextTechDbContext db) : ICatalogAdminService
{
    public async Task<IReadOnlyList<CategoriaAdminDto>> ListarCategoriasAsync(
        CancellationToken cancellationToken)
    {
        return await db.Categoria.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaAdminDto(c.IdCategoria, c.Nombre, c.Descripcion, c.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoriaAdminDto> CrearCategoriaAsync(
        int idUsuarioInterno,
        CrearCategoriaRequest request,
        CancellationToken cancellationToken)
    {
        var nombre = Requerido(request.Nombre, 150, "El nombre de la categoría");
        await AsegurarNombreCategoriaLibreAsync(nombre, idExcepto: null, cancellationToken);

        var ahora = DateTime.Now;
        var categoria = new Categoria
        {
            Nombre = nombre,
            Descripcion = Opcional(request.Descripcion, 500),
            Activo = true,
            FechaCreacion = ahora
        };

        db.Categoria.Add(categoria);
        await GuardarConUnicidadAsync("Ya existe una categoría con ese nombre.", cancellationToken);
        Auditar(idUsuarioInterno, "CATEGORIA_CREAR", "Categoria", categoria.IdCategoria, nombre, ahora);
        await db.SaveChangesAsync(cancellationToken);

        return MapearCategoria(categoria);
    }

    public async Task<CategoriaAdminDto> ActualizarCategoriaAsync(
        int idUsuarioInterno,
        int idCategoria,
        ActualizarCategoriaRequest request,
        CancellationToken cancellationToken)
    {
        var categoria = await ObtenerCategoriaAsync(idCategoria, cancellationToken);
        var nombre = Requerido(request.Nombre, 150, "El nombre de la categoría");
        await AsegurarNombreCategoriaLibreAsync(nombre, categoria.IdCategoria, cancellationToken);

        categoria.Nombre = nombre;
        categoria.Descripcion = Opcional(request.Descripcion, 500);
        categoria.FechaActualizacion = DateTime.Now;

        await GuardarConUnicidadAsync("Ya existe una categoría con ese nombre.", cancellationToken);
        Auditar(idUsuarioInterno, "CATEGORIA_EDITAR", "Categoria", categoria.IdCategoria, nombre, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);

        return MapearCategoria(categoria);
    }

    public async Task DesactivarCategoriaAsync(
        int idUsuarioInterno,
        int idCategoria,
        CancellationToken cancellationToken)
    {
        var categoria = await ObtenerCategoriaAsync(idCategoria, cancellationToken);
        var tieneProductosActivos = await db.Producto.AnyAsync(
            p => p.IdCategoria == idCategoria && p.Activo,
            cancellationToken);

        if (tieneProductosActivos)
        {
            throw new BusinessRuleException(
                "No se puede desactivar una categoría con productos activos.");
        }

        Desactivar(categoria, DateTime.Now);
        Auditar(idUsuarioInterno, "CATEGORIA_DESACTIVAR", "Categoria", categoria.IdCategoria, categoria.Nombre, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarCategoriaAsync(
        int idUsuarioInterno,
        int idCategoria,
        CancellationToken cancellationToken)
    {
        var categoria = await ObtenerCategoriaAsync(idCategoria, cancellationToken);
        Activar(categoria, DateTime.Now);
        Auditar(idUsuarioInterno, "CATEGORIA_ACTIVAR", "Categoria", categoria.IdCategoria, categoria.Nombre, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductoAdminResumenDto>> ListarProductosAsync(
        CancellationToken cancellationToken)
    {
        var filas = await (
            from p in db.Producto.AsNoTracking()
            join c in db.Categoria.AsNoTracking() on p.IdCategoria equals c.IdCategoria
            orderby p.CodigoProducto
            select new
            {
                p.IdProducto,
                p.CodigoProducto,
                p.Nombre,
                Categoria = c.Nombre,
                p.PrecioBase,
                p.PermitePersonalizacion,
                p.Activo,
                VariantesActivas = db.VarianteProducto.Count(v => v.IdProducto == p.IdProducto && v.Activo)
            })
            .ToListAsync(cancellationToken);

        return filas
            .Select(p => new ProductoAdminResumenDto(
                p.IdProducto,
                p.CodigoProducto,
                p.Nombre,
                p.Categoria,
                p.PrecioBase,
                p.PermitePersonalizacion,
                p.Activo,
                p.VariantesActivas))
            .ToList();
    }

    public Task<ProductoAdminDetalleDto> ObtenerProductoAsync(
        string codigoProducto,
        CancellationToken cancellationToken)
    {
        return MapearProductoAsync(codigoProducto, cancellationToken);
    }

    public async Task<ProductoAdminDetalleDto> CrearProductoAsync(
        int idUsuarioInterno,
        CrearProductoRequest request,
        CancellationToken cancellationToken)
    {
        var codigo = NormalizarCodigo(request.CodigoProducto, 30, "El código de producto");
        var nombre = Requerido(request.Nombre, 150, "El nombre del producto");
        ValidarPrecioBase(request.PrecioBase);
        await AsegurarCodigoProductoLibreAsync(codigo, idExcepto: null, cancellationToken);
        var categoria = await ObtenerCategoriaActivaAsync(request.IdCategoria, cancellationToken);

        var ahora = DateTime.Now;
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var producto = new Producto
            {
                IdCategoria = categoria.IdCategoria,
                CodigoProducto = codigo,
                Nombre = nombre,
                Descripcion = Opcional(request.Descripcion, 1000),
                PrecioBase = request.PrecioBase,
                PermitePersonalizacion = request.PermitePersonalizacion,
                Activo = true,
                FechaCreacion = ahora
            };

            db.Producto.Add(producto);
            await GuardarConUnicidadAsync("Ya existe un producto con ese código.", cancellationToken);

            if (request.PermitePersonalizacion)
            {
                AsegurarZonasLadoAB(producto.IdProducto, ahora);
                await db.SaveChangesAsync(cancellationToken);
            }

            Auditar(idUsuarioInterno, "PRODUCTO_CREAR", "Producto", producto.IdProducto, codigo, ahora);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return await MapearProductoAsync(codigo, cancellationToken);
    }

    public async Task<ProductoAdminDetalleDto> ActualizarProductoAsync(
        int idUsuarioInterno,
        string codigoProducto,
        ActualizarProductoRequest request,
        CancellationToken cancellationToken)
    {
        var producto = await ObtenerProductoPorCodigoAsync(codigoProducto, cancellationToken);
        var nombre = Requerido(request.Nombre, 150, "El nombre del producto");
        ValidarPrecioBase(request.PrecioBase);
        await ObtenerCategoriaActivaAsync(request.IdCategoria, cancellationToken);

        producto.IdCategoria = request.IdCategoria;
        producto.Nombre = nombre;
        producto.Descripcion = Opcional(request.Descripcion, 1000);
        producto.PrecioBase = request.PrecioBase;
        producto.PermitePersonalizacion = request.PermitePersonalizacion;
        producto.FechaActualizacion = DateTime.Now;

        if (request.PermitePersonalizacion)
        {
            AsegurarZonasLadoAB(producto.IdProducto, DateTime.Now);
        }

        await GuardarConUnicidadAsync("No se pudo actualizar el producto.", cancellationToken);
        Auditar(idUsuarioInterno, "PRODUCTO_EDITAR", "Producto", producto.IdProducto, producto.CodigoProducto, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);

        return await MapearProductoAsync(producto.CodigoProducto, cancellationToken);
    }

    public async Task DesactivarProductoAsync(
        int idUsuarioInterno,
        string codigoProducto,
        CancellationToken cancellationToken)
    {
        var producto = await ObtenerProductoPorCodigoAsync(codigoProducto, cancellationToken);
        Desactivar(producto, DateTime.Now);
        Auditar(idUsuarioInterno, "PRODUCTO_DESACTIVAR", "Producto", producto.IdProducto, producto.CodigoProducto, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarProductoAsync(
        int idUsuarioInterno,
        string codigoProducto,
        CancellationToken cancellationToken)
    {
        var producto = await ObtenerProductoPorCodigoAsync(codigoProducto, cancellationToken);
        var categoria = await ObtenerCategoriaAsync(producto.IdCategoria, cancellationToken);
        if (!categoria.Activo)
        {
            throw new BusinessRuleException("Activa primero la categoría del producto.");
        }

        Activar(producto, DateTime.Now);
        Auditar(idUsuarioInterno, "PRODUCTO_ACTIVAR", "Producto", producto.IdProducto, producto.CodigoProducto, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VarianteAdminDto> CrearVarianteAsync(
        int idUsuarioInterno,
        string codigoProducto,
        CrearVarianteRequest request,
        CancellationToken cancellationToken)
    {
        var producto = await ObtenerProductoPorCodigoAsync(codigoProducto, cancellationToken);
        var codigo = NormalizarCodigo(request.CodigoVariante, 30, "El código de variante");
        var nombre = Requerido(request.Nombre, 150, "El nombre de la variante");
        ValidarPrecioAdicional(request.PrecioAdicional);
        await AsegurarCodigoVarianteLibreAsync(codigo, idExcepto: null, cancellationToken);
        await AsegurarNombreVarianteLibreAsync(producto.IdProducto, nombre, idExcepto: null, cancellationToken);

        var forma = NormalizarForma(request.FormaLienzo);
        var ahora = DateTime.Now;
        var variante = new VarianteProducto
        {
            IdProducto = producto.IdProducto,
            CodigoVariante = codigo,
            Nombre = nombre,
            Descripcion = Opcional(request.Descripcion, 500),
            PrecioAdicional = request.PrecioAdicional,
            Activo = true,
            FechaCreacion = ahora
        };

        db.VarianteProducto.Add(variante);
        await GuardarConUnicidadAsync("Ya existe una variante con ese código o nombre.", cancellationToken);

        if (producto.PermitePersonalizacion)
        {
            await AsegurarPlantillasAsync(variante.IdVariante, producto.IdProducto, forma, ahora, cancellationToken);
        }

        Auditar(idUsuarioInterno, "VARIANTE_CREAR", "VarianteProducto", variante.IdVariante, codigo, ahora);
        await db.SaveChangesAsync(cancellationToken);

        return await MapearVarianteAsync(variante.IdVariante, producto.PrecioBase, cancellationToken);
    }

    public async Task<VarianteAdminDto> ActualizarVarianteAsync(
        int idUsuarioInterno,
        int idVariante,
        ActualizarVarianteRequest request,
        CancellationToken cancellationToken)
    {
        var variante = await ObtenerVarianteAsync(idVariante, cancellationToken);
        var producto = await db.Producto.FirstAsync(p => p.IdProducto == variante.IdProducto, cancellationToken);
        var nombre = Requerido(request.Nombre, 150, "El nombre de la variante");
        ValidarPrecioAdicional(request.PrecioAdicional);
        await AsegurarNombreVarianteLibreAsync(producto.IdProducto, nombre, variante.IdVariante, cancellationToken);

        variante.Nombre = nombre;
        variante.Descripcion = Opcional(request.Descripcion, 500);
        variante.PrecioAdicional = request.PrecioAdicional;
        variante.FechaActualizacion = DateTime.Now;

        if (producto.PermitePersonalizacion && !string.IsNullOrWhiteSpace(request.FormaLienzo))
        {
            var forma = NormalizarForma(request.FormaLienzo);
            await AsegurarPlantillasAsync(variante.IdVariante, producto.IdProducto, forma, DateTime.Now, cancellationToken);
        }

        await GuardarConUnicidadAsync("Ya existe una variante con ese nombre en el producto.", cancellationToken);
        Auditar(idUsuarioInterno, "VARIANTE_EDITAR", "VarianteProducto", variante.IdVariante, variante.CodigoVariante, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);

        return await MapearVarianteAsync(variante.IdVariante, producto.PrecioBase, cancellationToken);
    }

    public async Task DesactivarVarianteAsync(
        int idUsuarioInterno,
        int idVariante,
        CancellationToken cancellationToken)
    {
        var variante = await ObtenerVarianteAsync(idVariante, cancellationToken);
        Desactivar(variante, DateTime.Now);
        Auditar(idUsuarioInterno, "VARIANTE_DESACTIVAR", "VarianteProducto", variante.IdVariante, variante.CodigoVariante, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarVarianteAsync(
        int idUsuarioInterno,
        int idVariante,
        CancellationToken cancellationToken)
    {
        var variante = await ObtenerVarianteAsync(idVariante, cancellationToken);
        Activar(variante, DateTime.Now);
        Auditar(idUsuarioInterno, "VARIANTE_ACTIVAR", "VarianteProducto", variante.IdVariante, variante.CodigoVariante, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AreaEntregaAdminDto>> ListarAreasAsync(
        CancellationToken cancellationToken)
    {
        return await db.AreaEntrega.AsNoTracking()
            .OrderBy(a => a.Nombre)
            .Select(a => new AreaEntregaAdminDto(a.IdAreaEntrega, a.Nombre, a.Descripcion, a.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<AreaEntregaAdminDto> CrearAreaAsync(
        int idUsuarioInterno,
        CrearAreaEntregaRequest request,
        CancellationToken cancellationToken)
    {
        var nombre = Requerido(request.Nombre, 150, "El nombre del área");
        await AsegurarNombreAreaLibreAsync(nombre, idExcepto: null, cancellationToken);

        var ahora = DateTime.Now;
        var area = new AreaEntrega
        {
            Nombre = nombre,
            Descripcion = Opcional(request.Descripcion, 500),
            Activo = true,
            FechaCreacion = ahora
        };

        db.AreaEntrega.Add(area);
        await GuardarConUnicidadAsync("Ya existe un área de entrega con ese nombre.", cancellationToken);
        Auditar(idUsuarioInterno, "AREA_CREAR", "AreaEntrega", area.IdAreaEntrega, nombre, ahora);
        await db.SaveChangesAsync(cancellationToken);

        return MapearArea(area);
    }

    public async Task<AreaEntregaAdminDto> ActualizarAreaAsync(
        int idUsuarioInterno,
        int idAreaEntrega,
        ActualizarAreaEntregaRequest request,
        CancellationToken cancellationToken)
    {
        var area = await ObtenerAreaAsync(idAreaEntrega, cancellationToken);
        var nombre = Requerido(request.Nombre, 150, "El nombre del área");
        await AsegurarNombreAreaLibreAsync(nombre, area.IdAreaEntrega, cancellationToken);

        area.Nombre = nombre;
        area.Descripcion = Opcional(request.Descripcion, 500);
        area.FechaActualizacion = DateTime.Now;

        await GuardarConUnicidadAsync("Ya existe un área de entrega con ese nombre.", cancellationToken);
        Auditar(idUsuarioInterno, "AREA_EDITAR", "AreaEntrega", area.IdAreaEntrega, nombre, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);

        return MapearArea(area);
    }

    public async Task DesactivarAreaAsync(
        int idUsuarioInterno,
        int idAreaEntrega,
        CancellationToken cancellationToken)
    {
        var area = await ObtenerAreaAsync(idAreaEntrega, cancellationToken);
        Desactivar(area, DateTime.Now);
        Auditar(idUsuarioInterno, "AREA_DESACTIVAR", "AreaEntrega", area.IdAreaEntrega, area.Nombre, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivarAreaAsync(
        int idUsuarioInterno,
        int idAreaEntrega,
        CancellationToken cancellationToken)
    {
        var area = await ObtenerAreaAsync(idAreaEntrega, cancellationToken);
        Activar(area, DateTime.Now);
        Auditar(idUsuarioInterno, "AREA_ACTIVAR", "AreaEntrega", area.IdAreaEntrega, area.Nombre, DateTime.Now);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProductoAdminDetalleDto> MapearProductoAsync(
        string codigoProducto,
        CancellationToken cancellationToken)
    {
        var fila = await (
            from p in db.Producto.AsNoTracking()
            join c in db.Categoria.AsNoTracking() on p.IdCategoria equals c.IdCategoria
            where p.CodigoProducto == codigoProducto.Trim()
            select new { Producto = p, Categoria = c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (fila is null)
        {
            throw new NotFoundException("El producto no existe.");
        }

        var zonas = await db.ZonaPersonalizacion.AsNoTracking()
            .Where(z => z.IdProducto == fila.Producto.IdProducto)
            .OrderBy(z => z.OrdenVisual)
            .Select(z => new ZonaDto(z.IdZona, z.Nombre, z.EsObligatoria, z.OrdenVisual))
            .ToListAsync(cancellationToken);

        var variantes = await db.VarianteProducto.AsNoTracking()
            .Where(v => v.IdProducto == fila.Producto.IdProducto)
            .OrderBy(v => v.CodigoVariante)
            .ToListAsync(cancellationToken);

        var ids = variantes.Select(v => v.IdVariante).ToList();
        var plantillas = await db.PlantillaVarianteZona.AsNoTracking()
            .Where(p => ids.Contains(p.IdVariante) && p.Activo)
            .ToListAsync(cancellationToken);

        var variantesDto = variantes
            .Select(v => new VarianteAdminDto(
                v.IdVariante,
                v.CodigoVariante,
                v.Nombre,
                v.Descripcion,
                v.PrecioAdicional,
                fila.Producto.PrecioBase + v.PrecioAdicional,
                v.Activo,
                plantillas.FirstOrDefault(p => p.IdVariante == v.IdVariante)?.Forma))
            .ToList();

        return new ProductoAdminDetalleDto(
            fila.Producto.IdProducto,
            fila.Producto.IdCategoria,
            fila.Producto.CodigoProducto,
            fila.Producto.Nombre,
            fila.Producto.Descripcion,
            fila.Categoria,
            fila.Producto.PrecioBase,
            fila.Producto.PermitePersonalizacion,
            fila.Producto.Activo,
            zonas,
            variantesDto);
    }

    private async Task<VarianteAdminDto> MapearVarianteAsync(
        int idVariante,
        decimal precioBase,
        CancellationToken cancellationToken)
    {
        var variante = await db.VarianteProducto.AsNoTracking()
            .FirstAsync(v => v.IdVariante == idVariante, cancellationToken);

        var forma = await db.PlantillaVarianteZona.AsNoTracking()
            .Where(p => p.IdVariante == idVariante && p.Activo)
            .Select(p => p.Forma)
            .FirstOrDefaultAsync(cancellationToken);

        return new VarianteAdminDto(
            variante.IdVariante,
            variante.CodigoVariante,
            variante.Nombre,
            variante.Descripcion,
            variante.PrecioAdicional,
            precioBase + variante.PrecioAdicional,
            variante.Activo,
            forma);
    }

    private void AsegurarZonasLadoAB(int idProducto, DateTime ahora)
    {
        var existentes = db.ZonaPersonalizacion
            .Where(z => z.IdProducto == idProducto)
            .ToList();

        AsegurarZona(existentes, idProducto, "Lado A", 1, ahora);
        AsegurarZona(existentes, idProducto, "Lado B", 2, ahora);
    }

    private void AsegurarZona(
        IReadOnlyList<ZonaPersonalizacion> existentes,
        int idProducto,
        string nombre,
        int orden,
        DateTime ahora)
    {
        var zona = existentes.FirstOrDefault(z => z.Nombre == nombre);
        if (zona is null)
        {
            db.ZonaPersonalizacion.Add(new ZonaPersonalizacion
            {
                IdProducto = idProducto,
                Nombre = nombre,
                EsObligatoria = true,
                OrdenVisual = orden,
                Activo = true,
                FechaCreacion = ahora
            });
            return;
        }

        if (!zona.Activo)
        {
            Activar(zona, ahora);
        }
    }

    private async Task AsegurarPlantillasAsync(
        int idVariante,
        int idProducto,
        string forma,
        DateTime ahora,
        CancellationToken cancellationToken)
    {
        var zonas = await db.ZonaPersonalizacion
            .Where(z => z.IdProducto == idProducto && z.Activo)
            .ToListAsync(cancellationToken);

        foreach (var zona in zonas)
        {
            var plantilla = await db.PlantillaVarianteZona
                .FirstOrDefaultAsync(
                    p => p.IdVariante == idVariante && p.IdZona == zona.IdZona,
                    cancellationToken);

            if (plantilla is null)
            {
                db.PlantillaVarianteZona.Add(new PlantillaVarianteZona
                {
                    IdVariante = idVariante,
                    IdZona = zona.IdZona,
                    Forma = forma,
                    AnchoLienzo = LlaveroMedidas.LienzoPx,
                    AltoLienzo = LlaveroMedidas.LienzoPx,
                    Activo = true,
                    FechaCreacion = ahora
                });
                continue;
            }

            plantilla.Forma = forma;
            plantilla.AnchoLienzo = LlaveroMedidas.LienzoPx;
            plantilla.AltoLienzo = LlaveroMedidas.LienzoPx;
            plantilla.FechaActualizacion = ahora;
            if (!plantilla.Activo)
            {
                Activar(plantilla, ahora);
            }
        }
    }

    private async Task<Categoria> ObtenerCategoriaAsync(int idCategoria, CancellationToken cancellationToken)
    {
        return await db.Categoria.FirstOrDefaultAsync(c => c.IdCategoria == idCategoria, cancellationToken)
            ?? throw new NotFoundException("La categoría no existe.");
    }

    private async Task<Categoria> ObtenerCategoriaActivaAsync(int idCategoria, CancellationToken cancellationToken)
    {
        var categoria = await ObtenerCategoriaAsync(idCategoria, cancellationToken);
        if (!categoria.Activo)
        {
            throw new BusinessRuleException("La categoría no está activa.");
        }

        return categoria;
    }

    private async Task<Producto> ObtenerProductoPorCodigoAsync(string codigoProducto, CancellationToken cancellationToken)
    {
        var codigo = codigoProducto.Trim();
        return await db.Producto.FirstOrDefaultAsync(p => p.CodigoProducto == codigo, cancellationToken)
            ?? throw new NotFoundException("El producto no existe.");
    }

    private async Task<VarianteProducto> ObtenerVarianteAsync(int idVariante, CancellationToken cancellationToken)
    {
        return await db.VarianteProducto.FirstOrDefaultAsync(v => v.IdVariante == idVariante, cancellationToken)
            ?? throw new NotFoundException("La variante no existe.");
    }

    private async Task<AreaEntrega> ObtenerAreaAsync(int idAreaEntrega, CancellationToken cancellationToken)
    {
        return await db.AreaEntrega.FirstOrDefaultAsync(a => a.IdAreaEntrega == idAreaEntrega, cancellationToken)
            ?? throw new NotFoundException("El área de entrega no existe.");
    }

    private async Task AsegurarNombreCategoriaLibreAsync(string nombre, int? idExcepto, CancellationToken cancellationToken)
    {
        var existe = await db.Categoria.AnyAsync(
            c => c.Nombre == nombre && (idExcepto == null || c.IdCategoria != idExcepto),
            cancellationToken);
        if (existe)
        {
            throw new ConflictException("Ya existe una categoría con ese nombre.");
        }
    }

    private async Task AsegurarCodigoProductoLibreAsync(string codigo, int? idExcepto, CancellationToken cancellationToken)
    {
        var existe = await db.Producto.AnyAsync(
            p => p.CodigoProducto == codigo && (idExcepto == null || p.IdProducto != idExcepto),
            cancellationToken);
        if (existe)
        {
            throw new ConflictException("Ya existe un producto con ese código.");
        }
    }

    private async Task AsegurarCodigoVarianteLibreAsync(string codigo, int? idExcepto, CancellationToken cancellationToken)
    {
        var existe = await db.VarianteProducto.AnyAsync(
            v => v.CodigoVariante == codigo && (idExcepto == null || v.IdVariante != idExcepto),
            cancellationToken);
        if (existe)
        {
            throw new ConflictException("Ya existe una variante con ese código.");
        }
    }

    private async Task AsegurarNombreVarianteLibreAsync(
        int idProducto,
        string nombre,
        int? idExcepto,
        CancellationToken cancellationToken)
    {
        var existe = await db.VarianteProducto.AnyAsync(
            v => v.IdProducto == idProducto
                 && v.Nombre == nombre
                 && (idExcepto == null || v.IdVariante != idExcepto),
            cancellationToken);
        if (existe)
        {
            throw new ConflictException("Ya existe una variante con ese nombre en el producto.");
        }
    }

    private async Task AsegurarNombreAreaLibreAsync(string nombre, int? idExcepto, CancellationToken cancellationToken)
    {
        var existe = await db.AreaEntrega.AnyAsync(
            a => a.Nombre == nombre && (idExcepto == null || a.IdAreaEntrega != idExcepto),
            cancellationToken);
        if (existe)
        {
            throw new ConflictException("Ya existe un área de entrega con ese nombre.");
        }
    }

    private async Task GuardarConUnicidadAsync(string conflicto, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(conflicto);
        }
    }

    private void Auditar(
        int idUsuarioInterno,
        string accion,
        string entidad,
        int idEntidad,
        string detalle,
        DateTime fecha)
    {
        db.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdTipoActor = (int)TipoActorId.UsuarioInterno,
            IdUsuarioInterno = idUsuarioInterno,
            Accion = accion,
            Entidad = entidad,
            IdEntidad = idEntidad,
            Resultado = "EXITOSO",
            Detalle = Truncar(detalle, 1000),
            FechaHora = fecha
        });
    }

    private static void Desactivar(dynamic entidad, DateTime ahora)
    {
        if (!entidad.Activo)
        {
            throw new BusinessRuleException("El registro ya está desactivado.");
        }

        entidad.Activo = false;
        entidad.FechaDesactivacion = ahora;
        entidad.FechaActualizacion = ahora;
    }

    private static void Activar(dynamic entidad, DateTime ahora)
    {
        if (entidad.Activo)
        {
            throw new BusinessRuleException("El registro ya está activo.");
        }

        entidad.Activo = true;
        entidad.FechaDesactivacion = null;
        entidad.FechaActualizacion = ahora;
    }

    private static CategoriaAdminDto MapearCategoria(Categoria c)
        => new(c.IdCategoria, c.Nombre, c.Descripcion, c.Activo);

    private static AreaEntregaAdminDto MapearArea(AreaEntrega a)
        => new(a.IdAreaEntrega, a.Nombre, a.Descripcion, a.Activo);

    private static void ValidarPrecioBase(decimal precio)
    {
        if (precio <= 0)
        {
            throw new BusinessRuleException("El precio base debe ser mayor que cero.");
        }
    }

    private static void ValidarPrecioAdicional(decimal precio)
    {
        if (precio < 0)
        {
            throw new BusinessRuleException("El precio adicional no puede ser negativo.");
        }
    }

    private static string NormalizarForma(string? forma)
    {
        var valor = string.IsNullOrWhiteSpace(forma) ? "CIRCULAR" : forma.Trim().ToUpperInvariant();
        if (valor is not ("CIRCULAR" or "CUADRADA"))
        {
            throw new BusinessRuleException("La forma del lienzo debe ser CIRCULAR o CUADRADA.");
        }

        return valor;
    }

    private static string NormalizarCodigo(string valor, int max, string campo)
    {
        var codigo = Requerido(valor, max, campo).ToUpperInvariant();
        foreach (var ch in codigo)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_')
            {
                continue;
            }

            throw new BusinessRuleException($"{campo} solo admite letras, números, guion y guion bajo.");
        }

        return codigo;
    }

    private static string Requerido(string? valor, int max, string campo)
    {
        var limpio = (valor ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(limpio))
        {
            throw new BusinessRuleException($"{campo} es obligatorio.");
        }

        if (limpio.Length > max)
        {
            throw new BusinessRuleException($"{campo} no puede superar {max} caracteres.");
        }

        return limpio;
    }

    private static string? Opcional(string? valor, int max)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = valor.Trim();
        return limpio.Length <= max ? limpio : limpio[..max];
    }

    private static string Truncar(string valor, int max)
        => valor.Length <= max ? valor : valor[..max];
}
