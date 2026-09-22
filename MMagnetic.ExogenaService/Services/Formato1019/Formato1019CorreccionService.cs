using System.Globalization;
using Microsoft.AspNetCore.Http;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019CorreccionService : IFormato1019CorreccionService
{
    private readonly IArchivoTabularReader _lector;
    private readonly IClienteService _clientes;
    private readonly IDatoFinancieroService _datosFinancieros;

    public Formato1019CorreccionService(
        IArchivoTabularReader lector, IClienteService clientes, IDatoFinancieroService datosFinancieros)
    {
        _lector = lector;
        _clientes = clientes;
        _datosFinancieros = datosFinancieros;
    }

    public async Task<ResultadoCargaMasiva> CargarCorregidosAsync(IFormFile archivo, CancellationToken cancellationToken = default)
    {
        var filas = await _lector.LeerAsync(archivo, cancellationToken);
        var errores = new List<FilaErrorCarga>();
        var exitosas = 0;

        for (var i = 0; i < filas.Count; i++)
        {
            var numeroFila = i + 2; // la fila 1 del archivo son los encabezados
            var fila = filas[i];
            var erroresFila = new List<string>();

            var clienteDto = new ClienteDto
            {
                TipoDocumento = ObtenerValor(fila, "TipoDocumento"),
                NumeroDocumento = ObtenerValor(fila, "NumeroDocumento") ?? string.Empty,
                RazonSocial = ObtenerValor(fila, "RazonSocial"),
                PrimerNombre = ObtenerValor(fila, "PrimerNombre"),
                SegundoNombre = ObtenerValor(fila, "SegundoNombre"),
                PrimerApellido = ObtenerValor(fila, "PrimerApellido"),
                SegundoApellido = ObtenerValor(fila, "SegundoApellido"),
                Direccion = ObtenerValor(fila, "Direccion"),
                Telefono = ObtenerValor(fila, "Telefono"),
                CorreoElectronico = ObtenerValor(fila, "CorreoElectronico"),
                CodigoPais = ObtenerValor(fila, "CodigoPais"),
                CodigoDepartamento = ObtenerValor(fila, "CodigoDepartamento"),
                CodigoMunicipio = ObtenerValor(fila, "CodigoMunicipio"),
            };

            if (DateTime.TryParse(ObtenerValor(fila, "FechaNacimiento"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaNacimiento))
                clienteDto.FechaNacimiento = fechaNacimiento;

            if (byte.TryParse(ObtenerValor(fila, "DigitoVerificacion"), out var dv))
                clienteDto.DigitoVerificacion = dv;

            var resultadoCliente = await _clientes.CrearOActualizarPorDocumentoAsync(clienteDto, cancellationToken);
            if (!resultadoCliente.Exitoso)
                erroresFila.AddRange(resultadoCliente.Errores.Select(e => $"Cliente: {e}"));

            var datoFinancieroDto = new DatoFinancieroDto
            {
                NumeroDocumentoCliente = clienteDto.NumeroDocumento,
                TipoProducto = ObtenerValor(fila, "TipoProducto"),
                Entidad = ObtenerValor(fila, "Entidad"),
                Observaciones = ObtenerValor(fila, "Observaciones"),
                NumeroCuenta = ObtenerValor(fila, "NumeroCuenta"),
                PeriodoAno = ObtenerInt(fila, "PeriodoAno"),
                TipoCuenta = ObtenerByte(fila, "TipoCuenta"),
                CodigoExencion = ObtenerByte(fila, "CodigoExencion"),
                Saldo = ObtenerDecimal(fila, "Saldo"),
                IngresosAnuales = ObtenerDecimal(fila, "IngresosAnuales"),
                EgresosAnuales = ObtenerDecimal(fila, "EgresosAnuales"),
                Activos = ObtenerDecimal(fila, "Activos"),
                Pasivos = ObtenerDecimal(fila, "Pasivos"),
                Patrimonio = ObtenerDecimal(fila, "Patrimonio"),
                PromedioSaldoFinal = ObtenerDecimal(fila, "PromedioSaldoFinal"),
                MedianaSaldoDiario = ObtenerDecimal(fila, "MedianaSaldoDiario"),
                SaldoMaximo = ObtenerDecimal(fila, "SaldoMaximo"),
                SaldoMinimo = ObtenerDecimal(fila, "SaldoMinimo"),
                ValorMovCredito = ObtenerDecimal(fila, "ValorMovCredito"),
                NumMovCredito = ObtenerInt(fila, "NumMovCredito"),
                PromedioMovCredito = ObtenerDecimal(fila, "PromedioMovCredito"),
                MedianaMovCredito = ObtenerDecimal(fila, "MedianaMovCredito"),
                ValorMovDebito = ObtenerDecimal(fila, "ValorMovDebito"),
                NumMovDebito = ObtenerInt(fila, "NumMovDebito"),
                PromedioMovDebito = ObtenerDecimal(fila, "PromedioMovDebito"),
            };

            if (resultadoCliente.Exitoso)
            {
                var resultadoDatoFinanciero = await _datosFinancieros.CrearOActualizarAsync(datoFinancieroDto, cancellationToken);
                if (!resultadoDatoFinanciero.Exitoso)
                    erroresFila.AddRange(resultadoDatoFinanciero.Errores.Select(e => $"Datos financieros: {e}"));
            }

            if (erroresFila.Count == 0)
                exitosas++;
            else
                errores.Add(new FilaErrorCarga(numeroFila, string.Join("; ", erroresFila)));
        }

        return new ResultadoCargaMasiva { TotalFilas = filas.Count, Exitosas = exitosas, Errores = errores };
    }

    private static string? ObtenerValor(IReadOnlyDictionary<string, string> fila, string columna)
        => fila.TryGetValue(columna, out var valor) && !string.IsNullOrWhiteSpace(valor) ? valor.Trim() : null;

    private static decimal? ObtenerDecimal(IReadOnlyDictionary<string, string> fila, string columna)
        => decimal.TryParse(ObtenerValor(fila, columna), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) ? valor : null;

    private static int? ObtenerInt(IReadOnlyDictionary<string, string> fila, string columna)
        => int.TryParse(ObtenerValor(fila, columna), out var valor) ? valor : null;

    private static byte? ObtenerByte(IReadOnlyDictionary<string, string> fila, string columna)
        => byte.TryParse(ObtenerValor(fila, columna), out var valor) ? valor : null;
}
