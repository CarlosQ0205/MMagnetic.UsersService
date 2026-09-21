using System.Xml.Serialization;
using FluentAssertions;
using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.XunitTests;

public class Formato1019SerializationTests
{
    private const string XmlValido = """
        <?xml version="1.0" encoding="ISO-8859-1"?>
        <mas>
          <Cab>
            <Ano>2024</Ano>
            <CodCpt>1</CodCpt>
            <Formato>1019</Formato>
            <Version>9</Version>
            <NumEnvio>1</NumEnvio>
            <FecEnvio>2024-01-15T10:30:00</FecEnvio>
            <FecInicial>2024-01-01</FecInicial>
            <FecFinal>2024-01-31</FecFinal>
            <ValorTotal>15</ValorTotal>
            <CantReg>2</CantReg>
          </Cab>
          <movcta tdoc="13" nid="123456789" apl1="Perez" nom1="Juan" cta="1234567890" tipcta="1" codex="5"
                  sal="1000000" psaldof="900000" meddia="850000" smax="1200000" smin="500000"
                  vcred="200000" movcre="10" procre="20000" medcre="15000" vmovdeb="150000" nmovdeb="8" pordeb="18000" />
          <movcta tdoc="31" nid="900123456" dv="7" raz="Empresa SAS" cta="9876543210" tipcta="2" codex="10"
                  sal="-50000" psaldof="-40000" meddia="-30000" smax="10000" smin="-60000"
                  vcred="300000" movcre="5" procre="60000" medcre="50000" vmovdeb="100000" nmovdeb="3" pordeb="33000">
            <titSec cpts="1" tdocs="13" nids="800111222" />
          </movcta>
        </mas>
        """;

    private static CargaMasiva1019 Deserializar(string xml)
    {
        var serializer = new XmlSerializer(typeof(CargaMasiva1019));
        using var reader = new StringReader(xml);
        return (CargaMasiva1019)serializer.Deserialize(reader)!;
    }

    [Fact]
    public void Deserializa_encabezado_correctamente()
    {
        var carga = Deserializar(XmlValido);

        carga.Cab.Ano.Should().Be(2024);
        carga.Cab.CodCpt.Should().Be(1);
        carga.Cab.Formato.Should().Be(1019);
        carga.Cab.Version.Should().Be(9);
        carga.Cab.NumEnvio.Should().Be(1);
        carga.Cab.FecEnvio.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0));
        carga.Cab.FecInicial.Should().Be(new DateTime(2024, 1, 1));
        carga.Cab.FecFinal.Should().Be(new DateTime(2024, 1, 31));
        carga.Cab.ValorTotal.Should().Be(15);
        carga.Cab.CantReg.Should().Be(2);
    }

    [Fact]
    public void Deserializa_tdoc_correctamente_en_cada_movcta()
    {
        var carga = Deserializar(XmlValido);

        carga.Movimientos.Should().HaveCount(2);
        carga.Movimientos[0].Tdoc.Should().Be(13);
        carga.Movimientos[1].Tdoc.Should().Be(31);
    }

    [Fact]
    public void Atributos_opcionales_ausentes_quedan_sin_especificar()
    {
        var carga = Deserializar(XmlValido);

        carga.Movimientos[0].DvSpecified.Should().BeFalse();
        carga.Movimientos[0].DptoSpecified.Should().BeFalse();
        carga.Movimientos[0].MunSpecified.Should().BeFalse();
        carga.Movimientos[0].PaisSpecified.Should().BeFalse();
    }

    [Fact]
    public void Deserializa_titulares_secundarios()
    {
        var carga = Deserializar(XmlValido);

        carga.Movimientos[1].TitularesSecundarios.Should().ContainSingle();
        carga.Movimientos[1].TitularesSecundarios[0].Nids.Should().Be("800111222");
    }

    [Fact]
    public void Serializa_fechas_en_el_formato_exacto_exigido_por_la_dian()
    {
        var carga = Deserializar(XmlValido);
        var serializer = new XmlSerializer(typeof(CargaMasiva1019));
        using var writer = new StringWriter();
        serializer.Serialize(writer, carga);
        var xmlSerializado = writer.ToString();

        xmlSerializado.Should().Contain("<FecEnvio>2024-01-15T10:30:00</FecEnvio>");
        xmlSerializado.Should().Contain("<FecInicial>2024-01-01</FecInicial>");
        xmlSerializado.Should().Contain("<FecFinal>2024-01-31</FecFinal>");
    }
}
