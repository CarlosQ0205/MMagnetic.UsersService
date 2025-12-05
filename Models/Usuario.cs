using System;
using System.Collections.Generic;

namespace MMagnetic.UsersService.Models
{
    public class Usuario
    {
        public Guid UsuarioId { get; set; }  // PK

        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }

        public string? CorreoElectronico { get; set; }

        public string? PasswordHash { get; set; }
        public string? Salt { get; set; }

        public string? Telefono { get; set; }

        public bool EsActivo { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }

        // Relaciones
        public ICollection<UsuarioRol>? UsuariosRoles { get; set; }
    }
}
