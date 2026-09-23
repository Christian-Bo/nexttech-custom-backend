using NextTech.Application.DTOs.Personalization;

namespace NextTech.Application.Modules.Personalization;

public interface IPersonalizationService
{
    Task<PersonalizacionDto> CrearAsync(
        long idCompradorExterno,
        CrearPersonalizacionRequest request,
        CancellationToken cancellationToken);

    Task<PersonalizacionDto> ObtenerAsync(
        int idPersonalizacion,
        CancellationToken cancellationToken);
}
