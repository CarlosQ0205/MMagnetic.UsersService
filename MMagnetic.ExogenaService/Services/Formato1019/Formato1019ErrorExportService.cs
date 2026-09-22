using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019ErrorExportService : IFormato1019ErrorExportService
{
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

    /// <summary>
    /// Un Excel con una fila por cada "línea" (cuenta) del período que no pasó la validación,
    /// con TODOS los campos del Formato 1019 (los que vienen de Clientes y los que vienen de
    /// Datos_Financieros) más una columna "Errores". El mismo archivo, ya corregido, se vuelve
    /// a subir por completo con el endpoint de corregidos (no hace falta separar por tabla).
    /// </summary>
    public async Task<byte[]> ExportarErroresAsync(int periodoAno, CancellationToken cancellationToken = default)
    {
        var registros = await _ensamblador.EnsamblarAsync(periodoAno, cancellationToken);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Errores_F1019");
        string[] columnas =
        {
            "NumeroDocumento", "TipoDocumento", "RazonSocial", "PrimerNombre", "SegundoNombre",
            "PrimerApellido", "SegundoApellido", "FechaNacimiento", "Direccion", "Telefono",
            "CorreoElectronico", "CodigoPais", "CodigoDepartamento", "CodigoMunicipio", "DigitoVerificacion",
            "TipoProducto", "Entidad", "Observaciones", "PeriodoAno", "NumeroCuenta", "TipoCuenta",
            "CodigoExencion", "Saldo", "IngresosAnuales", "EgresosAnuales", "Activos", "Pasivos",
            "Patrimonio", "PromedioSaldoFinal", "MedianaSaldoDiario", "SaldoMaximo", "SaldoMinimo",
            "ValorMovCredito", "NumMovCredito", "PromedioMovCredito", "MedianaMovCredito",
            "ValorMovDebito", "NumMovDebito", "PromedioMovDebito", "Errores",
        };
        for (var i = 0; i < columnas.Length; i++)
            hoja.Cell(1, i + 1).Value = columnas[i];

        var fila = 2;
        foreach (var registro in registros)
        {
            var resultadoValidacion = _validator.ValidarRegistro(registro.Movimiento);
            var errores = registro.ErroresHomologacion
                .Concat(resultadoValidacion.Errores.Select(e => e.Mensaje))
                .Distinct()
                .ToList();

            if (errores.Count == 0)
                continue;

            var cliente = await _clientes.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == registro.ClienteId, cancellationToken);
            var datoFinanciero = await _clientes.DatosFinancieros.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DatoFinancieroId == registro.DatoFinancieroId, cancellationToken);
            if (cliente is null || datoFinanciero is null)
                continue;

            // El código DIAN se reconstruye a partir del Id interno del cliente (inverso del
            // Resolutor), con ceros a la izquierda para que al volver a subirlo siga
            // emparejando contra el catálogo (ej. "05", no "5").
            var homologacion = await _homologador.HomologarAsync(cliente.PaisId, cliente.DepartamentoId, cliente.MunicipioId, cancellationToken);

            var columna = 1;
            SetCell(hoja, fila, columna++, cliente.NumeroDocumento);
            SetCell(hoja, fila, columna++, cliente.TipoDocumento);
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

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
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
