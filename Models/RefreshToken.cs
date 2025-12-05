using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMagnetic.UsersService.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        public Guid RefreshTokenId { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime FechaExpiracion { get; set; }

        public string? IP { get; set; }

        public bool EsRevocado { get; set; }

        // Relación
        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }
    }
}
