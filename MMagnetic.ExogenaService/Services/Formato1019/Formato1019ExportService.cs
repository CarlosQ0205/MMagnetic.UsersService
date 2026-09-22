using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019ExportService : IFormato1019ExportService
{
    // Límite de "movcta" por archivo de envío (Anexo 2, Resolución 000162 de 2023).
    private const int TamanoLoteMaximo = 5000;

    private readonly IFormato1019EnsambladorService _ensamblador;
    private readonly IFormato1019Validator _validator;
    private readonly FormatosDbContext _formatos;
    private readonly ClientesDbContext _clientes;

    public Formato1019ExportService(
        IFormato1019EnsambladorService ensamblador,
        IFormato1019Validator validator,
        FormatosDbContext formatos,
        ClientesDbContext clientes)
    {
        _ensamblador = ensamblador;
        _validator = validator;
        _formatos = formatos;
        _clientes = clientes;
    }

    public async Task<ResultadoExportacion> GenerarXmlAsync(
        int periodoAno,
        int numEnvio,
        int codCpt,
        DateTime fecInicial,
        DateTime fecFinal,
        CancellationToken cancellationToken = default)
    {
        var registros = await ObtenerRegistrosDefinitivosAsync(periodoAno, cancellationToken);
        if (registros.Count == 0)
            return new ResultadoExportacion(false, null, null, new[] { ErrorSinRegistros(periodoAno) });

        return ConstruirYSerializarLote(registros, periodoAno, numEnvio, codCpt, fecInicial, fecFinal);
    }

    public async Task<ResultadoLotesExportacion> GenerarLotesXmlAsync(int periodoAno, CancellationToken cancellationToken = default)
    {
        var registros = await ObtenerRegistrosDefinitivosAsync(periodoAno, cancellationToken);
        if (registros.Count == 0)
            return new ResultadoLotesExportacion(false, null, new[] { ErrorSinRegistros(periodoAno) });

        var (fecInicial, fecFinal) = RangoDelAno(periodoAno);
        var archivos = new List<ArchivoLote>();
        var lotes = registros.Chunk(TamanoLoteMaximo).ToList();

        for (var i = 0; i < lotes.Count; i++)
        {
            // CodCpt=1 (Inserción) y las fechas son un valor de partida: el pre-validador de
            // la DIAN es quien asigna el consecutivo/concepto/fechas reales antes del envío.
            var resultado = ConstruirYSerializarLote(lotes[i].ToList(), periodoAno, numEnvio: i + 1, codCpt: 1, fecInicial, fecFinal);
            if (!resultado.Exitoso)
                return new ResultadoLotesExportacion(false, null, resultado.Errores);

            archivos.Add(new ArchivoLote(resultado.NombreArchivo!, resultado.ContenidoXml!));
        }

        return new ResultadoLotesExportacion(true, archivos, Array.Empty<Formato1019ValidationError>());
    }

    public async Task<ResultadoLotesExportacion> GenerarLotesExcelAsync(int periodoAno, CancellationToken cancellationToken = default)
    {
        var registros = await ObtenerRegistrosDefinitivosAsync(periodoAno, cancellationToken);
        if (registros.Count == 0)
            return new ResultadoLotesExportacion(false, null, new[] { ErrorSinRegistros(periodoAno) });

        var clienteIds = registros.Select(r => r.ClienteId).Distinct().ToList();
        var clientesPorId = await _clientes.Clientes
            .Where(c => clienteIds.Contains(c.ClienteId))
            .ToDictionaryAsync(c => c.ClienteId, c => c.NumeroDocumento, cancellationToken);

        var archivos = new List<ArchivoLote>();
        var lotes = registros.Chunk(TamanoLoteMaximo).ToList();
        var ahora = DateTime.UtcNow;

        for (var i = 0; i < lotes.Count; i++)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Datos");
            string[] columnas = { "NumeroDocumentoCliente", "PeriodoAno", "Linea", "CodigoConcepto", "Valor", "FechaGeneracion" };
            for (var c = 0; c < columnas.Length; c++)
                hoja.Cell(1, c + 1).Value = columnas[c];

            var fila = 2;
            foreach (var registro in lotes[i])
            {
                var numeroDocumento = clientesPorId.GetValueOrDefault(registro.ClienteId);
                foreach (var (codigo, valor) in Formato1019ConceptoExtractor.ObtenerConceptos(registro.Movimiento))
                {
                    hoja.Cell(fila, 1).Value = numeroDocumento;
                    hoja.Cell(fila, 2).Value = periodoAno;
                    hoja.Cell(fila, 3).Value = registro.NumeroCuenta;
                    hoja.Cell(fila, 4).Value = codigo;
                    hoja.Cell(fila, 5).Value = valor;
                    hoja.Cell(fila, 6).Value = ahora;
                    fila++;
                }
            }

            using var stream = new MemoryStream();
            libro.SaveAs(stream);
            archivos.Add(new ArchivoLote($"f1019_definitivo_{periodoAno}_lote{i + 1:D4}.xlsx", stream.ToArray()));
        }

        return new ResultadoLotesExportacion(true, archivos, Array.Empty<Formato1019ValidationError>());
    }

    private async Task<List<RegistroFormato1019>> ObtenerRegistrosDefinitivosAsync(int periodoAno, CancellationToken cancellationToken)
    {
        var llavesValidas = await _formatos.Formato1019Definitivo
            .Where(f => f.PeriodoAno == periodoAno && f.ClienteId != null && f.Linea != null)
            .Select(f => new { ClienteId = f.ClienteId!.Value, Linea = f.Linea! })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (llavesValidas.Count == 0)
            return new List<RegistroFormato1019>();

        var llavesSet = llavesValidas.Select(l => (l.ClienteId, l.Linea)).ToHashSet();

        // Se reconstruye el movcta completo (incluidos nid/cta/titSec, que no viven en el
        // modelo EAV de F_1019_Definitivo) volviendo a ensamblar desde el origen, y luego
        // filtrando solo las cuentas que quedaron en Definitivo.
        var registros = await _ensamblador.EnsamblarAsync(periodoAno, cancellationToken);
        return registros.Where(r => llavesSet.Contains((r.ClienteId, r.NumeroCuenta))).ToList();
    }

    private ResultadoExportacion ConstruirYSerializarLote(
        List<RegistroFormato1019> registros, int periodoAno, int numEnvio, int codCpt, DateTime fecInicial, DateTime fecFinal)
    {
        var movimientos = registros.Select(r => r.Movimiento).ToList();
        var carga = new CargaMasiva1019
        {
            Cab = new CabeceraFormato1019
            {
                Ano = periodoAno,
                CodCpt = codCpt,
                NumEnvio = numEnvio,
                FecEnvio = DateTime.UtcNow,
                FecInicial = fecInicial,
                FecFinal = fecFinal,
                ValorTotal = movimientos.Sum(m => (decimal)m.Codex),
                CantReg = movimientos.Count,
            },
            Movimientos = movimientos,
        };

        var resultadoValidacion = _validator.Validar(carga);
        if (!resultadoValidacion.EsValido)
            return new ResultadoExportacion(false, null, null, resultadoValidacion.Errores);

        var nombreArchivo = Formato1019FileNaming.ConstruirNombreArchivo(codCpt, periodoAno, numEnvio);
        return new ResultadoExportacion(true, nombreArchivo, SerializarXml(carga), Array.Empty<Formato1019ValidationError>());
    }

    private static (DateTime FecInicial, DateTime FecFinal) RangoDelAno(int periodoAno)
        => (new DateTime(periodoAno, 1, 1), new DateTime(periodoAno, 12, 31));

    private static Formato1019ValidationError ErrorSinRegistros(int periodoAno)
        => new(
            "EXPORT-VACIO",
            $"No hay registros validados en F_1019_Definitivo para el año {periodoAno}. Corre primero la clasificación de ese período.");

    private static byte[] SerializarXml(CargaMasiva1019 carga)
    {
        // El Anexo 2 exige ISO-8859-1; el default de .NET (UTF-8) declarado en el XML
        // violaría la especificación aunque el contenido fuera correcto.
        var encoding = Encoding.GetEncoding("ISO-8859-1");
        var serializer = new XmlSerializer(typeof(CargaMasiva1019));

        // Sin esto, XmlSerializer agrega xmlns:xsi/xmlns:xsd al elemento raíz, que no
        // aparecen en el ejemplo de la Resolución y no aportan nada al archivo.
        var sinNamespaces = new XmlSerializerNamespaces();
        sinNamespaces.Add(string.Empty, string.Empty);

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = encoding };
        using (var writer = XmlWriter.Create(stream, settings))
        {
            serializer.Serialize(writer, carga, sinNamespaces);
        }

        return stream.ToArray();
    }
}
