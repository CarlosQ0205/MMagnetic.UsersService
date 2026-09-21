namespace MMagnetic.ExogenaService.Models.Dto;

/// <summary>
/// DTO de entrada para crear/actualizar un Cliente, tanto desde el frontend (formulario)
/// como desde la carga masiva (fila de Excel/CSV). País/Departamento/Municipio se reciben
/// como el código DIAN (ej. "05", "001", "169"), no como el Id interno de la base — así
/// puede diligenciarlos alguien que solo conoce los códigos oficiales.
/// </summary>
public class ClienteDto
{
    public string? TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? PrimerNombre { get; set; }
    public string? SegundoNombre { get; set; }
    public string? PrimerApellido { get; set; }
    public string? SegundoApellido { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? CodigoPais { get; set; }
    public string? CodigoDepartamento { get; set; }
    public string? CodigoMunicipio { get; set; }
    public int? DigitoVerificacion { get; set; }
}
