namespace MMagnetic.ExogenaService.Models.Dto;

/// <summary>
/// DTO de entrada para crear/actualizar un Cotitular. El cliente se identifica por su
/// número de documento (no por ClienteId, que es un Guid interno que nadie llenando un
/// Excel va a conocer).
/// </summary>
public class CotitularDto
{
    public string NumeroDocumentoCliente { get; set; } = string.Empty;
    public string? TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal? Participacion { get; set; }
    public string? RelacionConCliente { get; set; }
}
