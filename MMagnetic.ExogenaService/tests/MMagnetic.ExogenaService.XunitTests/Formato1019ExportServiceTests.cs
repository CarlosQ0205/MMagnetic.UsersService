using System.Text;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dian;
using MMagnetic.ExogenaService.Models.Formato1019;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.XunitTests;

public class Formato1019ExportServiceTests
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

    private static void SembrarClienteValido(ClientesDbContext clientes)
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
    }

    [Fact]
    public async Task Sin_registros_en_definitivo_retorna_error_y_no_genera_archivo()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Sin_registros_en_definitivo_retorna_error_y_no_genera_archivo));
        using var dianDb = CrearDianDbContext(nameof(Sin_registros_en_definitivo_retorna_error_y_no_genera_archivo) + "-dian");
        using var formatosDb = CrearFormatosDbContext(nameof(Sin_registros_en_definitivo_retorna_error_y_no_genera_archivo) + "-formatos");

        var ensamblador = new Formato1019EnsambladorService(clientesDb, new HomologadorDianService(dianDb));
        var exportador = new Formato1019ExportService(ensamblador, new Formato1019Validator(), formatosDb);

        var resultado = await exportador.GenerarXmlAsync(PeriodoAno, numEnvio: 1, codCpt: 1, fecInicial: new DateTime(2024, 1, 1), fecFinal: new DateTime(2024, 12, 31));

        resultado.Exitoso.Should().BeFalse();
        resultado.ContenidoXml.Should().BeNull();
        resultado.Errores.Should().Contain(e => e.Codigo == "EXPORT-VACIO");
    }

    [Fact]
    public async Task Genera_xml_valido_a_partir_de_los_registros_clasificados_como_definitivos()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Genera_xml_valido_a_partir_de_los_registros_clasificados_como_definitivos));
        using var dianDb = CrearDianDbContext(nameof(Genera_xml_valido_a_partir_de_los_registros_clasificados_como_definitivos) + "-dian");
        using var formatosDb = CrearFormatosDbContext(nameof(Genera_xml_valido_a_partir_de_los_registros_clasificados_como_definitivos) + "-formatos");
        SembrarClienteValido(clientesDb);

        var ensamblador = new Formato1019EnsambladorService(clientesDb, new HomologadorDianService(dianDb));
        var validator = new Formato1019Validator();
        var clasificador = new Formato1019ClasificadorService(ensamblador, validator, formatosDb);
        await clasificador.ClasificarYPersistirAsync(PeriodoAno, usuarioId: null);

        var exportador = new Formato1019ExportService(ensamblador, validator, formatosDb);
        var fecInicial = new DateTime(2024, 1, 1);
        var fecFinal = new DateTime(2024, 12, 31);

        var resultado = await exportador.GenerarXmlAsync(PeriodoAno, numEnvio: 5, codCpt: 1, fecInicial, fecFinal);

        resultado.Exitoso.Should().BeTrue(because: string.Join("; ", resultado.Errores.Select(e => e.Mensaje)));
        resultado.ContenidoXml.Should().NotBeNullOrEmpty();
        resultado.NombreArchivo.Should().Be(Formato1019FileNaming.ConstruirNombreArchivo(1, PeriodoAno, 5));

        var textoXml = Encoding.GetEncoding("ISO-8859-1").GetString(resultado.ContenidoXml!);
        textoXml.Should().ContainEquivalentOf("iso-8859-1");
        textoXml.Should().Contain("<mas>");

        var serializer = new XmlSerializer(typeof(CargaMasiva1019));
        using var stream = new MemoryStream(resultado.ContenidoXml!);
        var carga = (CargaMasiva1019)serializer.Deserialize(stream)!;

        carga.Cab.Ano.Should().Be(PeriodoAno);
        carga.Cab.NumEnvio.Should().Be(5);
        carga.Cab.CantReg.Should().Be(1);
        carga.Movimientos.Should().ContainSingle();
        carga.Movimientos[0].Nid.Should().Be("123456789");
        carga.Movimientos[0].Cta.Should().Be("1234567890");
    }

    [Fact]
    public void ConstruirNombreArchivo_sigue_el_estandar_de_nombre_del_anexo_2()
    {
        var nombre = Formato1019FileNaming.ConstruirNombreArchivo(codCpt: 1, periodoAno: 2024, numEnvio: 5);

        nombre.Should().StartWith("Dmuisca_");
        nombre.Should().EndWith(".xml");

        var cuerpo = nombre["Dmuisca_".Length..^".xml".Length];
        cuerpo.Should().HaveLength(21);
        cuerpo.Substring(0, 2).Should().Be("01");       // cc: concepto = inserción
        cuerpo.Substring(2, 5).Should().Be("01019");    // mmmmm: formato
        cuerpo.Substring(7, 2).Should().Be("09");       // vv: versión
        cuerpo.Substring(9, 4).Should().Be("2024");     // aaaa: año
        cuerpo.Substring(13, 8).Should().Be("00000005"); // cccccccc: consecutivo
    }
}
