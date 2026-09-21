namespace MMagnetic.ExogenaService.Models.Formatos;

/// <summary>
/// Entidad "dbo.Formato_1019" (MM_Formatos). Es un modelo concepto/valor: cada fila
/// representa UN atributo numérico de un "movcta" (ej. CodigoConcepto="sal",
/// Valor=1500000). "Linea" identifica la cuenta (NumeroCuenta) a la que pertenece,
/// para poder reconstruir el registro completo agrupando por
/// (ClienteId, PeriodoAno, Linea). Los atributos de texto (nid, cta, apl1, raz, dir...)
/// no se duplican aquí: se leen por join a Cliente/DatoFinanciero vía ClienteId/Linea.
/// </summary>
public class Formato1019Concepto
{
    public Guid Formato1019Id { get; set; }
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
