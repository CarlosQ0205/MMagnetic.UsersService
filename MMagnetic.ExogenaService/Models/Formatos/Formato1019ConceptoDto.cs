namespace MMagnetic.ExogenaService.Models.Formatos;

/// <summary>Fila de Formato_1019 (staging) enriquecida con el número de documento del cliente.</summary>
public record Formato1019ConceptoDto(
    Guid Formato1019Id,
    Guid? ClienteId,
    string? NumeroDocumentoCliente,
    int PeriodoAno,
    string CodigoConcepto,
    decimal Valor,
    string? Linea,
    DateTime FechaGeneracion);
