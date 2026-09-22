namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019ClasificadorService
{
    /// <summary>
    /// Ensambla, homologa y valida las cuentas del período indicado; persiste el detalle
    /// numérico en Formato_1019, los registros inválidos (con su detalle) en Formato_Errores,
    /// y copia los válidos en F_1019_Definitivo.
    /// </summary>
    Task<ResumenClasificacion> ClasificarYPersistirAsync(int periodoAno, Guid? usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra el resultado de un período (Formato_1019, Formato_Errores y F_1019_Definitivo)
    /// sin volver a generarlo, para permitir rehacer todo el proceso de clasificación desde cero.
    /// </summary>
    Task LimpiarAsync(int periodoAno, CancellationToken cancellationToken = default);
}
