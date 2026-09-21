namespace MMagnetic.ExogenaService.Models.Formatos;

/// <summary>Fila de Formato_Errores enriquecida con el número de documento del cliente, para mostrarla en pantalla sin que el front tenga que cruzar bases de datos.</summary>
public record ErrorFormato1019Dto(
    Guid ErrorId,
    Guid? ClienteId,
    string? NumeroDocumentoCliente,
    string CodigoError,
    string? DescripcionError,
    string? ValorInvalido,
    string? Nivel,
    DateTime FechaError);
