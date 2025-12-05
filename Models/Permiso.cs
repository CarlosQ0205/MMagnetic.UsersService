using System;
using System.ComponentModel.DataAnnotations;

namespace MMagnetic.UsersService.Models
{
    public class Permiso
    {
        [Key]
        public Guid PermisoId { get; set; }

        [Required]
        [MaxLength(150)]
        public string? Nombre { get; set; }   // ← ahora nullable

        [MaxLength(400)]
        public string? Descripcion { get; set; }  // ← también nullable

        public DateTime FechaCreacion { get; set; }
    }
}
