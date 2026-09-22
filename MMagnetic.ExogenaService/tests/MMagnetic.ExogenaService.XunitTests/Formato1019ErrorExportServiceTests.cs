using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dian;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.XunitTests;

public class Formato1019ErrorExportServiceTests
{
    private const int PeriodoAno = 2024;

    private static ClientesDbContext CrearClientesDbContext(string nombre)
    {
        var options = new DbContextOptionsBuilder<ClientesDbContext>().UseInMemoryDatabase(nombre).Options;
        return new ClientesDbContext(options);
    }

    private static DianDbContext CrearDianDbContext(string nombre)
    {
        var options = new DbContextOptionsBuilder<DianDbContext>().UseInMemoryDatabase(nombre).Options;
        var contexto = new DianDbContext(options);
        contexto.Paises.Add(new DianPais { PaisId = 1, CodigoPais = "169", NombrePais = "COLOMBIA" });
        contexto.Departamentos.Add(new DianDepartamento { DepartamentoId = 10, PaisId = 1, CodigoDepartamento = "05", NombreDepartamento = "Antioquia" });
        contexto.Municipios.Add(new DianMunicipio { MunicipioId = 100, DepartamentoId = 10, PaisId = 1, CodigoMunicipio = "001", NombreMunicipio = "Medellín" });
        contexto.SaveChanges();
        return contexto;
    }

    /// <summary>Cliente con error de cuenta (codex fuera de rango) pero datos de cliente correctos.</summary>
    private static Guid SembrarClienteConErrorDeCuenta(ClientesDbContext clientes)
    {
        var clienteId = Guid.NewGuid();
        clientes.Clientes.Add(new Cliente
        {
            ClienteId = clienteId,
            TipoDocumento = "CC",
            NumeroDocumento = "123456789",
            PrimerNombre = "Juan",
            PrimerApellido = "Perez",
            PaisId = 1,
            DepartamentoId = 10,
            MunicipioId = 100,
            FechaRegistro = DateTime.UtcNow,
            EsActivo = true,
        });

        clientes.DatosFinancieros.Add(new DatoFinanciero
        {
            DatoFinancieroId = Guid.NewGuid(),
            ClienteId = clienteId,
            PeriodoAno = PeriodoAno,
            NumeroCuenta = "1234567890",
            TipoCuenta = 1,
            CodigoExencion = 150, // fuera de rango (0-99): MOV-CODEX
            Saldo = 1_000_000,
            PromedioSaldoFinal = 900_000,
            MedianaSaldoDiario = 850_000,
            SaldoMaximo = 1_200_000,
            SaldoMinimo = 500_000,
            ValorMovCredito = 200_000,
            NumMovCredito = 10,
            PromedioMovCredito = 20_000,
            MedianaMovCredito = 15_000,
            ValorMovDebito = 150_000,
            NumMovDebito = 8,
            PromedioMovDebito = 18_000,
            FechaRegistro = DateTime.UtcNow,
        });

        clientes.SaveChanges();
        return clienteId;
    }

    /// <summary>Cliente con error propio (tipo de documento inválido) y cuenta correcta.</summary>
    private static Guid SembrarClienteConErrorPropio(ClientesDbContext clientes)
    {
        var clienteId = Guid.NewGuid();
        clientes.Clientes.Add(new Cliente
        {
            ClienteId = clienteId,
            TipoDocumento = "CC",
            NumeroDocumento = "987654321",
            PrimerNombre = null, // sin nombre ni razón social: MOV-TITULAR
            PrimerApellido = null,
            PaisId = 1,
            DepartamentoId = 10,
            MunicipioId = 100,
            FechaRegistro = DateTime.UtcNow,
            EsActivo = true,
        });

        clientes.DatosFinancieros.Add(new DatoFinanciero
        {
            DatoFinancieroId = Guid.NewGuid(),
            ClienteId = clienteId,
            PeriodoAno = PeriodoAno,
            NumeroCuenta = "9999999999",
            TipoCuenta = 1,
            CodigoExencion = 5,
            Saldo = 500_000,
            PromedioSaldoFinal = 500_000,
            MedianaSaldoDiario = 500_000,
            SaldoMaximo = 500_000,
            SaldoMinimo = 500_000,
            ValorMovCredito = 10_000,
            NumMovCredito = 1,
            PromedioMovCredito = 10_000,
            MedianaMovCredito = 10_000,
            ValorMovDebito = 10_000,
            NumMovDebito = 1,
            PromedioMovDebito = 10_000,
            FechaRegistro = DateTime.UtcNow,
        });

        clientes.SaveChanges();
        return clienteId;
    }

    [Fact]
    public async Task Exporta_la_linea_completa_del_cliente_y_de_la_cuenta_con_error()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Exporta_la_linea_completa_del_cliente_y_de_la_cuenta_con_error));
        using var dianDb = CrearDianDbContext(nameof(Exporta_la_linea_completa_del_cliente_y_de_la_cuenta_con_error) + "-dian");
        SembrarClienteConErrorDeCuenta(clientesDb);

        var homologador = new HomologadorDianService(dianDb);
        var ensamblador = new Formato1019EnsambladorService(clientesDb, homologador);
        var validator = new Formato1019Validator();
        var servicio = new Formato1019ErrorExportService(ensamblador, validator, homologador, clientesDb);

        var contenido = await servicio.ExportarErroresAsync(PeriodoAno);

        contenido.Should().NotBeEmpty();

        using var libro = new XLWorkbook(new MemoryStream(contenido));
        var hoja = libro.Worksheet("Errores_F1019");

        // Encabezados presentes.
        hoja.Cell(1, 1).GetString().Should().Be("NumeroDocumento");
        hoja.Cell(1, 40).GetString().Should().Be("Errores");

        // La fila 2 trae la línea completa (cliente + cuenta) de esa cuenta, no solo el dato que falló.
        hoja.Cell(2, 1).GetString().Should().Be("123456789"); // NumeroDocumento
        hoja.Cell(2, 20).GetString().Should().Be("1234567890"); // NumeroCuenta
        hoja.Cell(2, 23).GetValue<decimal>().Should().Be(1_000_000); // Saldo
        hoja.Cell(2, 40).GetString().Should().Contain("codex");
    }

    [Fact]
    public async Task Exporta_la_linea_completa_del_cliente_cuando_el_error_es_del_cliente()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Exporta_la_linea_completa_del_cliente_cuando_el_error_es_del_cliente));
        using var dianDb = CrearDianDbContext(nameof(Exporta_la_linea_completa_del_cliente_cuando_el_error_es_del_cliente) + "-dian");
        SembrarClienteConErrorPropio(clientesDb);

        var homologador = new HomologadorDianService(dianDb);
        var ensamblador = new Formato1019EnsambladorService(clientesDb, homologador);
        var validator = new Formato1019Validator();
        var servicio = new Formato1019ErrorExportService(ensamblador, validator, homologador, clientesDb);

        var contenido = await servicio.ExportarErroresAsync(PeriodoAno);

        using var libro = new XLWorkbook(new MemoryStream(contenido));
        var hoja = libro.Worksheet("Errores_F1019");

        hoja.Cell(2, 1).GetString().Should().Be("987654321"); // NumeroDocumento
        hoja.Cell(2, 12).GetString().Should().Be("169"); // CodigoPais con ceros a la izquierda
        hoja.Cell(2, 13).GetString().Should().Be("05");  // CodigoDepartamento con cero a la izquierda
        hoja.Cell(2, 14).GetString().Should().Be("001"); // CodigoMunicipio con ceros a la izquierda
        hoja.Cell(2, 20).GetString().Should().Be("9999999999"); // NumeroCuenta (misma línea trae también la cuenta)
        hoja.Cell(2, 40).GetString().Should().Contain("titular");
    }

    [Fact]
    public async Task Sin_registros_invalidos_genera_un_libro_con_solo_encabezados()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Sin_registros_invalidos_genera_un_libro_con_solo_encabezados));
        using var dianDb = CrearDianDbContext(nameof(Sin_registros_invalidos_genera_un_libro_con_solo_encabezados) + "-dian");

        var homologador = new HomologadorDianService(dianDb);
        var ensamblador = new Formato1019EnsambladorService(clientesDb, homologador);
        var validator = new Formato1019Validator();
        var servicio = new Formato1019ErrorExportService(ensamblador, validator, homologador, clientesDb);

        var contenido = await servicio.ExportarErroresAsync(PeriodoAno);

        using var libro = new XLWorkbook(new MemoryStream(contenido));
        libro.Worksheet("Errores_F1019").RowsUsed().Should().ContainSingle();
    }
}
