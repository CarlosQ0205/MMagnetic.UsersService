using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Models.Dian;

namespace MMagnetic.ExogenaService.Data;

/// <summary>Acceso de solo lectura al catálogo DIAN (MM_DIAN: países, departamentos, municipios).</summary>
public class DianDbContext : DbContext
{
    public DianDbContext(DbContextOptions<DianDbContext> options) : base(options)
    {
    }

    public DbSet<DianPais> Paises => Set<DianPais>();
    public DbSet<DianDepartamento> Departamentos => Set<DianDepartamento>();
    public DbSet<DianMunicipio> Municipios => Set<DianMunicipio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DianPais>().ToTable("DIAN_Paises").HasKey(p => p.PaisId);
        modelBuilder.Entity<DianDepartamento>().ToTable("DIAN_Departamentos").HasKey(d => d.DepartamentoId);
        modelBuilder.Entity<DianMunicipio>().ToTable("DIAN_Municipios").HasKey(m => m.MunicipioId);
    }
}
