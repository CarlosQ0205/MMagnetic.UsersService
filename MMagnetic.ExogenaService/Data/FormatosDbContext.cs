using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Models.Formatos;

namespace MMagnetic.ExogenaService.Data;

/// <summary>Acceso a la base de datos MM_Formatos (staging, errores y definitivo del Formato 1019).</summary>
public class FormatosDbContext : DbContext
{
    public FormatosDbContext(DbContextOptions<FormatosDbContext> options) : base(options)
    {
    }

    public DbSet<Formato1019Concepto> Formato1019 => Set<Formato1019Concepto>();
    public DbSet<FormatoError> FormatoErrores => Set<FormatoError>();
    public DbSet<Formato1019Definitivo> Formato1019Definitivo => Set<Formato1019Definitivo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Formato1019Concepto>(e =>
        {
            e.ToTable("Formato_1019");
            e.HasKey(f => f.Formato1019Id);
        });

        modelBuilder.Entity<FormatoError>(e =>
        {
            e.ToTable("Formato_Errores");
            e.HasKey(f => f.ErrorId);
        });

        modelBuilder.Entity<Formato1019Definitivo>(e =>
        {
            e.ToTable("F_1019_Definitivo");
            e.HasKey(f => f.Formato1019DefinitivoId);
        });
    }
}
