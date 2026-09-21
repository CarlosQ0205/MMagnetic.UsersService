using Microsoft.AspNetCore.Http;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;

namespace MMagnetic.ExogenaService.Services.Clientes;

public interface IDatoFinancieroService
{
    Task<IReadOnlyList<DatoFinanciero>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<ResultadoOperacion<DatoFinanciero>> CrearAsync(DatoFinancieroDto dto, CancellationToken cancellationToken = default);
    Task<ResultadoOperacion<DatoFinanciero>> ActualizarAsync(Guid datoFinancieroId, DatoFinancieroDto dto, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(Guid datoFinancieroId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga por lote con upsert: si ya existe un registro para el mismo cliente, cuenta
    /// y período, lo actualiza en vez de duplicarlo (soporta recargar un archivo corregido).
    /// </summary>
    Task<ResultadoCargaMasiva> CargarMasivoAsync(IFormFile archivo, CancellationToken cancellationToken = default);
}
