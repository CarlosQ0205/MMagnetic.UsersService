using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clientes;

    public ClientesController(IClienteService clientes)
    {
        _clientes = clientes;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        => Ok(await _clientes.ListarAsync(cancellationToken));

    [HttpGet("{clienteId:guid}")]
    public async Task<IActionResult> Obtener(Guid clienteId, CancellationToken cancellationToken)
    {
        var cliente = await _clientes.ObtenerAsync(clienteId, cancellationToken);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    /// <summary>Alta manual de un cliente (formulario del frontend).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ClienteDto dto, CancellationToken cancellationToken)
    {
        var resultado = await _clientes.CrearAsync(dto, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Dato) : BadRequest(resultado.Errores);
    }

    /// <summary>Edición manual de un cliente (ajustes desde el frontend).</summary>
    [HttpPut("{clienteId:guid}")]
    public async Task<IActionResult> Actualizar(Guid clienteId, [FromBody] ClienteDto dto, CancellationToken cancellationToken)
    {
        var resultado = await _clientes.ActualizarAsync(clienteId, dto, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Dato) : BadRequest(resultado.Errores);
    }

    /// <summary>Baja lógica (EsActivo = false); no borra el histórico.</summary>
    [HttpDelete("{clienteId:guid}")]
    public async Task<IActionResult> Desactivar(Guid clienteId, CancellationToken cancellationToken)
        => await _clientes.DesactivarAsync(clienteId, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Carga masiva desde un archivo .xlsx o .csv.</summary>
    [HttpPost("carga-masiva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CargarMasivo(IFormFile archivo, CancellationToken cancellationToken)
    {
        if (archivo.Length == 0)
            return BadRequest("El archivo está vacío.");

        var resultado = await _clientes.CargarMasivoAsync(archivo, cancellationToken);
        return Ok(resultado);
    }
}
