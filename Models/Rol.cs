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
        public string Nombre { get; set; }

        [MaxLength(400)]
        public string Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool EsActivo { get; set; }
    }
}
