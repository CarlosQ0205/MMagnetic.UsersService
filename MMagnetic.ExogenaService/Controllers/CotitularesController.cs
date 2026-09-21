using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CotitularesController : ControllerBase
{
    private readonly ICotitularService _cotitulares;

    public CotitularesController(ICotitularService cotitulares)
    {
        _cotitulares = cotitulares;
    }

    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> ListarPorCliente(Guid clienteId, CancellationToken cancellationToken)
        => Ok(await _cotitulares.ListarPorClienteAsync(clienteId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CotitularDto dto, CancellationToken cancellationToken)
    {
        var resultado = await _cotitulares.CrearAsync(dto, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Dato) : BadRequest(resultado.Errores);
    }

    [HttpPut("{cotitularId:guid}")]
    public async Task<IActionResult> Actualizar(Guid cotitularId, [FromBody] CotitularDto dto, CancellationToken cancellationToken)
    {
        var resultado = await _cotitulares.ActualizarAsync(cotitularId, dto, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Dato) : BadRequest(resultado.Errores);
    }

    [HttpDelete("{cotitularId:guid}")]
    public async Task<IActionResult> Eliminar(Guid cotitularId, CancellationToken cancellationToken)
        => await _cotitulares.EliminarAsync(cotitularId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("carga-masiva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CargarMasivo(IFormFile archivo, CancellationToken cancellationToken)
    {
        if (archivo.Length == 0)
            return BadRequest("El archivo está vacío.");

        var resultado = await _cotitulares.CargarMasivoAsync(archivo, cancellationToken);
        return Ok(resultado);
    }
}
