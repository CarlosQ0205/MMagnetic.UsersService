using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.XunitTests;

public class DatoFinancieroServiceTests
{
    private static ClientesDbContext CrearClientesDbContext(string nombre)
    {
        var options = new DbContextOptionsBuilder<ClientesDbContext>().UseInMemoryDatabase(nombre).Options;
        return new ClientesDbContext(options);
    }

    private static void SembrarCliente(ClientesDbContext clientes, string numeroDocumento = "123456789")
    {
        clientes.Clientes.Add(new Cliente
        {
            ClienteId = Guid.NewGuid(),
            TipoDocumento = "CC",
            NumeroDocumento = numeroDocumento,
            PrimerNombre = "Juan",
            PrimerApellido = "Perez",
            FechaRegistro = DateTime.UtcNow,
            EsActivo = true,
        });
        clientes.SaveChanges();
    }

    private static IFormFile CrearArchivoCsv(string contenido)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(contenido));
        return new FormFile(stream, 0, stream.Length, "archivo", "datos.csv");
    }

    [Fact]
    public async Task Crea_un_registro_financiero_valido()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Crea_un_registro_financiero_valido));
        SembrarCliente(clientesDb);
        var servicio = new DatoFinancieroService(clientesDb, new ArchivoTabularReader());

        var resultado = await servicio.CrearAsync(new DatoFinancieroDto
        {
            NumeroDocumentoCliente = "123456789",
            PeriodoAno = 2024,
            NumeroCuenta = "1234567890",
            TipoCuenta = 1,
            CodigoExencion = 5,
            Saldo = 1_000_000,
        });

        resultado.Exitoso.Should().BeTrue();
        resultado.Dato!.NumeroCuenta.Should().Be("1234567890");
    }

    [Fact]
    public async Task Carga_masiva_recargada_actualiza_en_vez_de_duplicar_la_misma_cuenta_y_periodo()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Carga_masiva_recargada_actualiza_en_vez_de_duplicar_la_misma_cuenta_y_periodo));
        SembrarCliente(clientesDb);
        var servicio = new DatoFinancieroService(clientesDb, new ArchivoTabularReader());

        const string encabezados = "NumeroDocumentoCliente,PeriodoAno,NumeroCuenta,TipoCuenta,CodigoExencion,Saldo\n";
        var primeraCarga = await servicio.CargarMasivoAsync(CrearArchivoCsv(encabezados + "123456789,2024,1234567890,1,5,1000000\n"));
        primeraCarga.Exitosas.Should().Be(1);

        // Mismo cliente+cuenta+periodo, pero con el saldo corregido.
        var segundaCarga = await servicio.CargarMasivoAsync(CrearArchivoCsv(encabezados + "123456789,2024,1234567890,1,5,2000000\n"));
        segundaCarga.Exitosas.Should().Be(1);

        var registros = await clientesDb.DatosFinancieros.ToListAsync();
        registros.Should().ContainSingle(); // no se duplicó
        registros[0].Saldo.Should().Be(2_000_000);
    }

    [Fact]
    public async Task Carga_masiva_reporta_error_si_el_cliente_no_existe()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Carga_masiva_reporta_error_si_el_cliente_no_existe));
        var servicio = new DatoFinancieroService(clientesDb, new ArchivoTabularReader());

        const string csv = "NumeroDocumentoCliente,PeriodoAno,NumeroCuenta,TipoCuenta,CodigoExencion,Saldo\n" +
                            "no-existe,2024,1234567890,1,5,1000000\n";

        var resultado = await servicio.CargarMasivoAsync(CrearArchivoCsv(csv));

        resultado.Exitosas.Should().Be(0);
        resultado.Errores.Should().ContainSingle(e => e.Mensaje.Contains("no-existe"));
    }

    [Fact]
    public async Task Eliminar_borra_el_registro_financiero()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Eliminar_borra_el_registro_financiero));
        SembrarCliente(clientesDb);
        var servicio = new DatoFinancieroService(clientesDb, new ArchivoTabularReader());
        var creado = await servicio.CrearAsync(new DatoFinancieroDto { NumeroDocumentoCliente = "123456789", PeriodoAno = 2024, NumeroCuenta = "111" });

        var eliminado = await servicio.EliminarAsync(creado.Dato!.DatoFinancieroId);

        eliminado.Should().BeTrue();
        (await clientesDb.DatosFinancieros.CountAsync()).Should().Be(0);
    }
}
