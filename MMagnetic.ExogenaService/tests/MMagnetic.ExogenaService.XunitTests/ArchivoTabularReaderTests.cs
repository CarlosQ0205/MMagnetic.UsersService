using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MMagnetic.ExogenaService.Services.CargaMasiva;

namespace MMagnetic.ExogenaService.XunitTests;

public class ArchivoTabularReaderTests
{
    private readonly ArchivoTabularReader _lector = new();

    private static IFormFile CrearArchivo(byte[] contenido, string nombreArchivo)
    {
        var stream = new MemoryStream(contenido);
        return new FormFile(stream, 0, stream.Length, "archivo", nombreArchivo);
    }

    [Fact]
    public async Task Lee_un_csv_simple_correctamente()
    {
        const string csv = "TipoDocumento,NumeroDocumento,PrimerNombre\nCC,123456789,Juan\nNIT,900123456,Empresa SAS\n";
        var archivo = CrearArchivo(Encoding.UTF8.GetBytes(csv), "clientes.csv");

        var filas = await _lector.LeerAsync(archivo);

        filas.Should().HaveCount(2);
        filas[0]["TipoDocumento"].Should().Be("CC");
        filas[0]["NumeroDocumento"].Should().Be("123456789");
        filas[0]["PrimerNombre"].Should().Be("Juan");
        filas[1]["NumeroDocumento"].Should().Be("900123456");
    }

    [Fact]
    public async Task Ignora_filas_completamente_vacias_en_csv()
    {
        const string csv = "TipoDocumento,NumeroDocumento\nCC,123\n,\nCC,456\n";
        var archivo = CrearArchivo(Encoding.UTF8.GetBytes(csv), "clientes.csv");

        var filas = await _lector.LeerAsync(archivo);

        filas.Should().HaveCount(2);
    }

    [Fact]
    public async Task Lee_un_xlsx_simple_correctamente()
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Clientes");
        hoja.Cell(1, 1).Value = "TipoDocumento";
        hoja.Cell(1, 2).Value = "NumeroDocumento";
        hoja.Cell(1, 3).Value = "PrimerNombre";
        hoja.Cell(2, 1).Value = "CC";
        hoja.Cell(2, 2).Value = "123456789";
        hoja.Cell(2, 3).Value = "Juan";

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        var archivo = CrearArchivo(stream.ToArray(), "clientes.xlsx");

        var filas = await _lector.LeerAsync(archivo);

        filas.Should().ContainSingle();
        filas[0]["TipoDocumento"].Should().Be("CC");
        filas[0]["NumeroDocumento"].Should().Be("123456789");
        filas[0]["PrimerNombre"].Should().Be("Juan");
    }

    [Fact]
    public async Task Rechaza_formatos_no_soportados()
    {
        var archivo = CrearArchivo(Encoding.UTF8.GetBytes("contenido"), "clientes.txt");

        var accion = async () => await _lector.LeerAsync(archivo);

        await accion.Should().ThrowAsync<NotSupportedException>();
    }
}
