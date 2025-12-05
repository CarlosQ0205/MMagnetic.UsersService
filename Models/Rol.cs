using System;
using System.ComponentModel.DataAnnotations;

namespace MMagnetic.UsersService.Models
{
    public class Rol
    {
        [Key]
        public Guid RolId { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Nombre { get; set; }  // ← puede ser nullable durante instanciación

        [MaxLength(400)]
        public string? Descripcion { get; set; } // ← también puede ser null

        public DateTime FechaCreacion { get; set; }

        public bool EsActivo { get; set; }
    }
}
