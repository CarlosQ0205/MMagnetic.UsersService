namespace MMagnetic.ExogenaService.Services.Formato1019;

public record ResultadoExportacion(
    bool Exitoso,
    string? NombreArchivo,
    byte[]? ContenidoXml,
    IReadOnlyList<Formato1019ValidationError> Errores);
