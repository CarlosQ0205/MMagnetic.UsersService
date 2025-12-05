using System;
using System.ComponentModel.DataAnnotations;

namespace MMagnetic.UsersService.Models
{
    public class UsuarioRol
    {
        [Key]
        public Guid UsuarioRolId { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        [Required]
        public Guid RolId { get; set; }

        public DateTime FechaAsignacion { get; set; }

        // Relaciones (nullable para evitar advertencias del compilador)
        public Usuario? Usuario { get; set; }
        public Rol? Rol { get; set; }
    }
}
