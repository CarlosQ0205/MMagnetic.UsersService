using System;
using System.ComponentModel.DataAnnotations;

namespace MMagnetic.UsersService.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid RefreshTokenId { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        public string Token { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public string IP { get; set; }

        public bool EsRevocado { get; set; }

        // Relación con Usuario
        public Usuario Usuario { get; set; }
    }
}
