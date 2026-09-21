namespace MMagnetic.ExogenaService.Models.Dian;

/// <summary>Entidad "dbo.DIAN_Municipios" (MM_DIAN, catálogo de solo lectura).</summary>
public class DianMunicipio
{
    public int MunicipioId { get; set; }
    public int DepartamentoId { get; set; }
    public int PaisId { get; set; }
    public string? CodigoMunicipio { get; set; }
    public string NombreMunicipio { get; set; } = string.Empty;
}
