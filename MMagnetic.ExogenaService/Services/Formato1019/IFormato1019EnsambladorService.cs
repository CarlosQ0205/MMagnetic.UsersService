namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019EnsambladorService
{
    /// <summary>
    /// Arma un "movcta" por cada cuenta (Datos_Financieros) registrada para el período dado,
    /// tomando los datos del titular de Clientes, los cotitulares de Cotitulares (replicados
    /// en cada cuenta del cliente) y homologando país/depto/municipio contra MM_DIAN.
    /// </summary>
    Task<IReadOnlyList<RegistroFormato1019>> EnsamblarAsync(int periodoAno, CancellationToken cancellationToken = default);
}
