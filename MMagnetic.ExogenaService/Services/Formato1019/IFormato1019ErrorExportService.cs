namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019ErrorExportService
{
    /// <summary>
    /// Genera un .xlsx con dos hojas ("Clientes" y "DatosFinancieros"), cada una con las
    /// mismas columnas que su carga masiva correspondiente más una columna "Errores": la
    /// línea completa de cada cliente/cuenta que falló, no solo el dato puntual que causó
    /// el error. Pensado para bajar, corregir en Excel y volver a subir por los mismos
    /// endpoints de carga masiva.
    /// </summary>
    Task<byte[]> ExportarErroresAsync(int periodoAno, CancellationToken cancellationToken = default);
}
