using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;

namespace MMagnetic.ExogenaService.Services.Clientes;

public class CotitularService : ICotitularService
{
    private readonly ClientesDbContext _clientes;
    private readonly IArchivoTabularReader _lector;

    public CotitularService(ClientesDbContext clientes, IArchivoTabularReader lector)
    {
        _clientes = clientes;
        _lector = lector;
    }

    public async Task<IReadOnlyList<Cotitular>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
        => await _clientes.Cotitulares.AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync(cancellationToken);

    public async Task<ResultadoOperacion<Cotitular>> CrearAsync(CotitularDto dto, CancellationToken cancellationToken = default)
    {
        var (errores, clienteId) = await ValidarAsync(dto, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<Cotitular>.Fallo(errores);

        var cotitular = new Cotitular
        {
            CotitularId = Guid.NewGuid(),
            ClienteId = clienteId!.Value,
            FechaRegistro = DateTime.UtcNow,
        };
        MapearDtoAEntidad(dto, cotitular);

        _clientes.Cotitulares.Add(cotitular);
        await _clientes.SaveChangesAsync(cancellationToken);

        return ResultadoOperacion<Cotitular>.Ok(cotitular);
    }

    public async Task<ResultadoOperacion<Cotitular>> ActualizarAsync(Guid cotitularId, CotitularDto dto, CancellationToken cancellationToken = default)
    {
        var cotitular = await _clientes.Cotitulares.FirstOrDefaultAsync(c => c.CotitularId == cotitularId, cancellationToken);
        if (cotitular is null)
            return ResultadoOperacion<Cotitular>.Fallo("El cotitular no existe.");

        var (errores, clienteId) = await ValidarAsync(dto, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<Cotitular>.Fallo(errores);

        cotitular.ClienteId = clienteId!.Value;
        MapearDtoAEntidad(dto, cotitular);
        await _clientes.SaveChangesAsync(cancellationToken);

        return ResultadoOperacion<Cotitular>.Ok(cotitular);
    }

    public async Task<bool> EliminarAsync(Guid cotitularId, CancellationToken cancellationToken = default)
    {
        var cotitular = await _clientes.Cotitulares.FirstOrDefaultAsync(c => c.CotitularId == cotitularId, cancellationToken);
        if (cotitular is null)
            return false;

        _clientes.Cotitulares.Remove(cotitular);
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

            var dto = new CotitularDto
            {
                NumeroDocumentoCliente = ObtenerValor(fila, "NumeroDocumentoCliente") ?? string.Empty,
                TipoDocumento = ObtenerValor(fila, "TipoDocumento"),
                NumeroDocumento = ObtenerValor(fila, "NumeroDocumento") ?? string.Empty,
                NombreCompleto = ObtenerValor(fila, "NombreCompleto") ?? string.Empty,
                RelacionConCliente = ObtenerValor(fila, "RelacionConCliente"),
            };

            if (decimal.TryParse(ObtenerValor(fila, "Participacion"), out var participacion))
                dto.Participacion = participacion;

            var resultadoFila = await CrearAsync(dto, cancellationToken);
            if (resultadoFila.Exitoso)
                exitosas++;
            else
                errores.Add(new FilaErrorCarga(numeroFila, string.Join("; ", resultadoFila.Errores)));
        }

        return new ResultadoCargaMasiva { TotalFilas = filas.Count, Exitosas = exitosas, Errores = errores };
    }

    private async Task<(List<string> Errores, Guid? ClienteId)> ValidarAsync(CotitularDto dto, CancellationToken cancellationToken)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.NumeroDocumentoCliente))
            errores.Add("NumeroDocumentoCliente es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.NumeroDocumento))
            errores.Add("NumeroDocumento es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            errores.Add("NombreCompleto es obligatorio.");

        if (dto.Participacion is < 0 or > 100)
            errores.Add("Participacion debe estar entre 0 y 100.");

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

    private static void MapearDtoAEntidad(CotitularDto dto, Cotitular cotitular)
    {
        cotitular.TipoDocumento = dto.TipoDocumento;
        cotitular.NumeroDocumento = dto.NumeroDocumento;
        cotitular.NombreCompleto = dto.NombreCompleto;
        cotitular.Participacion = dto.Participacion;
        cotitular.RelacionConCliente = dto.RelacionConCliente;
    }

    private static string? ObtenerValor(IReadOnlyDictionary<string, string> fila, string columna)
        => fila.TryGetValue(columna, out var valor) && !string.IsNullOrWhiteSpace(valor) ? valor.Trim() : null;
}
