using Microsoft.EntityFrameworkCore;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Repositories;

public sealed class BuyerBiometricStore(NextTechDbContext db) : IBuyerBiometricStore
{
    public async Task<BuyerBiometricCredential?> GetActiveAsync(long buyerId, CancellationToken ct)
    {
        var entity = await db.BiometriaComprador
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdCompradorExterno == buyerId && x.Activo, ct);

        return entity is null ? null : Map(entity);
    }

    public async Task<BuyerBiometricCredential> UpsertAsync(
        BuyerBiometricCredential credential,
        CancellationToken ct)
    {
        var entity = await db.BiometriaComprador
            .SingleOrDefaultAsync(x => x.IdCompradorExterno == credential.BuyerId, ct);

        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new BuyerBiometricCredentialEntity
            {
                IdCompradorExterno = credential.BuyerId,
                FechaEnrolamiento = ToUtcDateTime(credential.EnrolledAtUtc),
                Activo = true
            };
            db.BiometriaComprador.Add(entity);
        }
        else
        {
            entity.FechaActualizacion = now;
            entity.Activo = true;
        }

        entity.BiometricTemplate = credential.BiometricTemplate;
        entity.TemplateVersion = credential.TemplateVersion;
        entity.TemplateKeyId = credential.TemplateKeyId;
        entity.TemplateModel = credential.TemplateModel;
        entity.TemplateDimensions = credential.TemplateDimensions;
        entity.EmbeddingSha256 = credential.EmbeddingSha256;
        entity.RetratoDatos = credential.PortraitContent;
        entity.RetratoMime = credential.PortraitContentType;
        entity.RetratoAncho = credential.PortraitWidth;
        entity.RetratoAlto = credential.PortraitHeight;
        entity.RetratoFondo = credential.PortraitBackground;

        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static BuyerBiometricCredential Map(BuyerBiometricCredentialEntity entity)
        => new(
            entity.IdCompradorExterno,
            entity.BiometricTemplate,
            entity.TemplateVersion,
            entity.TemplateKeyId,
            entity.TemplateModel,
            entity.TemplateDimensions,
            entity.EmbeddingSha256,
            entity.RetratoDatos,
            entity.RetratoMime,
            entity.RetratoAncho,
            entity.RetratoAlto,
            entity.RetratoFondo,
            new DateTimeOffset(DateTime.SpecifyKind(entity.FechaEnrolamiento, DateTimeKind.Utc)),
            entity.FechaActualizacion is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(entity.FechaActualizacion.Value, DateTimeKind.Utc)));

    private static DateTime ToUtcDateTime(DateTimeOffset value)
        => value.UtcDateTime;
}
