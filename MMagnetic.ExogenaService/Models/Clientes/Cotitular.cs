namespace MMagnetic.ExogenaService.Models.Clientes;

/// <summary>
/// Entidad "dbo.Cotitulares" (MM_Clientes). Está a nivel Cliente (no por cuenta):
/// si un cliente reporta varias cuentas, todos sus cotitulares se replican como
/// "titSec" en cada una de ellas al ensamblar el Formato 1019.
/// </summary>
public class Cotitular
{
    public Guid CotitularId { get; set; }
    public Guid ClienteId { get; set; }
    public string? TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal? Participacion { get; set; }
    public string? RelacionConCliente { get; set; }
    public DateTime FechaRegistro { get; set; }
}
