using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dian;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.XunitTests;

public class Formato1019PipelineTests
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
        contexto.Departamentos.Add(new DianDepartamento { DepartamentoId = 10, PaisId = 1, CodigoDepartamento = "11", NombreDepartamento = "BOGOTA D.C." });
        contexto.Municipios.Add(new DianMunicipio { MunicipioId = 100, DepartamentoId = 10, PaisId = 1, CodigoMunicipio = "001", NombreMunicipio = "BOGOTA D.C." });
        contexto.SaveChanges();
        return contexto;
    }

    private static FormatosDbContext CrearFormatosDbContext(string nombre)
    {
        var options = new DbContextOptionsBuilder<FormatosDbContext>().UseInMemoryDatabase(nombre).Options;
        return new FormatosDbContext(options);
    }

    private static (Guid ClienteId, Guid DatoFinancieroId) SembrarClienteValido(ClientesDbContext clientes)
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

        clientes.Cotitulares.Add(new Cotitular
        {
            CotitularId = Guid.NewGuid(),
            ClienteId = clienteId,
            TipoDocumento = "CC",
            NumeroDocumento = "800111222",
            NombreCompleto = "Maria Lopez",
            FechaRegistro = DateTime.UtcNow,
        });

        var datoFinancieroId = Guid.NewGuid();
        clientes.DatosFinancieros.Add(new DatoFinanciero
        {
            DatoFinancieroId = datoFinancieroId,
            ClienteId = clienteId,
            PeriodoAno = PeriodoAno,
            NumeroCuenta = "1234567890",
            TipoCuenta = 1,
            CodigoExencion = 5,
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
        return (clienteId, datoFinancieroId);
    }

    [Fact]
    public async Task Ensamblador_arma_el_movcta_con_los_datos_del_cliente_cuenta_y_cotitulares()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Ensamblador_arma_el_movcta_con_los_datos_del_cliente_cuenta_y_cotitulares));
        using var dianDb = CrearDianDbContext(nameof(Ensamblador_arma_el_movcta_con_los_datos_del_cliente_cuenta_y_cotitulares) + "-dian");
        SembrarClienteValido(clientesDb);

        var ensamblador = new Formato1019EnsambladorService(clientesDb, new HomologadorDianService(dianDb));

        var registros = await ensamblador.EnsamblarAsync(PeriodoAno);

        registros.Should().ContainSingle();
        var movimiento = registros[0].Movimiento;
        movimiento.Tdoc.Should().Be(13); // CC -> 13
        movimiento.Nid.Should().Be("123456789");
        movimiento.Apl1.Should().Be("Perez");
        movimiento.Nom1.Should().Be("Juan");
        movimiento.Cta.Should().Be("1234567890");
        movimiento.DptoSpecified.Should().BeTrue();
        movimiento.Dpto.Should().Be(11);
        movimiento.MunSpecified.Should().BeTrue();
        movimiento.Mun.Should().Be(1);
        movimiento.PaisSpecified.Should().BeTrue();
        movimiento.Pais.Should().Be(169);
        movimiento.TitularesSecundarios.Should().ContainSingle();
        movimiento.TitularesSecundarios[0].Nids.Should().Be("800111222");
        movimiento.TitularesSecundarios[0].Tdocs.Should().Be(13);
    }

    [Fact]
    public async Task Clasificador_envia_cuenta_valida_a_definitivo_sin_errores()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Clasificador_envia_cuenta_valida_a_definitivo_sin_errores));
        using var dianDb = CrearDianDbContext(nameof(Clasificador_envia_cuenta_valida_a_definitivo_sin_errores) + "-dian");
        using var formatosDb = CrearFormatosDbContext(nameof(Clasificador_envia_cuenta_valida_a_definitivo_sin_errores) + "-formatos");
        SembrarClienteValido(clientesDb);

        var ensamblador = new Formato1019EnsambladorService(clientesDb, new HomologadorDianService(dianDb));
        var clasificador = new Formato1019ClasificadorService(ensamblador, new Formato1019Validator(), formatosDb);

        var resumen = await clasificador.ClasificarYPersistirAsync(PeriodoAno, usuarioId: null);

        resumen.TotalRegistros.Should().Be(1);
        resumen.Validos.Should().Be(1);
        resumen.ConErrores.Should().Be(0);

        formatosDb.Formato1019.Should().NotBeEmpty();
        formatosDb.Formato1019Definitivo.Count().Should().Be(formatosDb.Formato1019.Count());
        formatosDb.FormatoErrores.Should().BeEmpty();
    }

    [Fact]
    public async Task Clasificador_envia_cuenta_invalida_a_formato_errores_y_no_a_definitivo()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Clasificador_envia_cuenta_invalida_a_formato_errores_y_no_a_definitivo));
        using var dianDb = CrearDianDbContext(nameof(Clasificador_envia_cuenta_invalida_a_formato_errores_y_no_a_definitivo) + "-dian");
        using var formatosDb = CrearFormatosDbContext(nameof(Clasificador_envia_cuenta_invalida_a_formato_errores_y_no_a_definitivo) + "-formatos");
        SembrarClienteValido(clientesDb);
        // codex fuera de rango (0-99): fuerza que el registro sea inválido.
        clientesDb.DatosFinancieros.First().CodigoExencion = 150;
        clientesDb.SaveChanges();

        var ensamblador = new Formato1019EnsambladorService(clientesDb, new HomologadorDianService(dianDb));
        var clasificador = new Formato1019ClasificadorService(ensamblador, new Formato1019Validator(), formatosDb);

        var resumen = await clasificador.ClasificarYPersistirAsync(PeriodoAno, usuarioId: null);

        resumen.Validos.Should().Be(0);
        resumen.ConErrores.Should().Be(1);

        formatosDb.FormatoErrores.Should().Contain(e => e.CodigoError == "MOV-CODEX");
        formatosDb.Formato1019Definitivo.Should().BeEmpty();
        // El detalle igual queda registrado en el staging para trazabilidad.
        formatosDb.Formato1019.Should().Contain(c => c.CodigoConcepto == "codex" && c.Valor == 150);
    }

    [Fact]
    public async Task Reclasificar_el_mismo_periodo_reemplaza_el_resultado_anterior_sin_duplicar()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Reclasificar_el_mismo_periodo_reemplaza_el_resultado_anterior_sin_duplicar));
        using var dianDb = CrearDianDbContext(nameof(Reclasificar_el_mismo_periodo_reemplaza_el_resultado_anterior_sin_duplicar) + "-dian");
        using var formatosDb = CrearFormatosDbContext(nameof(Reclasificar_el_mismo_periodo_reemplaza_el_resultado_anterior_sin_duplicar) + "-formatos");
        SembrarClienteValido(clientesDb);
        clientesDb.DatosFinancieros.First().CodigoExencion = 150; // inválido a propósito
        clientesDb.SaveChanges();

        var ensamblador = new Formato1019EnsambladorService(clientesDb, new HomologadorDianService(dianDb));
        var clasificador = new Formato1019ClasificadorService(ensamblador, new Formato1019Validator(), formatosDb);

        var primeraCorrida = await clasificador.ClasificarYPersistirAsync(PeriodoAno, usuarioId: null);
        primeraCorrida.ConErrores.Should().Be(1);
        var totalConceptosTrasError = formatosDb.Formato1019.Count();

        // El usuario corrige el dato de origen y vuelve a clasificar el mismo período.
        clientesDb.DatosFinancieros.First().CodigoExencion = 5;
        clientesDb.SaveChanges();
        var segundaCorrida = await clasificador.ClasificarYPersistirAsync(PeriodoAno, usuarioId: null);

        segundaCorrida.ConErrores.Should().Be(0);
        segundaCorrida.Validos.Should().Be(1);
        formatosDb.FormatoErrores.Should().BeEmpty();
        formatosDb.Formato1019Definitivo.Count().Should().Be(totalConceptosTrasError);
        formatosDb.Formato1019.Count().Should().Be(totalConceptosTrasError); // no se acumuló, se reemplazó
    }
}
