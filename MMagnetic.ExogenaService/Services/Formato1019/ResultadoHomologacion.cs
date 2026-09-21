namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>
/// Resultado de traducir los Id internos de Cliente (PaisId/DepartamentoId/MunicipioId)
/// a los códigos numéricos DIAN reales (CodigoPais/CodigoDepartamento/CodigoMunicipio)
/// que exige el atributo "pais"/"dpto"/"mun" de movcta.
/// </summary>
public class ResultadoHomologacion
{
    public int? CodigoPais { get; set; }
    public int? CodigoDepartamento { get; set; }
    public int? CodigoMunicipio { get; set; }

    private readonly List<string> _errores = new();
    public IReadOnlyList<string> Errores => _errores;
    public bool EsValido => _errores.Count == 0;

    public void AgregarError(string mensaje) => _errores.Add(mensaje);
}
