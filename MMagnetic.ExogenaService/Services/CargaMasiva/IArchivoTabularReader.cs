using Microsoft.AspNetCore.Http;

namespace MMagnetic.ExogenaService.Services.CargaMasiva;

/// <summary>Lee un archivo .xlsx o .csv y lo devuelve como filas de {encabezado -> valor}, sin conocer el significado de las columnas.</summary>
public interface IArchivoTabularReader
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> LeerAsync(IFormFile archivo, CancellationToken cancellationToken = default);
}
