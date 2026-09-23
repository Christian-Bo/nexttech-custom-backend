using Microsoft.EntityFrameworkCore;
using NextTech.Application.Common.Files;
using NextTech.Application.DTOs.Personalization;
using NextTech.Application.Modules.Personalization;
using NextTech.Domain.Exceptions;
using NextTech.Infrastructure.Persistence.SqlServer;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Store;

public sealed class PersonalizationService(NextTechDbContext db) : IPersonalizationService
{
    public async Task<PersonalizacionDto> CrearAsync(
        long idCompradorExterno,
        CrearPersonalizacionRequest request,
        CancellationToken cancellationToken)
    {
        _ = idCompradorExterno;

        var variante = await db.VarianteProducto
            .FirstOrDefaultAsync(v => v.IdVariante == request.IdVariante && v.Activo, cancellationToken);

        if (variante is null)
        {
            throw new NotFoundException("La variante no existe o no está activa.");
        }

        var producto = await db.Producto
            .FirstOrDefaultAsync(p => p.IdProducto == variante.IdProducto && p.Activo, cancellationToken);

        if (producto is null)
        {
            throw new NotFoundException("El producto de la variante no está disponible.");
        }

        if (!producto.PermitePersonalizacion)
        {
            throw new BusinessRuleException("Este producto no admite personalización.");
        }

        var zonasProducto = await db.ZonaPersonalizacion
            .Where(z => z.IdProducto == producto.IdProducto && z.Activo)
            .ToListAsync(cancellationToken);

        var obligatorias = zonasProducto.Where(z => z.EsObligatoria).Select(z => z.IdZona).ToHashSet();
        var zonasEnviadas = request.Zonas.Select(z => z.IdZona).ToHashSet();

        if (!obligatorias.IsSubsetOf(zonasEnviadas))
        {
            throw new BusinessRuleException("Faltan zonas obligatorias (Lado A y Lado B).");
        }

        if (request.Zonas.Select(z => z.IdZona).Distinct().Count() != request.Zonas.Count)
        {
            throw new BusinessRuleException("No se puede repetir la misma zona en una personalización.");
        }

        var idsZonaProducto = zonasProducto.Select(z => z.IdZona).ToHashSet();
        if (request.Zonas.Any(z => !idsZonaProducto.Contains(z.IdZona)))
        {
            throw new BusinessRuleException("Hay una zona que no pertenece al producto de la variante.");
        }

        var ahora = DateTime.Now;
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var personalizacion = new Personalizacion
            {
                IdVariante = variante.IdVariante,
                Bloqueada = false,
                FechaCreacion = ahora
            };

            db.Personalizacion.Add(personalizacion);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var zona in request.Zonas)
            {
                PersonalizationJsonValidator.Validar(zona.ConfiguracionJson);

                var imagen = ImagePayloadParser.Parse(
                    zona.ImagenBase64,
                    zona.NombreArchivo,
                    zona.TipoMime);

                var archivo = new Archivo
                {
                    NombreOriginal = string.IsNullOrWhiteSpace(zona.NombreArchivo)
                        ? $"zona-{zona.IdZona}{imagen.Extension}"
                        : Path.GetFileName(zona.NombreArchivo),
                    TipoMime = imagen.Mime,
                    Extension = imagen.Extension,
                    Datos = imagen.Datos,
                    TamanoBytes = imagen.Datos.Length,
                    FechaCreacion = ahora
                };

                db.Archivo.Add(archivo);
                await db.SaveChangesAsync(cancellationToken);

                db.PersonalizacionZona.Add(new PersonalizacionZona
                {
                    IdPersonalizacion = personalizacion.IdPersonalizacion,
                    IdZona = zona.IdZona,
                    IdArchivoImagenFinal = archivo.IdArchivo,
                    ConfiguracionJson = zona.ConfiguracionJson,
                    FechaCreacion = ahora
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var zonasGuardadas = await db.PersonalizacionZona.AsNoTracking()
                .Where(z => z.IdPersonalizacion == personalizacion.IdPersonalizacion)
                .Select(z => new ZonaPersonalizacionDto(z.IdZona, z.IdArchivoImagenFinal, z.ConfiguracionJson))
                .ToListAsync(cancellationToken);

            return new PersonalizacionDto(
                personalizacion.IdPersonalizacion,
                personalizacion.IdVariante,
                personalizacion.Bloqueada,
                zonasGuardadas);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PersonalizacionDto> ObtenerAsync(
        int idPersonalizacion,
        CancellationToken cancellationToken)
    {
        var personalizacion = await db.Personalizacion.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPersonalizacion == idPersonalizacion, cancellationToken);

        if (personalizacion is null)
        {
            throw new NotFoundException("La personalización no existe.");
        }

        var zonas = await db.PersonalizacionZona.AsNoTracking()
            .Where(z => z.IdPersonalizacion == personalizacion.IdPersonalizacion)
            .Select(z => new ZonaPersonalizacionDto(z.IdZona, z.IdArchivoImagenFinal, z.ConfiguracionJson))
            .ToListAsync(cancellationToken);

        return new PersonalizacionDto(
            personalizacion.IdPersonalizacion,
            personalizacion.IdVariante,
            personalizacion.Bloqueada,
            zonas);
    }
}
