using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public class Formato1019EnsambladorService : IFormato1019EnsambladorService
{
    private readonly ClientesDbContext _clientes;
    private readonly IHomologadorDianService _homologador;

    public Formato1019EnsambladorService(ClientesDbContext clientes, IHomologadorDianService homologador)
    {
        _clientes = clientes;
        _homologador = homologador;
    }

    public async Task<IReadOnlyList<RegistroFormato1019>> EnsamblarAsync(int periodoAno, CancellationToken cancellationToken = default)
    {
        var cuentas = await _clientes.DatosFinancieros
            .AsNoTracking()
            .Where(d => d.PeriodoAno == periodoAno && d.NumeroCuenta != null)
            .ToListAsync(cancellationToken);

        var clienteIds = cuentas.Select(c => c.ClienteId).Distinct().ToList();

        var clientesPorId = await _clientes.Clientes
            .AsNoTracking()
            .Where(c => clienteIds.Contains(c.ClienteId))
            .ToDictionaryAsync(c => c.ClienteId, cancellationToken);

        var cotitularesPorCliente = await _clientes.Cotitulares
            .AsNoTracking()
            .Where(co => clienteIds.Contains(co.ClienteId))
            .ToListAsync(cancellationToken);
        var cotitularesLookup = cotitularesPorCliente.GroupBy(co => co.ClienteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var registros = new List<RegistroFormato1019>(cuentas.Count);

        foreach (var cuenta in cuentas)
        {
            if (!clientesPorId.TryGetValue(cuenta.ClienteId, out var cliente))
                continue; // Cuenta huérfana (sin cliente); se reporta como error de integridad más adelante.

            var homologacion = await _homologador.HomologarAsync(
                cliente.PaisId, cliente.DepartamentoId, cliente.MunicipioId, cancellationToken);

            var movimiento = MapearMovimiento(cliente, cuenta, cotitularesLookup.GetValueOrDefault(cliente.ClienteId, []), homologacion);

            registros.Add(new RegistroFormato1019(
                cliente.ClienteId,
                cuenta.DatoFinancieroId,
                cuenta.NumeroCuenta!,
                periodoAno,
                movimiento,
                homologacion.Errores));
        }

        return registros;
    }

    private static MovimientoCuenta MapearMovimiento(
        Cliente cliente,
        DatoFinanciero cuenta,
        List<Cotitular> cotitulares,
        ResultadoHomologacion homologacion)
    {
        var movimiento = new MovimientoCuenta
        {
            Tdoc = CatalogoTiposDocumento.Mapear(cliente.TipoDocumento) ?? 0,
            Nid = cliente.NumeroDocumento,
            Apl1 = cliente.PrimerApellido,
            Apl2 = cliente.SegundoApellido,
            Nom1 = cliente.PrimerNombre,
            Nom2 = cliente.SegundoNombre,
            Raz = cliente.RazonSocial,
            Dir = cliente.Direccion,
            Cta = cuenta.NumeroCuenta ?? string.Empty,
            TipCta = cuenta.TipoCuenta ?? 0,
            Codex = cuenta.CodigoExencion ?? 0,
            Sal = cuenta.Saldo ?? 0,
            PSaldoF = cuenta.PromedioSaldoFinal ?? 0,
            MedDia = cuenta.MedianaSaldoDiario ?? 0,
            SMax = cuenta.SaldoMaximo ?? 0,
            SMin = cuenta.SaldoMinimo ?? 0,
            VCred = cuenta.ValorMovCredito ?? 0,
            MovCre = cuenta.NumMovCredito ?? 0,
            ProCre = cuenta.PromedioMovCredito ?? 0,
            MedCre = cuenta.MedianaMovCredito ?? 0,
            VMovDeb = cuenta.ValorMovDebito ?? 0,
            NMovDeb = cuenta.NumMovDebito ?? 0,
            PorDeb = cuenta.PromedioMovDebito ?? 0,
        };

        if (cliente.DigitoVerificacion.HasValue)
        {
            movimiento.Dv = cliente.DigitoVerificacion.Value;
            movimiento.DvSpecified = true;
        }

        if (homologacion.CodigoDepartamento.HasValue)
        {
            movimiento.Dpto = homologacion.CodigoDepartamento.Value;
            movimiento.DptoSpecified = true;
        }

        if (homologacion.CodigoMunicipio.HasValue)
        {
            movimiento.Mun = homologacion.CodigoMunicipio.Value;
            movimiento.MunSpecified = true;
        }

        if (homologacion.CodigoPais.HasValue)
        {
            movimiento.Pais = homologacion.CodigoPais.Value;
            movimiento.PaisSpecified = true;
        }

        // NOTA: Cotitulares no tiene columna "Concepto" (cpts) ni nombres separados en
        // apellidos/nombres (solo "NombreCompleto"), así que aquí solo se puede hacer un
        // mapeo aproximado: se asume Concepto=1 (cotitular) y se vuelca NombreCompleto en
        // "Nom1s". Hay que confirmar el código real de "Concepto" y, de ser necesario,
        // separar el nombre en Cotitulares para que esto sea fiel al Anexo 2.
        movimiento.TitularesSecundarios = cotitulares.Select(co => new TitularSecundario
        {
            Cpts = 1,
            Tdocs = CatalogoTiposDocumento.Mapear(co.TipoDocumento) ?? 0,
            Nids = co.NumeroDocumento,
            Nom1s = co.NombreCompleto,
        }).ToList();

        return movimiento;
    }
}
