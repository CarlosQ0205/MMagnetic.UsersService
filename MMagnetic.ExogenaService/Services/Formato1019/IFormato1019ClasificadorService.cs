namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019ClasificadorService
{
    /// <summary>
    /// Ensambla, homologa y valida las cuentas del período indicado; persiste el detalle
    /// numérico en Formato_1019, los registros inválidos (con su detalle) en Formato_Errores,
    /// y copia los válidos en F_1019_Definitivo.
    /// </summary>
    Task<ResumenClasificacion> ClasificarYPersistirAsync(int periodoAno, Guid? usuarioId, CancellationToken cancellationToken = default);
}
