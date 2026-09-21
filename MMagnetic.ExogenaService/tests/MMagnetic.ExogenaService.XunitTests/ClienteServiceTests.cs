using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Dian;
using MMagnetic.ExogenaService.Models.Dto;
using MMagnetic.ExogenaService.Services.CargaMasiva;
using MMagnetic.ExogenaService.Services.Clientes;

namespace MMagnetic.ExogenaService.XunitTests;

public class ClienteServiceTests
{
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

    private static ClienteDto DtoValido() => new()
    {
        TipoDocumento = "CC",
        NumeroDocumento = "123456789",
        PrimerNombre = "Juan",
        PrimerApellido = "Perez",
        CodigoPais = "169",
        CodigoDepartamento = "05",
        CodigoMunicipio = "001",
    };

    [Fact]
    public async Task Crea_un_cliente_valido_y_homologa_los_codigos_dian()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Crea_un_cliente_valido_y_homologa_los_codigos_dian));
        using var dianDb = CrearDianDbContext(nameof(Crea_un_cliente_valido_y_homologa_los_codigos_dian) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());

        var resultado = await servicio.CrearAsync(DtoValido());

        resultado.Exitoso.Should().BeTrue();
        resultado.Dato!.PaisId.Should().Be(1);
        resultado.Dato.DepartamentoId.Should().Be(10);
        resultado.Dato.MunicipioId.Should().Be(100);
        resultado.Dato.EsActivo.Should().BeTrue();
    }

    [Fact]
    public async Task Rechaza_tipo_de_documento_desconocido()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Rechaza_tipo_de_documento_desconocido));
        using var dianDb = CrearDianDbContext(nameof(Rechaza_tipo_de_documento_desconocido) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());

        var dto = DtoValido();
        dto.TipoDocumento = "XYZ";

        var resultado = await servicio.CrearAsync(dto);

        resultado.Exitoso.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Contains("XYZ"));
    }

    [Fact]
    public async Task Rechaza_codigo_dian_inexistente()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Rechaza_codigo_dian_inexistente));
        using var dianDb = CrearDianDbContext(nameof(Rechaza_codigo_dian_inexistente) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());

        var dto = DtoValido();
        dto.CodigoDepartamento = "99";

        var resultado = await servicio.CrearAsync(dto);

        resultado.Exitoso.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Contains("99"));
    }

    [Fact]
    public async Task Rechaza_documento_duplicado()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Rechaza_documento_duplicado));
        using var dianDb = CrearDianDbContext(nameof(Rechaza_documento_duplicado) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());
        await servicio.CrearAsync(DtoValido());

        var resultado = await servicio.CrearAsync(DtoValido());

        resultado.Exitoso.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Contains("Ya existe"));
    }

    [Fact]
    public async Task Actualizar_permite_conservar_el_mismo_documento_del_propio_cliente()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Actualizar_permite_conservar_el_mismo_documento_del_propio_cliente));
        using var dianDb = CrearDianDbContext(nameof(Actualizar_permite_conservar_el_mismo_documento_del_propio_cliente) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());
        var creado = await servicio.CrearAsync(DtoValido());

        var dto = DtoValido();
        dto.Telefono = "3000000000";
        var resultado = await servicio.ActualizarAsync(creado.Dato!.ClienteId, dto);

        resultado.Exitoso.Should().BeTrue();
        resultado.Dato!.Telefono.Should().Be("3000000000");
    }

    [Fact]
    public async Task Desactivar_hace_baja_logica_sin_borrar_el_registro()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Desactivar_hace_baja_logica_sin_borrar_el_registro));
        using var dianDb = CrearDianDbContext(nameof(Desactivar_hace_baja_logica_sin_borrar_el_registro) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());
        var creado = await servicio.CrearAsync(DtoValido());

        var desactivado = await servicio.DesactivarAsync(creado.Dato!.ClienteId);

        desactivado.Should().BeTrue();
        var listado = await servicio.ListarAsync();
        listado.Should().BeEmpty(); // Listar solo trae activos
        (await clientesDb.Clientes.CountAsync()).Should().Be(1); // sigue existiendo en la tabla
    }

    [Fact]
    public async Task Carga_masiva_desde_csv_crea_los_clientes_validos_y_reporta_los_invalidos()
    {
        using var clientesDb = CrearClientesDbContext(nameof(Carga_masiva_desde_csv_crea_los_clientes_validos_y_reporta_los_invalidos));
        using var dianDb = CrearDianDbContext(nameof(Carga_masiva_desde_csv_crea_los_clientes_validos_y_reporta_los_invalidos) + "-dian");
        var servicio = new ClienteService(clientesDb, new ResolutorCatalogoDianService(dianDb), new ArchivoTabularReader());

        const string csv = "TipoDocumento,NumeroDocumento,PrimerNombre,PrimerApellido,CodigoPais,CodigoDepartamento,CodigoMunicipio\n" +
                            "CC,123456789,Juan,Perez,169,05,001\n" +
                            "CC,,SinDocumento,Apellido,169,05,001\n";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var archivo = new FormFile(stream, 0, stream.Length, "archivo", "clientes.csv");

        var resultado = await servicio.CargarMasivoAsync(archivo);

        resultado.TotalFilas.Should().Be(2);
        resultado.Exitosas.Should().Be(1);
        resultado.ConErrores.Should().Be(1);
        resultado.Errores.Should().ContainSingle(e => e.Fila == 3);
    }
}
