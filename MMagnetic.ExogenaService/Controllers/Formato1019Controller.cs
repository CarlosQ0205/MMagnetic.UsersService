using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Formatos;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Formato1019Controller : ControllerBase
{
    private readonly IFormato1019ClasificadorService _clasificador;
    private readonly FormatosDbContext _formatos;
    private readonly ClientesDbContext _clientes;

    public Formato1019Controller(
        IFormato1019ClasificadorService clasificador,
        FormatosDbContext formatos,
        ClientesDbContext clientes)
    {
        _clasificador = clasificador;
        _formatos = formatos;
        _clientes = clientes;
    }

    /// <summary>
    /// Ensambla, homologa y valida todas las cuentas del período (año) indicado.
    /// Reemplaza cualquier resultado previo de ese mismo período (es seguro volver a
    /// llamarlo después de corregir datos de origen).
    /// </summary>
    [HttpPost("clasificar/{periodoAno:int}")]
    public async Task<ActionResult<ResumenClasificacion>> Clasificar(int periodoAno, CancellationToken cancellationToken)
    {
        var resumen = await _clasificador.ClasificarYPersistirAsync(periodoAno, usuarioId: null, cancellationToken);
        return Ok(resumen);
    }

    /// <summary>Errores encontrados en el último proceso de clasificación de un período, con el número de documento del cliente para ubicarlo fácilmente.</summary>
    [HttpGet("errores/{periodoAno:int}")]
    public async Task<ActionResult<IReadOnlyList<ErrorFormato1019Dto>>> ObtenerErrores(int periodoAno, CancellationToken cancellationToken)
    {
        var clienteIds = await _formatos.Formato1019
            .Where(f => f.PeriodoAno == periodoAno)
            .Select(f => f.ClienteId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var errores = await _formatos.FormatoErrores
            .Where(e => clienteIds.Contains(e.ClienteId))
            .OrderBy(e => e.ClienteId)
            .ToListAsync(cancellationToken);

        var clientesPorId = await _clientes.Clientes
            .Where(c => clienteIds.Contains(c.ClienteId))
            .ToDictionaryAsync(c => c.ClienteId, c => c.NumeroDocumento, cancellationToken);

        var resultado = errores.Select(e => new ErrorFormato1019Dto(
            e.ErrorId,
            e.ClienteId,
            e.ClienteId.HasValue && clientesPorId.TryGetValue(e.ClienteId.Value, out var numeroDocumento) ? numeroDocumento : null,
            e.CodigoError,
            e.DescripcionError,
            e.ValorInvalido,
            e.Nivel,
            e.FechaError)).ToList();

        return Ok(resultado);
    }

    /// <summary>Registros ya validados y listos para exportar de un período.</summary>
    [HttpGet("definitivo/{periodoAno:int}")]
    public async Task<ActionResult<IReadOnlyList<Formato1019Definitivo>>> ObtenerDefinitivo(int periodoAno, CancellationToken cancellationToken)
    {
        var registros = await _formatos.Formato1019Definitivo
            .Where(f => f.PeriodoAno == periodoAno)
            .OrderBy(f => f.ClienteId)
            .ThenBy(f => f.Linea)
            .ToListAsync(cancellationToken);

        return Ok(registros);
    }
}
