using Microsoft.AspNetCore.Http;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;

namespace MMagnetic.ExogenaService.Services.Clientes;

public interface ICotitularService
{
    Task<IReadOnlyList<Cotitular>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<ResultadoOperacion<Cotitular>> CrearAsync(CotitularDto dto, CancellationToken cancellationToken = default);
    Task<ResultadoOperacion<Cotitular>> ActualizarAsync(Guid cotitularId, CotitularDto dto, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(Guid cotitularId, CancellationToken cancellationToken = default);
    Task<ResultadoCargaMasiva> CargarMasivoAsync(IFormFile archivo, CancellationToken cancellationToken = default);
}
