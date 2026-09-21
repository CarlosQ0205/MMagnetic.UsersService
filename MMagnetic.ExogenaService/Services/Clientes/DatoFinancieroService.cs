using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;

namespace MMagnetic.ExogenaService.Services.Clientes;

public class DatoFinancieroService : IDatoFinancieroService
{
    private readonly ClientesDbContext _clientes;
    private readonly IArchivoTabularReader _lector;

    public DatoFinancieroService(ClientesDbContext clientes, IArchivoTabularReader lector)
    {
        _clientes = clientes;
        _lector = lector;
    }

    public async Task<IReadOnlyList<DatoFinanciero>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
        => await _clientes.DatosFinancieros.AsNoTracking()
            .Where(d => d.ClienteId == clienteId)
            .OrderByDescending(d => d.PeriodoAno)
            .ToListAsync(cancellationToken);

    public async Task<ResultadoOperacion<DatoFinanciero>> CrearAsync(DatoFinancieroDto dto, CancellationToken cancellationToken = default)
    {
        var (errores, clienteId) = await ValidarAsync(dto, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<DatoFinanciero>.Fallo(errores);

        var datoFinanciero = new DatoFinanciero
        {
            DatoFinancieroId = Guid.NewGuid(),
            ClienteId = clienteId!.Value,
            FechaRegistro = DateTime.UtcNow,
        };
        MapearDtoAEntidad(dto, datoFinanciero);

        _clientes.DatosFinancieros.Add(datoFinanciero);
        await _clientes.SaveChangesAsync(cancellationToken);

        return ResultadoOperacion<DatoFinanciero>.Ok(datoFinanciero);
    }

    public async Task<ResultadoOperacion<DatoFinanciero>> ActualizarAsync(Guid datoFinancieroId, DatoFinancieroDto dto, CancellationToken cancellationToken = default)
    {
        var datoFinanciero = await _clientes.DatosFinancieros.FirstOrDefaultAsync(d => d.DatoFinancieroId == datoFinancieroId, cancellationToken);
        if (datoFinanciero is null)
            return ResultadoOperacion<DatoFinanciero>.Fallo("El registro financiero no existe.");

        var (errores, clienteId) = await ValidarAsync(dto, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<DatoFinanciero>.Fallo(errores);

        datoFinanciero.ClienteId = clienteId!.Value;
        MapearDtoAEntidad(dto, datoFinanciero);
        await _clientes.SaveChangesAsync(cancellationToken);

        return ResultadoOperacion<DatoFinanciero>.Ok(datoFinanciero);
    }

    public async Task<bool> EliminarAsync(Guid datoFinancieroId, CancellationToken cancellationToken = default)
    {
        var datoFinanciero = await _clientes.DatosFinancieros.FirstOrDefaultAsync(d => d.DatoFinancieroId == datoFinancieroId, cancellationToken);
        if (datoFinanciero is null)
            return false;

        _clientes.DatosFinancieros.Remove(datoFinanciero);
        await _clientes.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ResultadoCargaMasiva> CargarMasivoAsync(IFormFile archivo, CancellationToken cancellationToken = default)
    {
        var filas = await _lector.LeerAsync(archivo, cancellationToken);
        var errores = new List<FilaErrorCarga>();
        var exitosas = 0;

        for (var i = 0; i < filas.Count; i++)
        {
            var numeroFila = i + 2;
            var fila = filas[i];

            var dto = new DatoFinancieroDto
            {
                NumeroDocumentoCliente = ObtenerValor(fila, "NumeroDocumentoCliente") ?? string.Empty,
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

            var resultadoFila = await CrearOActualizarPorLoteAsync(dto, cancellationToken);
            if (resultadoFila.Exitoso)
                exitosas++;
            else
                errores.Add(new FilaErrorCarga(numeroFila, string.Join("; ", resultadoFila.Errores)));
        }

        return new ResultadoCargaMasiva { TotalFilas = filas.Count, Exitosas = exitosas, Errores = errores };
    }

    private async Task<ResultadoOperacion<DatoFinanciero>> CrearOActualizarPorLoteAsync(DatoFinancieroDto dto, CancellationToken cancellationToken)
    {
        var (errores, clienteId) = await ValidarAsync(dto, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<DatoFinanciero>.Fallo(errores);

        DatoFinanciero? existente = null;
        if (!string.IsNullOrWhiteSpace(dto.NumeroCuenta) && dto.PeriodoAno.HasValue)
        {
            existente = await _clientes.DatosFinancieros.FirstOrDefaultAsync(
                d => d.ClienteId == clienteId!.Value && d.NumeroCuenta == dto.NumeroCuenta && d.PeriodoAno == dto.PeriodoAno,
                cancellationToken);
        }

        var datoFinanciero = existente ?? new DatoFinanciero
        {
            DatoFinancieroId = Guid.NewGuid(),
            ClienteId = clienteId!.Value,
            FechaRegistro = DateTime.UtcNow,
        };

        MapearDtoAEntidad(dto, datoFinanciero);

        if (existente is null)
            _clientes.DatosFinancieros.Add(datoFinanciero);

        await _clientes.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<DatoFinanciero>.Ok(datoFinanciero);
    }

    private async Task<(List<string> Errores, Guid? ClienteId)> ValidarAsync(DatoFinancieroDto dto, CancellationToken cancellationToken)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.NumeroDocumentoCliente))
            errores.Add("NumeroDocumentoCliente es obligatorio.");

        Guid? clienteId = null;
        if (!string.IsNullOrWhiteSpace(dto.NumeroDocumentoCliente))
        {
            clienteId = await _clientes.Clientes.AsNoTracking()
                .Where(c => c.NumeroDocumento == dto.NumeroDocumentoCliente)
                .Select(c => (Guid?)c.ClienteId)
                .FirstOrDefaultAsync(cancellationToken);

            if (clienteId is null)
                errores.Add($"No existe un cliente con NumeroDocumento '{dto.NumeroDocumentoCliente}'. Cárguelo primero.");
        }

        return (errores, clienteId);
    }

    private static void MapearDtoAEntidad(DatoFinancieroDto dto, DatoFinanciero datoFinanciero)
    {
        datoFinanciero.TipoProducto = dto.TipoProducto;
        datoFinanciero.Entidad = dto.Entidad;
        datoFinanciero.Observaciones = dto.Observaciones;
        datoFinanciero.Saldo = dto.Saldo;
        datoFinanciero.IngresosAnuales = dto.IngresosAnuales;
        datoFinanciero.EgresosAnuales = dto.EgresosAnuales;
        datoFinanciero.Activos = dto.Activos;
        datoFinanciero.Pasivos = dto.Pasivos;
        datoFinanciero.Patrimonio = dto.Patrimonio;
        datoFinanciero.PeriodoAno = dto.PeriodoAno;
        datoFinanciero.NumeroCuenta = dto.NumeroCuenta;
        datoFinanciero.TipoCuenta = dto.TipoCuenta;
        datoFinanciero.CodigoExencion = dto.CodigoExencion;
        datoFinanciero.PromedioSaldoFinal = dto.PromedioSaldoFinal;
        datoFinanciero.MedianaSaldoDiario = dto.MedianaSaldoDiario;
        datoFinanciero.SaldoMaximo = dto.SaldoMaximo;
        datoFinanciero.SaldoMinimo = dto.SaldoMinimo;
        datoFinanciero.ValorMovCredito = dto.ValorMovCredito;
        datoFinanciero.NumMovCredito = dto.NumMovCredito;
        datoFinanciero.PromedioMovCredito = dto.PromedioMovCredito;
        datoFinanciero.MedianaMovCredito = dto.MedianaMovCredito;
        datoFinanciero.ValorMovDebito = dto.ValorMovDebito;
        datoFinanciero.NumMovDebito = dto.NumMovDebito;
        datoFinanciero.PromedioMovDebito = dto.PromedioMovDebito;
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
