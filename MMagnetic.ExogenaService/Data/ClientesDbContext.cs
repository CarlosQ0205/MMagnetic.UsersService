using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Models.Clientes;

namespace MMagnetic.ExogenaService.Data;

/// <summary>Acceso a la base de datos MM_Clientes (Clientes, Cotitulares, Datos_Financieros).</summary>
public class ClientesDbContext : DbContext
{
    public ClientesDbContext(DbContextOptions<ClientesDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Cotitular> Cotitulares => Set<Cotitular>();
    public DbSet<DatoFinanciero> DatosFinancieros => Set<DatoFinanciero>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.HasKey(c => c.ClienteId);
            e.HasMany(c => c.Cotitulares).WithOne().HasForeignKey(c => c.ClienteId);
            e.HasMany(c => c.DatosFinancieros).WithOne().HasForeignKey(d => d.ClienteId);
        });

        modelBuilder.Entity<Cotitular>(e =>
        {
            e.ToTable("Cotitulares");
            e.HasKey(c => c.CotitularId);
        });

        modelBuilder.Entity<DatoFinanciero>(e =>
        {
            e.ToTable("Datos_Financieros");
            e.HasKey(d => d.DatoFinancieroId);
        });
    }
}
