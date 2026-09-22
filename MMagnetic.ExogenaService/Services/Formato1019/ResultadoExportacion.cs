namespace MMagnetic.ExogenaService.Services.Formato1019;

public record ResultadoExportacion(
    bool Exitoso,
    string? NombreArchivo,
    byte[]? ContenidoXml,
    IReadOnlyList<Formato1019ValidationError> Errores);

public record ArchivoLote(string NombreArchivo, byte[] Contenido);

/// <summary>
/// Resultado de generar F_1019_Definitivo en varios archivos (máximo 5000 "movcta" por
/// archivo, el límite del Anexo 2), pensado para pasar por el pre-validador de la DIAN.
/// </summary>
public record ResultadoLotesExportacion(
    bool Exitoso,
    IReadOnlyList<ArchivoLote>? Archivos,
    IReadOnlyList<Formato1019ValidationError> Errores);
