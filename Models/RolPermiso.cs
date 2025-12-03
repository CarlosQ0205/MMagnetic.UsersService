using System;
using System.ComponentModel.DataAnnotations;

namespace MMagnetic.UsersService.Models
{
    public class RolPermiso
    {
        [Key]
        public Guid RolPermisoId { get; set; }

        [Required]
        public Guid RolId { get; set; }

        [Required]
        public Guid PermisoId { get; set; }

        public DateTime FechaAsignacion { get; set; }

        // Relaciones
        public Rol Rol { get; set; }
        public Permiso Permiso { get; set; }
    }
}
