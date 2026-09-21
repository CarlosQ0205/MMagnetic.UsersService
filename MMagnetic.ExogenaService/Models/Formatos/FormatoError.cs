namespace MMagnetic.ExogenaService.Models.Formatos;

/// <summary>Entidad "dbo.Formato_Errores" (MM_Formatos). Editable: el usuario corrige el dato de origen y se vuelve a validar.</summary>
public class FormatoError
{
    public Guid ErrorId { get; set; }
    public Guid? Formato1019Id { get; set; }
    public Guid? ClienteId { get; set; }
    public string CodigoError { get; set; } = string.Empty;
    public string? DescripcionError { get; set; }
    public string? ValorInvalido { get; set; }
    public string? Nivel { get; set; }
    public DateTime FechaError { get; set; }
    public Guid? UsuarioProcesador { get; set; }
}
