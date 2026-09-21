namespace MMagnetic.ExogenaService.Services.Clientes;

public record ResultadoResolucionCatalogo(int? PaisId, int? DepartamentoId, int? MunicipioId, IReadOnlyList<string> Errores)
{
    public bool EsValido => Errores.Count == 0;
}

/// <summary>
/// Traduce códigos DIAN (ej. "169", "05", "001") a los Id internos de MM_DIAN. Es el
/// inverso de IHomologadorDianService (que va de Id -> código); este va de código -> Id,
/// que es lo que necesita el alta de un Cliente (manual o por carga masiva), donde el
/// usuario diligencia el código oficial, no el Id autonumérico de la tabla.
/// </summary>
public interface IResolutorCatalogoDianService
{
    Task<ResultadoResolucionCatalogo> ResolverAsync(
        string? codigoPais,
        string? codigoDepartamento,
        string? codigoMunicipio,
        CancellationToken cancellationToken = default);
}
