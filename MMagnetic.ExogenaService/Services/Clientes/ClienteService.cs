using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.Services.Clientes;

public class ClienteService : IClienteService
{
    private readonly ClientesDbContext _clientes;
    private readonly IResolutorCatalogoDianService _resolutor;
    private readonly IArchivoTabularReader _lector;

    public ClienteService(ClientesDbContext clientes, IResolutorCatalogoDianService resolutor, IArchivoTabularReader lector)
    {
        _clientes = clientes;
        _resolutor = resolutor;
        _lector = lector;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken = default)
        => await _clientes.Clientes.AsNoTracking()
            .Where(c => c.EsActivo)
            .OrderBy(c => c.NumeroDocumento)
            .ToListAsync(cancellationToken);

    public async Task<Cliente?> ObtenerAsync(Guid clienteId, CancellationToken cancellationToken = default)
        => await _clientes.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId, cancellationToken);

    public async Task<ResultadoOperacion<Cliente>> CrearAsync(ClienteDto dto, CancellationToken cancellationToken = default)
    {
        var errores = await ValidarAsync(dto, clienteIdActual: null, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<Cliente>.Fallo(errores);

        var homologacion = await _resolutor.ResolverAsync(dto.CodigoPais, dto.CodigoDepartamento, dto.CodigoMunicipio, cancellationToken);
        if (!homologacion.EsValido)
            return ResultadoOperacion<Cliente>.Fallo(homologacion.Errores);

        var cliente = new Cliente
        {
            ClienteId = Guid.NewGuid(),
            FechaRegistro = DateTime.UtcNow,
            EsActivo = true,
        };
        MapearDtoAEntidad(dto, cliente, homologacion);

        _clientes.Clientes.Add(cliente);
        await _clientes.SaveChangesAsync(cancellationToken);

        return ResultadoOperacion<Cliente>.Ok(cliente);
    }

    public async Task<ResultadoOperacion<Cliente>> ActualizarAsync(Guid clienteId, ClienteDto dto, CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId, cancellationToken);
        if (cliente is null)
            return ResultadoOperacion<Cliente>.Fallo("El cliente no existe.");

        var errores = await ValidarAsync(dto, clienteIdActual: clienteId, cancellationToken);
        if (errores.Count > 0)
            return ResultadoOperacion<Cliente>.Fallo(errores);

        var homologacion = await _resolutor.ResolverAsync(dto.CodigoPais, dto.CodigoDepartamento, dto.CodigoMunicipio, cancellationToken);
        if (!homologacion.EsValido)
            return ResultadoOperacion<Cliente>.Fallo(homologacion.Errores);

        MapearDtoAEntidad(dto, cliente, homologacion);
        await _clientes.SaveChangesAsync(cancellationToken);

        return ResultadoOperacion<Cliente>.Ok(cliente);
    }

    public async Task<bool> DesactivarAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId, cancellationToken);
        if (cliente is null)
            return false;

        cliente.EsActivo = false;
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
            var numeroFila = i + 2; // la fila 1 del archivo son los encabezados
            var fila = filas[i];

            var dto = new ClienteDto
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
                dto.FechaNacimiento = fechaNacimiento;

            if (byte.TryParse(ObtenerValor(fila, "DigitoVerificacion"), out var dv))
                dto.DigitoVerificacion = dv;

            var resultadoFila = await CrearAsync(dto, cancellationToken);
            if (resultadoFila.Exitoso)
                exitosas++;
            else
                errores.Add(new FilaErrorCarga(numeroFila, string.Join("; ", resultadoFila.Errores)));
        }

        return new ResultadoCargaMasiva { TotalFilas = filas.Count, Exitosas = exitosas, Errores = errores };
    }

    private async Task<List<string>> ValidarAsync(ClienteDto dto, Guid? clienteIdActual, CancellationToken cancellationToken)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.NumeroDocumento))
            errores.Add("NumeroDocumento es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.TipoDocumento))
            errores.Add("TipoDocumento es obligatorio.");
        else if (CatalogoTiposDocumento.Mapear(dto.TipoDocumento) is null)
            errores.Add($"TipoDocumento '{dto.TipoDocumento}' no está en el catálogo de tipos de documento conocidos.");

        // Misma regla que exige Formato1019Validator al clasificar (MOV-TITULAR), aplicada
        // ya desde el alta para no dejar crear un cliente "vacío" sin que nada lo marque.
        var esPersonaJuridica = !string.IsNullOrWhiteSpace(dto.RazonSocial);
        var esPersonaNatural = !string.IsNullOrWhiteSpace(dto.PrimerNombre) || !string.IsNullOrWhiteSpace(dto.PrimerApellido);
        if (!esPersonaJuridica && !esPersonaNatural)
            errores.Add("Debe diligenciar 'RazonSocial' (persona jurídica) o 'PrimerNombre'/'PrimerApellido' (persona natural).");

        if (!string.IsNullOrWhiteSpace(dto.NumeroDocumento) && !string.IsNullOrWhiteSpace(dto.TipoDocumento))
        {
            var existente = await _clientes.Clientes.AsNoTracking()
                .Where(c => c.TipoDocumento == dto.TipoDocumento && c.NumeroDocumento == dto.NumeroDocumento)
                .Select(c => (Guid?)c.ClienteId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existente.HasValue && existente.Value != clienteIdActual)
                errores.Add($"Ya existe un cliente con documento {dto.TipoDocumento} {dto.NumeroDocumento}.");
        }

        return errores;
    }

    private static void MapearDtoAEntidad(ClienteDto dto, Cliente cliente, ResultadoResolucionCatalogo homologacion)
    {
        cliente.TipoDocumento = dto.TipoDocumento;
        cliente.NumeroDocumento = dto.NumeroDocumento;
        cliente.RazonSocial = dto.RazonSocial;
        cliente.PrimerNombre = dto.PrimerNombre;
        cliente.SegundoNombre = dto.SegundoNombre;
        cliente.PrimerApellido = dto.PrimerApellido;
        cliente.SegundoApellido = dto.SegundoApellido;
        cliente.FechaNacimiento = dto.FechaNacimiento;
        cliente.Direccion = dto.Direccion;
        cliente.Telefono = dto.Telefono;
        cliente.CorreoElectronico = dto.CorreoElectronico;
        cliente.PaisId = homologacion.PaisId;
        cliente.DepartamentoId = homologacion.DepartamentoId;
        cliente.MunicipioId = homologacion.MunicipioId;
        cliente.DigitoVerificacion = (byte?)dto.DigitoVerificacion;
    }

    private static string? ObtenerValor(IReadOnlyDictionary<string, string> fila, string columna)
        => fila.TryGetValue(columna, out var valor) && !string.IsNullOrWhiteSpace(valor) ? valor.Trim() : null;
}
