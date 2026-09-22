using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Formato1019;
using MMagnetic.ExogenaService.Models.Formatos;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019ClasificadorService : IFormato1019ClasificadorService
{
    private readonly IFormato1019EnsambladorService _ensamblador;
    private readonly IFormato1019Validator _validator;
    private readonly FormatosDbContext _formatos;

    public Formato1019ClasificadorService(
        IFormato1019EnsambladorService ensamblador,
        IFormato1019Validator validator,
        FormatosDbContext formatos)
    {
        _ensamblador = ensamblador;
        _validator = validator;
        _formatos = formatos;
    }

    public async Task<ResumenClasificacion> ClasificarYPersistirAsync(
        int periodoAno, Guid? usuarioId, CancellationToken cancellationToken = default)
    {
        // Idempotente: si el usuario corrige datos de origen y vuelve a clasificar el mismo
        // período, se reemplaza el resultado anterior en vez de acumular filas duplicadas
        // (que además chocarían contra la llave única de Formato_1019 tras la Migración 001).
        await LimpiarResultadoAnteriorAsync(periodoAno, cancellationToken);

        var registros = await _ensamblador.EnsamblarAsync(periodoAno, cancellationToken);
        var ahora = DateTime.UtcNow;
        var validos = 0;
        var conErrores = 0;

        foreach (var registro in registros)
        {
            var conceptos = ObtenerConceptos(registro.Movimiento)
                .Select(c => new Formato1019Concepto
                {
                    Formato1019Id = Guid.NewGuid(),
                    ClienteId = registro.ClienteId,
                    PeriodoAno = registro.PeriodoAno,
                    CodigoConcepto = c.Codigo,
                    Valor = c.Valor,
                    Linea = registro.NumeroCuenta,
                    FechaGeneracion = ahora,
                    UsuarioGenerador = usuarioId,
                })
                .ToList();

            _formatos.Formato1019.AddRange(conceptos);

            var resultadoValidacion = _validator.ValidarRegistro(registro.Movimiento);
            var erroresHomologacion = registro.ErroresHomologacion
                .Select(mensaje => new Formato1019ValidationError("HOMOLOGACION", mensaje));
            var errores = erroresHomologacion.Concat(resultadoValidacion.Errores).ToList();

            if (errores.Count == 0)
            {
                foreach (var concepto in conceptos)
                {
                    _formatos.Formato1019Definitivo.Add(new Formato1019Definitivo
                    {
                        Formato1019DefinitivoId = Guid.NewGuid(),
                        Formato1019Id = concepto.Formato1019Id,
                        ClienteId = concepto.ClienteId,
                        PeriodoAno = concepto.PeriodoAno,
                        CodigoConcepto = concepto.CodigoConcepto,
                        Valor = concepto.Valor,
                        Linea = concepto.Linea,
                        FechaGeneracion = ahora,
                        UsuarioGenerador = usuarioId,
                    });
                }

                validos++;
            }
            else
            {
                foreach (var error in errores)
                {
                    // Los códigos de validación siguen el patrón "MOV-{ATRIBUTO}" (ver
                    // Formato1019Validator), lo que permite enlazar el error a la fila
                    // concepto/valor exacta cuando aplica a un solo atributo.
                    var nombreConcepto = error.Codigo.StartsWith("MOV-", StringComparison.Ordinal)
                        ? error.Codigo["MOV-".Length..].ToLowerInvariant()
                        : null;
                    var conceptoRelacionado = nombreConcepto is not null
                        ? conceptos.Find(c => c.CodigoConcepto == nombreConcepto)
                        : null;

                    _formatos.FormatoErrores.Add(new FormatoError
                    {
                        ErrorId = Guid.NewGuid(),
                        Formato1019Id = conceptoRelacionado?.Formato1019Id,
                        ClienteId = registro.ClienteId,
                        CodigoError = error.Codigo,
                        DescripcionError = error.Mensaje,
                        ValorInvalido = $"cuenta {registro.NumeroCuenta}" + (error.Contexto is null ? "" : $" ({error.Contexto})"),
                        Nivel = "Error",
                        FechaError = ahora,
                        UsuarioProcesador = usuarioId,
                    });
                }

                conErrores++;
            }
        }

        await _formatos.SaveChangesAsync(cancellationToken);

        return new ResumenClasificacion(registros.Count, validos, conErrores);
    }

    public Task LimpiarAsync(int periodoAno, CancellationToken cancellationToken = default)
        => LimpiarResultadoAnteriorAsync(periodoAno, cancellationToken);

    private async Task LimpiarResultadoAnteriorAsync(int periodoAno, CancellationToken cancellationToken)
    {
        var clienteIdsPeriodoAnterior = await _formatos.Formato1019
            .Where(f => f.PeriodoAno == periodoAno)
            .Select(f => f.ClienteId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (clienteIdsPeriodoAnterior.Count > 0)
        {
            var erroresAnteriores = _formatos.FormatoErrores
                .Where(e => e.ClienteId != null && clienteIdsPeriodoAnterior.Contains(e.ClienteId));
            _formatos.FormatoErrores.RemoveRange(erroresAnteriores);
        }

        _formatos.Formato1019Definitivo.RemoveRange(_formatos.Formato1019Definitivo.Where(f => f.PeriodoAno == periodoAno));
        _formatos.Formato1019.RemoveRange(_formatos.Formato1019.Where(f => f.PeriodoAno == periodoAno));

        await _formatos.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<(string Codigo, decimal Valor)> ObtenerConceptos(MovimientoCuenta mov)
    {
        yield return ("tdoc", mov.Tdoc);
        if (mov.DvSpecified) yield return ("dv", mov.Dv);
        if (mov.DptoSpecified) yield return ("dpto", mov.Dpto);
        if (mov.MunSpecified) yield return ("mun", mov.Mun);
        if (mov.PaisSpecified) yield return ("pais", mov.Pais);
        yield return ("tipcta", mov.TipCta);
        yield return ("codex", mov.Codex);
        yield return ("sal", mov.Sal);
        yield return ("psaldof", mov.PSaldoF);
        yield return ("meddia", mov.MedDia);
        yield return ("smax", mov.SMax);
        yield return ("smin", mov.SMin);
        yield return ("vcred", mov.VCred);
        yield return ("movcre", mov.MovCre);
        yield return ("procre", mov.ProCre);
        yield return ("medcre", mov.MedCre);
        yield return ("vmovdeb", mov.VMovDeb);
        yield return ("nmovdeb", mov.NMovDeb);
        yield return ("pordeb", mov.PorDeb);
    }
}
