using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DatosFinancierosController : ControllerBase
{
    private readonly IDatoFinancieroService _datosFinancieros;

    public DatosFinancierosController(IDatoFinancieroService datosFinancieros)
    {
        _datosFinancieros = datosFinancieros;
    }

    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> ListarPorCliente(Guid clienteId, CancellationToken cancellationToken)
        => Ok(await _datosFinancieros.ListarPorClienteAsync(clienteId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] DatoFinancieroDto dto, CancellationToken cancellationToken)
    {
        var resultado = await _datosFinancieros.CrearAsync(dto, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Dato) : BadRequest(resultado.Errores);
    }

    [HttpPut("{datoFinancieroId:guid}")]
    public async Task<IActionResult> Actualizar(Guid datoFinancieroId, [FromBody] DatoFinancieroDto dto, CancellationToken cancellationToken)
    {
        var resultado = await _datosFinancieros.ActualizarAsync(datoFinancieroId, dto, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Dato) : BadRequest(resultado.Errores);
    }

    [HttpDelete("{datoFinancieroId:guid}")]
    public async Task<IActionResult> Eliminar(Guid datoFinancieroId, CancellationToken cancellationToken)
        => await _datosFinancieros.EliminarAsync(datoFinancieroId, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Carga masiva con upsert por (cliente, cuenta, período): recargar el mismo archivo corregido actualiza en vez de duplicar.</summary>
    [HttpPost("carga-masiva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CargarMasivo(IFormFile archivo, CancellationToken cancellationToken)
    {
        if (archivo.Length == 0)
            return BadRequest("El archivo está vacío.");

        var resultado = await _datosFinancieros.CargarMasivoAsync(archivo, cancellationToken);
        return Ok(resultado);
    }
}
