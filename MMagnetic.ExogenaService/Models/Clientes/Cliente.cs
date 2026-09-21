namespace MMagnetic.ExogenaService.Models.Clientes;

/// <summary>Entidad "dbo.Clientes" de la base de datos MM_Clientes.</summary>
public class Cliente
{
    public Guid ClienteId { get; set; }
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
    public int? PaisId { get; set; }
    public int? DepartamentoId { get; set; }
    public int? MunicipioId { get; set; }

    // Agregado en la Migración 001: dv (Dígito de Verificación) del Formato 1019
    // pertenece al NIT del titular, no a la cuenta, por eso vive en Cliente.
    public byte? DigitoVerificacion { get; set; }

    public DateTime FechaRegistro { get; set; }
    public bool EsActivo { get; set; }

    public List<Cotitular> Cotitulares { get; set; } = new();
    public List<DatoFinanciero> DatosFinancieros { get; set; } = new();
}
