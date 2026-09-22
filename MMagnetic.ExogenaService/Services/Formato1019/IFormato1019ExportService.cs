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

    /// <summary>
    /// Arma F_1019_Definitivo del período en uno o varios archivos XML, de máximo 5000
    /// "movcta" cada uno (el límite del Anexo 2). NumEnvio/CodCpt/fechas se autocompletan
    /// (Inserción, año del período) porque el pre-validador de la DIAN es quien terminará
    /// asignando esos valores reales antes del envío definitivo.
    /// </summary>
    Task<ResultadoLotesExportacion> GenerarLotesXmlAsync(int periodoAno, CancellationToken cancellationToken = default);

    /// <summary>Igual que <see cref="GenerarLotesXmlAsync"/> pero cada archivo es un .xlsx con las mismas columnas que la descarga de F_1019_Definitivo.</summary>
    Task<ResultadoLotesExportacion> GenerarLotesExcelAsync(int periodoAno, CancellationToken cancellationToken = default);
}
