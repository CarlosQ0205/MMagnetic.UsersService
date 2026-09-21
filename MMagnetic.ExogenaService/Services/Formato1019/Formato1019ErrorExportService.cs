using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019ErrorExportService : IFormato1019ErrorExportService
{
    // Códigos de Formato1019Validator que corresponden a un campo de Cliente (ver
    // Formato1019EnsambladorService.MapearMovimiento: tdoc/nid/apl*/nom*/raz/dir/dpto/mun/pais
    // se llenan desde Cliente).
    private static readonly HashSet<string> CodigosCliente = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOV-TDOC", "MOV-NID", "MOV-DV", "MOV-NOMBRES", "MOV-RAZ", "MOV-TITULAR", "MOV-DIR",
        "MOV-DPTO", "MOV-MUN", "MOV-PAIS", "HOMOLOGACION",
    };

    // El resto de códigos MOV-* corresponden a un campo de Datos_Financieros (cuenta/saldos/movimientos).
    private static readonly HashSet<string> CodigosDatoFinanciero = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOV-CTA", "MOV-TIPCTA", "MOV-CODEX", "MOV-SAL", "MOV-PSALDOF", "MOV-MEDDIA", "MOV-SMAX",
        "MOV-SMIN", "MOV-VCRED", "MOV-MOVCRE", "MOV-PROCRE", "MOV-MEDCRE", "MOV-VMOVDEB",
        "MOV-NMOVDEB", "MOV-PORDEB",
    };

    private readonly IFormato1019EnsambladorService _ensamblador;
    private readonly IFormato1019Validator _validator;
    private readonly IHomologadorDianService _homologador;
    private readonly ClientesDbContext _clientes;

    public Formato1019ErrorExportService(
        IFormato1019EnsambladorService ensamblador,
        IFormato1019Validator validator,
        IHomologadorDianService homologador,
        ClientesDbContext clientes)
    {
        _ensamblador = ensamblador;
        _validator = validator;
        _homologador = homologador;
        _clientes = clientes;
    }

    public async Task<byte[]> ExportarErroresAsync(int periodoAno, CancellationToken cancellationToken = default)
    {
        var registros = await _ensamblador.EnsamblarAsync(periodoAno, cancellationToken);

        var clientesConError = new Dictionary<Guid, SortedSet<string>>();
        var cuentasConError = new List<(Guid ClienteId, Guid DatoFinancieroId, SortedSet<string> Errores)>();

        foreach (var registro in registros)
        {
            var resultadoValidacion = _validator.ValidarRegistro(registro.Movimiento);
            var todosLosErrores = registro.ErroresHomologacion
                .Select(mensaje => new Formato1019ValidationError("HOMOLOGACION", mensaje))
                .Concat(resultadoValidacion.Errores)
                .ToList();

            if (todosLosErrores.Count == 0)
                continue;

            var erroresCliente = todosLosErrores.Where(e => CodigosCliente.Contains(e.Codigo)).Select(e => e.Mensaje).ToList();
            var erroresCuenta = todosLosErrores.Where(e => CodigosDatoFinanciero.Contains(e.Codigo)).Select(e => e.Mensaje).ToList();

            if (erroresCliente.Count > 0)
            {
                if (!clientesConError.TryGetValue(registro.ClienteId, out var conjunto))
                    clientesConError[registro.ClienteId] = conjunto = new SortedSet<string>();

                foreach (var mensaje in erroresCliente)
                    conjunto.Add(mensaje);
            }

            if (erroresCuenta.Count > 0)
                cuentasConError.Add((registro.ClienteId, registro.DatoFinancieroId, new SortedSet<string>(erroresCuenta)));
        }

        using var libro = new XLWorkbook();
        await EscribirHojaClientesAsync(libro, clientesConError, cancellationToken);
        await EscribirHojaDatosFinancierosAsync(libro, cuentasConError, cancellationToken);

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task EscribirHojaClientesAsync(
        XLWorkbook libro, Dictionary<Guid, SortedSet<string>> clientesConError, CancellationToken cancellationToken)
    {
        var hoja = libro.Worksheets.Add("Clientes");
        string[] columnas =
        {
            "TipoDocumento", "NumeroDocumento", "RazonSocial", "PrimerNombre", "SegundoNombre",
            "PrimerApellido", "SegundoApellido", "FechaNacimiento", "Direccion", "Telefono",
            "CorreoElectronico", "CodigoPais", "CodigoDepartamento", "CodigoMunicipio",
            "DigitoVerificacion", "Errores",
        };
        for (var i = 0; i < columnas.Length; i++)
            hoja.Cell(1, i + 1).Value = columnas[i];

        var fila = 2;
        foreach (var (clienteId, errores) in clientesConError)
        {
            var cliente = await _clientes.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId, cancellationToken);
            if (cliente is null)
                continue;

            // El código DIAN se reconstruye a partir del Id interno del cliente (inverso del
            // Resolutor), con ceros a la izquierda para que al volver a subirlo siga
            // emparejando contra el catálogo (ej. "05", no "5").
            var homologacion = await _homologador.HomologarAsync(cliente.PaisId, cliente.DepartamentoId, cliente.MunicipioId, cancellationToken);

            var columna = 1;
            SetCell(hoja, fila, columna++, cliente.TipoDocumento);
            SetCell(hoja, fila, columna++, cliente.NumeroDocumento);
            SetCell(hoja, fila, columna++, cliente.RazonSocial);
            SetCell(hoja, fila, columna++, cliente.PrimerNombre);
            SetCell(hoja, fila, columna++, cliente.SegundoNombre);
            SetCell(hoja, fila, columna++, cliente.PrimerApellido);
            SetCell(hoja, fila, columna++, cliente.SegundoApellido);
            SetCell(hoja, fila, columna++, cliente.FechaNacimiento);
            SetCell(hoja, fila, columna++, cliente.Direccion);
            SetCell(hoja, fila, columna++, cliente.Telefono);
            SetCell(hoja, fila, columna++, cliente.CorreoElectronico);
            SetCell(hoja, fila, columna++, homologacion.CodigoPais?.ToString("D3"));
            SetCell(hoja, fila, columna++, homologacion.CodigoDepartamento?.ToString("D2"));
            SetCell(hoja, fila, columna++, homologacion.CodigoMunicipio?.ToString("D3"));
            SetCell(hoja, fila, columna++, cliente.DigitoVerificacion);
            SetCell(hoja, fila, columna, string.Join(" | ", errores));

            fila++;
        }
    }

    private async Task EscribirHojaDatosFinancierosAsync(
        XLWorkbook libro,
        List<(Guid ClienteId, Guid DatoFinancieroId, SortedSet<string> Errores)> cuentasConError,
        CancellationToken cancellationToken)
    {
        var hoja = libro.Worksheets.Add("DatosFinancieros");
        string[] columnas =
        {
            "NumeroDocumentoCliente", "TipoProducto", "Entidad", "Observaciones", "PeriodoAno",
            "NumeroCuenta", "TipoCuenta", "CodigoExencion", "Saldo", "IngresosAnuales",
            "EgresosAnuales", "Activos", "Pasivos", "Patrimonio", "PromedioSaldoFinal",
            "MedianaSaldoDiario", "SaldoMaximo", "SaldoMinimo", "ValorMovCredito", "NumMovCredito",
            "PromedioMovCredito", "MedianaMovCredito", "ValorMovDebito", "NumMovDebito",
            "PromedioMovDebito", "Errores",
        };
        for (var i = 0; i < columnas.Length; i++)
            hoja.Cell(1, i + 1).Value = columnas[i];

        var fila = 2;
        foreach (var (clienteId, datoFinancieroId, errores) in cuentasConError)
        {
            var datoFinanciero = await _clientes.DatosFinancieros.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DatoFinancieroId == datoFinancieroId, cancellationToken);
            var cliente = await _clientes.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId, cancellationToken);
            if (datoFinanciero is null || cliente is null)
                continue;

            var columna = 1;
            SetCell(hoja, fila, columna++, cliente.NumeroDocumento);
            SetCell(hoja, fila, columna++, datoFinanciero.TipoProducto);
            SetCell(hoja, fila, columna++, datoFinanciero.Entidad);
            SetCell(hoja, fila, columna++, datoFinanciero.Observaciones);
            SetCell(hoja, fila, columna++, datoFinanciero.PeriodoAno);
            SetCell(hoja, fila, columna++, datoFinanciero.NumeroCuenta);
            SetCell(hoja, fila, columna++, datoFinanciero.TipoCuenta);
            SetCell(hoja, fila, columna++, datoFinanciero.CodigoExencion);
            SetCell(hoja, fila, columna++, datoFinanciero.Saldo);
            SetCell(hoja, fila, columna++, datoFinanciero.IngresosAnuales);
            SetCell(hoja, fila, columna++, datoFinanciero.EgresosAnuales);
            SetCell(hoja, fila, columna++, datoFinanciero.Activos);
            SetCell(hoja, fila, columna++, datoFinanciero.Pasivos);
            SetCell(hoja, fila, columna++, datoFinanciero.Patrimonio);
            SetCell(hoja, fila, columna++, datoFinanciero.PromedioSaldoFinal);
            SetCell(hoja, fila, columna++, datoFinanciero.MedianaSaldoDiario);
            SetCell(hoja, fila, columna++, datoFinanciero.SaldoMaximo);
            SetCell(hoja, fila, columna++, datoFinanciero.SaldoMinimo);
            SetCell(hoja, fila, columna++, datoFinanciero.ValorMovCredito);
            SetCell(hoja, fila, columna++, datoFinanciero.NumMovCredito);
            SetCell(hoja, fila, columna++, datoFinanciero.PromedioMovCredito);
            SetCell(hoja, fila, columna++, datoFinanciero.MedianaMovCredito);
            SetCell(hoja, fila, columna++, datoFinanciero.ValorMovDebito);
            SetCell(hoja, fila, columna++, datoFinanciero.NumMovDebito);
            SetCell(hoja, fila, columna++, datoFinanciero.PromedioMovDebito);
            SetCell(hoja, fila, columna, string.Join(" | ", errores));

            fila++;
        }
    }

    private static void SetCell(IXLWorksheet hoja, int fila, int columna, object? valor)
    {
        if (valor is null)
            return;

        var celda = hoja.Cell(fila, columna);
        switch (valor)
        {
            case string texto: celda.Value = texto; break;
            case DateTime fecha: celda.Value = fecha; break;
            case decimal numero: celda.Value = numero; break;
            case int entero: celda.Value = entero; break;
            case byte pequeno: celda.Value = pequeno; break;
            default: celda.Value = valor.ToString(); break;
        }
    }
}
