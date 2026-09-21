namespace MMagnetic.ExogenaService.Models.Dian;

/// <summary>Entidad "dbo.DIAN_Paises" (MM_DIAN, catálogo de solo lectura).</summary>
public class DianPais
{
    public int PaisId { get; set; }
    public string? CodigoPais { get; set; }
    public string NombrePais { get; set; } = string.Empty;
}
