namespace MMagnetic.ExogenaService.Models.Formatos;

/// <summary>
/// Entidad "dbo.F_1019_Definitivo" (MM_Formatos). Misma forma que Formato1019Concepto;
/// contiene solo los conceptos que pasaron la validación, listos para exportar.
/// </summary>
public class Formato1019Definitivo
{
    public Guid Formato1019DefinitivoId { get; set; }
    public Guid? Formato1019Id { get; set; }
    public Guid? ClienteId { get; set; }
    public int PeriodoAno { get; set; }
    public byte? PeriodoMes { get; set; }
    public string CodigoConcepto { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Descripcion { get; set; }
    public string? Linea { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public Guid? UsuarioGenerador { get; set; }
}
