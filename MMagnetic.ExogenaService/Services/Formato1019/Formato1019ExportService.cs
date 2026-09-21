using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019ExportService : IFormato1019ExportService
{
    private readonly IFormato1019EnsambladorService _ensamblador;
    private readonly IFormato1019Validator _validator;
    private readonly FormatosDbContext _formatos;

    public Formato1019ExportService(
        IFormato1019EnsambladorService ensamblador,
        IFormato1019Validator validator,
        FormatosDbContext formatos)
    {
        _ensamblador = ensamblador;
        _validator = validator;
        _formatos = formatos;
    }

    public async Task<ResultadoExportacion> GenerarXmlAsync(
        int periodoAno,
        int numEnvio,
        int codCpt,
        DateTime fecInicial,
        DateTime fecFinal,
        CancellationToken cancellationToken = default)
    {
        var llavesValidas = await _formatos.Formato1019Definitivo
            .Where(f => f.PeriodoAno == periodoAno && f.ClienteId != null && f.Linea != null)
            .Select(f => new { ClienteId = f.ClienteId!.Value, Linea = f.Linea! })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (llavesValidas.Count == 0)
        {
            return new ResultadoExportacion(false, null, null, new[]
            {
                new Formato1019ValidationError(
                    "EXPORT-VACIO",
                    $"No hay registros validados en F_1019_Definitivo para el año {periodoAno}. Corre primero la clasificación de ese período.")
            });
        }

        var llavesSet = llavesValidas.Select(l => (l.ClienteId, l.Linea)).ToHashSet();

        // Se reconstruye el movcta completo (incluidos nid/cta/titSec, que no viven en el
        // modelo EAV de F_1019_Definitivo) volviendo a ensamblar desde el origen, y luego
        // filtrando solo las cuentas que quedaron en Definitivo.
        var registros = await _ensamblador.EnsamblarAsync(periodoAno, cancellationToken);
        var movimientos = registros
            .Where(r => llavesSet.Contains((r.ClienteId, r.NumeroCuenta)))
            .Select(r => r.Movimiento)
            .ToList();

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
        var contenidoXml = SerializarXml(carga);

        return new ResultadoExportacion(true, nombreArchivo, contenidoXml, Array.Empty<Formato1019ValidationError>());
    }

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
