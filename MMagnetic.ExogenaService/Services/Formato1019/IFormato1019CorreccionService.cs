using Microsoft.AspNetCore.Http;
using MMagnetic.ExogenaService.Models.Dto;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019CorreccionService
{
    /// <summary>
    /// Sube el mismo archivo que se descarga desde "Descarga errores F-1019" (una fila por
    /// línea con error, con todos los campos de Cliente y de Datos_Financieros). Por cada fila
    /// actualiza (o crea si no existe) el Cliente y el Dato_Financiero correspondientes, para
    /// que al volver a apretar "Crear Formato 1019" la línea corregida se reclasifique.
    /// </summary>
    Task<ResultadoCargaMasiva> CargarCorregidosAsync(IFormFile archivo, CancellationToken cancellationToken = default);
}
