namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019ErrorExportService
{
    /// <summary>
    /// Genera un .xlsx con una fila por cada línea (cuenta) que falló, con todos los campos
    /// del Formato 1019 (los de Cliente y los de Datos_Financieros juntos) más una columna
    /// "Errores" — la línea completa, no solo el dato puntual que causó el error. Pensado
    /// para bajar, corregir en Excel y volver a subir tal cual por el endpoint de corregidos
    /// (POST api/formato1019/corregidos), sin separar por tabla.
    /// </summary>
    Task<byte[]> ExportarErroresAsync(int periodoAno, CancellationToken cancellationToken = default);
}
