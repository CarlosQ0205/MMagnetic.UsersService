using System.ComponentModel.DataAnnotations.Schema;

namespace MMagnetic.UsersService.Models
{
    [Table("Usuarios")]
    public class User : BaseEntity
    {
        [Column("NombreUsuario")]
        public string NombreUsuario { get; set; }

        [Column("Contraseña")]
        public string Contraseña { get; set; }

        [Column("Correo")]
        public string Correo { get; set; }
    }
}