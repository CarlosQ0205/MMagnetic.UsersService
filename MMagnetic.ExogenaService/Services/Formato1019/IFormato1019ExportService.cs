namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019ExportService
{
    /// <summary>
    /// Arma el archivo XML final para la DIAN a partir de los registros ya validados en
    /// F_1019_Definitivo para el período dado. Antes de generarlo, corre una validación
    /// completa del lote (encabezado + llave única entre registros), que es la única
    /// que puede detectar duplicados entre cuentas de distintos clientes.
    /// </summary>
    Task<ResultadoExportacion> GenerarXmlAsync(
        int periodoAno,
        int numEnvio,
        int codCpt,
        DateTime fecInicial,
        DateTime fecFinal,
        CancellationToken cancellationToken = default);
}
