using Microsoft.EntityFrameworkCore;
using MMagnetic.UsersService.Models;

namespace MMagnetic.UsersService.Data
{
    public class UsersDbContext : DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<UsuarioRol> UsuariosRoles { get; set; }
        public DbSet<RolPermiso> RolesPermisos { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Necesario para relaciones N:N
            modelBuilder.Entity<UsuarioRol>()
                .HasKey(ur => new { ur.UsuarioId, ur.RolId });

            modelBuilder.Entity<RolPermiso>()
                .HasKey(rp => new { rp.RolId, rp.PermisoId });

            base.OnModelCreating(modelBuilder);
        }
    }
}
