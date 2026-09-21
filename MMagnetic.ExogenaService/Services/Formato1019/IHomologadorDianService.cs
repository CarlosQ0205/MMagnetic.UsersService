namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IHomologadorDianService
{
    Task<ResultadoHomologacion> HomologarAsync(
        int? paisId,
        int? departamentoId,
        int? municipioId,
        CancellationToken cancellationToken = default);
}
