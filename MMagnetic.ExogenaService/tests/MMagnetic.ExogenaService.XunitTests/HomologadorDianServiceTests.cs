using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Models.Dian;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.XunitTests;

public class HomologadorDianServiceTests
{
    private static DianDbContext CrearContextoConCatalogo()
    {
        var options = new DbContextOptionsBuilder<DianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var contexto = new DianDbContext(options);
        contexto.Paises.Add(new DianPais { PaisId = 1, CodigoPais = "169", NombrePais = "COLOMBIA" });
        contexto.Paises.Add(new DianPais { PaisId = 2, CodigoPais = "249", NombrePais = "ESTADOS UNIDOS" });
        contexto.Departamentos.Add(new DianDepartamento { DepartamentoId = 10, PaisId = 1, CodigoDepartamento = "11", NombreDepartamento = "BOGOTA D.C." });
        contexto.Municipios.Add(new DianMunicipio { MunicipioId = 100, DepartamentoId = 10, PaisId = 1, CodigoMunicipio = "001", NombreMunicipio = "BOGOTA D.C." });
        contexto.SaveChanges();
        return contexto;
    }

    [Fact]
    public async Task Homologa_pais_departamento_y_municipio_correctamente()
    {
        using var dian = CrearContextoConCatalogo();
        var homologador = new HomologadorDianService(dian);

        var resultado = await homologador.HomologarAsync(paisId: 1, departamentoId: 10, municipioId: 100);

        resultado.EsValido.Should().BeTrue();
        resultado.CodigoPais.Should().Be(169);
        resultado.CodigoDepartamento.Should().Be(11);
        resultado.CodigoMunicipio.Should().Be(1);
    }

    [Fact]
    public async Task PaisId_inexistente_genera_error()
    {
        using var dian = CrearContextoConCatalogo();
        var homologador = new HomologadorDianService(dian);

        var resultado = await homologador.HomologarAsync(paisId: 999, departamentoId: null, municipioId: null);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Contains("999"));
    }

    [Fact]
    public async Task Colombia_sin_departamento_ni_municipio_genera_error()
    {
        using var dian = CrearContextoConCatalogo();
        var homologador = new HomologadorDianService(dian);

        var resultado = await homologador.HomologarAsync(paisId: 1, departamentoId: null, municipioId: null);

        resultado.Errores.Should().Contain(e => e.Contains("Colombia"));
    }

    [Fact]
    public async Task Pais_distinto_de_colombia_no_exige_departamento_ni_municipio()
    {
        using var dian = CrearContextoConCatalogo();
        var homologador = new HomologadorDianService(dian);

        var resultado = await homologador.HomologarAsync(paisId: 2, departamentoId: null, municipioId: null);

        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public async Task Departamento_que_no_pertenece_al_pais_informado_genera_error()
    {
        using var dian = CrearContextoConCatalogo();
        var homologador = new HomologadorDianService(dian);

        var resultado = await homologador.HomologarAsync(paisId: 2, departamentoId: 10, municipioId: null);

        resultado.Errores.Should().Contain(e => e.Contains("no pertenece"));
    }
}
