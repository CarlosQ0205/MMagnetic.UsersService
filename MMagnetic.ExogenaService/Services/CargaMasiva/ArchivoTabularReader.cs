using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;

namespace MMagnetic.ExogenaService.Services.CargaMasiva;

public class ArchivoTabularReader : IArchivoTabularReader
{
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> LeerAsync(
        IFormFile archivo, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        return extension switch
        {
            ".csv" => await LeerCsvAsync(archivo, cancellationToken),
            ".xlsx" => LeerXlsx(archivo),
            _ => throw new NotSupportedException($"Formato de archivo no soportado: '{extension}'. Solo se aceptan .xlsx y .csv."),
        };
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> LeerCsvAsync(
        IFormFile archivo, CancellationToken cancellationToken)
    {
        var filas = new List<IReadOnlyDictionary<string, string>>();

        using var stream = archivo.OpenReadStream();
        using var lector = new StreamReader(stream);
        using var csv = new CsvReader(lector, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        });

        if (!await csv.ReadAsync() || !csv.ReadHeader())
            return filas;

        var encabezados = csv.HeaderRecord ?? Array.Empty<string>();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var encabezado in encabezados)
                fila[encabezado] = csv.GetField(encabezado) ?? string.Empty;

            if (fila.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                filas.Add(fila);
        }

        return filas;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> LeerXlsx(IFormFile archivo)
    {
        var filas = new List<IReadOnlyDictionary<string, string>>();

        using var stream = archivo.OpenReadStream();
        using var libro = new XLWorkbook(stream);
        var hoja = libro.Worksheets.First();
        var rangoUsado = hoja.RangeUsed();
        if (rangoUsado is null)
            return filas;

        var filaEncabezados = rangoUsado.FirstRow();
        var encabezados = filaEncabezados.Cells()
            .Select(c => c.GetString().Trim())
            .ToList();

        foreach (var filaExcel in rangoUsado.RowsUsed().Skip(1))
        {
            var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < encabezados.Count; i++)
            {
                var celda = filaExcel.Cell(i + 1);
                fila[encabezados[i]] = celda.DataType == XLDataType.DateTime
                    ? celda.GetDateTime().ToString("yyyy-MM-dd")
                    : celda.GetString().Trim();
            }

            if (fila.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                filas.Add(fila);
        }

        return filas;
    }
}
