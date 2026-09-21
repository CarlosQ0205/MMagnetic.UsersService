namespace MMagnetic.ExogenaService.Models.Dian;

/// <summary>Entidad "dbo.DIAN_Departamentos" (MM_DIAN, catálogo de solo lectura).</summary>
public class DianDepartamento
{
    public int DepartamentoId { get; set; }
    public int PaisId { get; set; }
    public string? CodigoDepartamento { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;
}
