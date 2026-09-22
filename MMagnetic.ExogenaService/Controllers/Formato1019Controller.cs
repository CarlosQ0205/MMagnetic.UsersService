using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Models.Formatos;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class Formato1019Controller : ControllerBase
{
    private readonly IFormato1019ClasificadorService _clasificador;
    private readonly IFormato1019ExportService _exportador;
    private readonly IFormato1019ErrorExportService _exportadorErrores;
    private readonly IFormato1019CorreccionService _correccion;
    private readonly FormatosDbContext _formatos;
    private readonly ClientesDbContext _clientes;

    public Formato1019Controller(
        IFormato1019ClasificadorService clasificador,
        IFormato1019ExportService exportador,
        IFormato1019ErrorExportService exportadorErrores,
        IFormato1019CorreccionService correccion,
        FormatosDbContext formatos,
        ClientesDbContext clientes)
    {
        _clasificador = clasificador;
        _exportador = exportador;
        _exportadorErrores = exportadorErrores;
        _correccion = correccion;
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

    /// <summary>
    /// Borra el resultado de un período (Formato_1019, Formato_Errores y F_1019_Definitivo) para
    /// poder rehacer todo el proceso de clasificación desde cero.
    /// </summary>
    [HttpDelete("{periodoAno:int}")]
    public async Task<IActionResult> Limpiar(int periodoAno, CancellationToken cancellationToken)
    {
        await _clasificador.LimpiarAsync(periodoAno, cancellationToken);
        return NoContent();
    }

    /// <summary>Detalle completo (staging) de Formato_1019 para un período: todos los conceptos procesados, hayan pasado o no la validación.</summary>
    [HttpGet("{periodoAno:int}")]
    public async Task<ActionResult<IReadOnlyList<Formato1019ConceptoDto>>> ObtenerStaging(int periodoAno, CancellationToken cancellationToken)
        => Ok(await ObtenerStagingDatosAsync(periodoAno, cancellationToken));

    /// <summary>Descarga en .xlsx el detalle completo (staging) de Formato_1019 de un período.</summary>
    [HttpGet("{periodoAno:int}/exportar")]
    public async Task<IActionResult> ExportarStaging(int periodoAno, CancellationToken cancellationToken)
    {
        var datos = await ObtenerStagingDatosAsync(periodoAno, cancellationToken);
        return File(
            GenerarExcelConceptos(datos),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"formato1019_{periodoAno}.xlsx");
    }

    /// <summary>Errores encontrados en el último proceso de clasificación de un período, con el número de documento del cliente para ubicarlo fácilmente.</summary>
    [HttpGet("errores/{periodoAno:int}")]
    public async Task<ActionResult<IReadOnlyList<ErrorFormato1019Dto>>> ObtenerErrores(int periodoAno, CancellationToken cancellationToken)
        => Ok(await ObtenerErroresDatosAsync(periodoAno, cancellationToken));

    /// <summary>
    /// Descarga en .xlsx la línea completa (no solo el dato puntual) de cada cliente/cuenta
    /// que no pasó la validación de un período, con sus errores, lista para corregir y
    /// volver a subir por los mismos endpoints de carga masiva (Clientes / Datos financieros).
    /// </summary>
    [HttpGet("errores/{periodoAno:int}/exportar")]
    public async Task<IActionResult> ExportarErrores(int periodoAno, CancellationToken cancellationToken)
    {
        var contenido = await _exportadorErrores.ExportarErroresAsync(periodoAno, cancellationToken);
        return File(
            contenido,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"errores_formato1019_{periodoAno}.xlsx");
    }

    /// <summary>
    /// Sube el archivo de corregidos (el mismo formato de "errores/{periodoAno}/exportar", ya
    /// corregido): por cada fila actualiza el Cliente y el Dato_Financiero correspondientes.
    /// </summary>
    [HttpPost("corregidos")]
    public async Task<ActionResult<ResultadoCargaMasiva>> SubirCorregidos(IFormFile archivo, CancellationToken cancellationToken)
        => Ok(await _correccion.CargarCorregidosAsync(archivo, cancellationToken));

    /// <summary>Registros ya validados y listos para exportar de un período.</summary>
    [HttpGet("definitivo/{periodoAno:int}")]
    public async Task<ActionResult<IReadOnlyList<Formato1019ConceptoDto>>> ObtenerDefinitivo(int periodoAno, CancellationToken cancellationToken)
        => Ok(await ObtenerDefinitivoDatosAsync(periodoAno, cancellationToken));

    /// <summary>Descarga en .xlsx los registros de F_1019_Definitivo de un período.</summary>
    [HttpGet("definitivo/{periodoAno:int}/exportar")]
    public async Task<IActionResult> ExportarDefinitivo(int periodoAno, CancellationToken cancellationToken)
    {
        var datos = await ObtenerDefinitivoDatosAsync(periodoAno, cancellationToken);
        return File(
            GenerarExcelConceptos(datos),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"f1019_definitivo_{periodoAno}.xlsx");
    }

    /// <summary>
    /// Genera el archivo XML final para la DIAN a partir de F_1019_Definitivo. Corre una
    /// validación completa del lote antes de generarlo (incluida la llave única entre
    /// registros de distintos clientes, que no se valida durante la clasificación).
    /// </summary>
    [HttpGet("exportar/{periodoAno:int}")]
    public async Task<IActionResult> Exportar(
        int periodoAno,
        [FromQuery] int numEnvio,
        [FromQuery] int codCpt,
        [FromQuery] DateTime fecInicial,
        [FromQuery] DateTime fecFinal,
        CancellationToken cancellationToken)
    {
        var resultado = await _exportador.GenerarXmlAsync(periodoAno, numEnvio, codCpt, fecInicial, fecFinal, cancellationToken);

        if (!resultado.Exitoso)
            return BadRequest(resultado.Errores);

        return File(resultado.ContenidoXml!, "application/xml", resultado.NombreArchivo);
    }

    private async Task<List<Formato1019ConceptoDto>> ObtenerStagingDatosAsync(int periodoAno, CancellationToken cancellationToken)
    {
        var conceptos = await _formatos.Formato1019
            .Where(f => f.PeriodoAno == periodoAno)
            .OrderBy(f => f.ClienteId)
            .ThenBy(f => f.Linea)
            .ToListAsync(cancellationToken);

        var clienteIds = conceptos.Select(c => c.ClienteId).Distinct().ToList();
        var clientesPorId = await _clientes.Clientes
            .Where(c => clienteIds.Contains(c.ClienteId))
            .ToDictionaryAsync(c => c.ClienteId, c => c.NumeroDocumento, cancellationToken);

        return conceptos.Select(c => new Formato1019ConceptoDto(
            c.Formato1019Id,
            c.ClienteId,
            c.ClienteId.HasValue && clientesPorId.TryGetValue(c.ClienteId.Value, out var numeroDocumento) ? numeroDocumento : null,
            c.PeriodoAno,
            c.CodigoConcepto,
            c.Valor,
            c.Linea,
            c.FechaGeneracion)).ToList();
    }

    private async Task<List<ErrorFormato1019Dto>> ObtenerErroresDatosAsync(int periodoAno, CancellationToken cancellationToken)
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

        return errores.Select(e => new ErrorFormato1019Dto(
            e.ErrorId,
            e.ClienteId,
            e.ClienteId.HasValue && clientesPorId.TryGetValue(e.ClienteId.Value, out var numeroDocumento) ? numeroDocumento : null,
            e.CodigoError,
            e.DescripcionError,
            e.ValorInvalido,
            e.Nivel,
            e.FechaError)).ToList();
    }

    private async Task<List<Formato1019ConceptoDto>> ObtenerDefinitivoDatosAsync(int periodoAno, CancellationToken cancellationToken)
    {
        var registros = await _formatos.Formato1019Definitivo
            .Where(f => f.PeriodoAno == periodoAno)
            .OrderBy(f => f.ClienteId)
            .ThenBy(f => f.Linea)
            .ToListAsync(cancellationToken);

        var clienteIds = registros.Select(r => r.ClienteId).Distinct().ToList();
        var clientesPorId = await _clientes.Clientes
            .Where(c => clienteIds.Contains(c.ClienteId))
            .ToDictionaryAsync(c => c.ClienteId, c => c.NumeroDocumento, cancellationToken);

        return registros.Select(r => new Formato1019ConceptoDto(
            r.Formato1019DefinitivoId,
            r.ClienteId,
            r.ClienteId.HasValue && clientesPorId.TryGetValue(r.ClienteId.Value, out var numeroDocumento) ? numeroDocumento : null,
            r.PeriodoAno,
            r.CodigoConcepto,
            r.Valor,
            r.Linea,
            r.FechaGeneracion)).ToList();
    }

    private static byte[] GenerarExcelConceptos(IReadOnlyList<Formato1019ConceptoDto> conceptos)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Datos");
        string[] columnas = { "NumeroDocumentoCliente", "PeriodoAno", "Linea", "CodigoConcepto", "Valor", "FechaGeneracion" };
        for (var i = 0; i < columnas.Length; i++)
            hoja.Cell(1, i + 1).Value = columnas[i];

        var fila = 2;
        foreach (var concepto in conceptos)
        {
            hoja.Cell(fila, 1).Value = concepto.NumeroDocumentoCliente;
            hoja.Cell(fila, 2).Value = concepto.PeriodoAno;
            hoja.Cell(fila, 3).Value = concepto.Linea;
            hoja.Cell(fila, 4).Value = concepto.CodigoConcepto;
            hoja.Cell(fila, 5).Value = concepto.Valor;
            hoja.Cell(fila, 6).Value = concepto.FechaGeneracion;
            fila++;
        }

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
    }
}
