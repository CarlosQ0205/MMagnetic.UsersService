using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Clientes;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.XunitTests;

public class CotitularServiceTests
{
    private static ClientesDbContext CrearClientesDbContext(string nombre)
    {
        var options = new DbContextOptionsBuilder<ClientesDbContext>().UseInMemoryDatabase(nombre).Options;
        return new ClientesDbContext(options);
    }

    private static Guid SembrarCliente(ClientesDbContext clientes, string numeroDocumento = "123456789")
    {
        var clienteId = Guid.NewGuid();
        clientes.Clientes.Add(new Cliente
        {
            ClienteId = clienteId,
            TipoDocumento = "CC",
            NumeroDocumento = numeroDocumento,
            PrimerNombre = "Juan",
            PrimerApellido = "Perez",
            FechaRegistro = DateTime.UtcNow,
            EsActivo = true,
        });
        clientes.SaveChanges();
        return clienteId;
    }

    [Fact]
    public async Task Crea_un_cotitular_vinculado_al_cliente_por_numero_de_documento()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Crea_un_cotitular_vinculado_al_cliente_por_numero_de_documento));
        var clienteId = SembrarCliente(clientesDb);
        var servicio = new CotitularService(clientesDb, new ArchivoTabularReader());

        var resultado = await servicio.CrearAsync(new CotitularDto
        {
            NumeroDocumentoCliente = "123456789",
            TipoDocumento = "CC",
            NumeroDocumento = "800111222",
            NombreCompleto = "Maria Lopez",
            Participacion = 50,
        });

        resultado.Exitoso.Should().BeTrue();
        resultado.Dato!.ClienteId.Should().Be(clienteId);
    }

    [Fact]
    public async Task Rechaza_si_el_cliente_no_existe()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Rechaza_si_el_cliente_no_existe));
        var servicio = new CotitularService(clientesDb, new ArchivoTabularReader());

        var resultado = await servicio.CrearAsync(new CotitularDto
        {
            NumeroDocumentoCliente = "no-existe",
            NumeroDocumento = "800111222",
            NombreCompleto = "Maria Lopez",
        });

        resultado.Exitoso.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Contains("no-existe"));
    }

    [Fact]
    public async Task Rechaza_participacion_fuera_de_rango()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Rechaza_participacion_fuera_de_rango));
        SembrarCliente(clientesDb);
        var servicio = new CotitularService(clientesDb, new ArchivoTabularReader());

        var resultado = await servicio.CrearAsync(new CotitularDto
        {
            NumeroDocumentoCliente = "123456789",
            NumeroDocumento = "800111222",
            NombreCompleto = "Maria Lopez",
            Participacion = 150,
        });

        resultado.Exitoso.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Contains("Participacion"));
    }

    [Fact]
    public async Task Eliminar_borra_el_cotitular()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Eliminar_borra_el_cotitular));
        SembrarCliente(clientesDb);
        var servicio = new CotitularService(clientesDb, new ArchivoTabularReader());
        var creado = await servicio.CrearAsync(new CotitularDto
        {
            NumeroDocumentoCliente = "123456789",
            NumeroDocumento = "800111222",
            NombreCompleto = "Maria Lopez",
        });

        var eliminado = await servicio.EliminarAsync(creado.Dato!.CotitularId);

        eliminado.Should().BeTrue();
        (await clientesDb.Cotitulares.CountAsync()).Should().Be(0);
    }
}
