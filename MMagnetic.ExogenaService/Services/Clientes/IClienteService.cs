using Microsoft.AspNetCore.Http;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;

namespace MMagnetic.ExogenaService.Services.Clientes;

public interface IClienteService
{
    Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Cliente?> ObtenerAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<ResultadoOperacion<Cliente>> CrearAsync(ClienteDto dto, CancellationToken cancellationToken = default);
    Task<ResultadoOperacion<Cliente>> ActualizarAsync(Guid clienteId, ClienteDto dto, CancellationToken cancellationToken = default);
    Task<bool> DesactivarAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<ResultadoCargaMasiva> CargarMasivoAsync(IFormFile archivo, CancellationToken cancellationToken = default);
}
