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

    /// <summary>
    /// Si ya existe un cliente con el mismo TipoDocumento+NumeroDocumento lo actualiza; si no,
    /// lo crea. Usado por la carga de corregidos de Formato 1019, donde el cliente casi siempre
    /// ya existe (es quien tuvo el error) y se le está corrigiendo un dato.
    /// </summary>
    Task<ResultadoOperacion<Cliente>> CrearOActualizarPorDocumentoAsync(ClienteDto dto, CancellationToken cancellationToken = default);
}
