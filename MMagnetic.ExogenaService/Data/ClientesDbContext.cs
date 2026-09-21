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
            e.Property(c => c.Participacion).HasPrecision(5, 2); // decimal(5,2) en la tabla
        });

        modelBuilder.Entity<DatoFinanciero>(e =>
        {
            e.ToTable("Datos_Financieros");
            e.HasKey(d => d.DatoFinancieroId);

            // Precisión exacta según la definición original de la tabla y la Migración 001.
            e.Property(d => d.Saldo).HasPrecision(18, 2);
            e.Property(d => d.IngresosAnuales).HasPrecision(18, 2);
            e.Property(d => d.EgresosAnuales).HasPrecision(18, 2);
            e.Property(d => d.Activos).HasPrecision(18, 2);
            e.Property(d => d.Pasivos).HasPrecision(18, 2);
            e.Property(d => d.Patrimonio).HasPrecision(18, 2);
            e.Property(d => d.PromedioSaldoFinal).HasPrecision(29, 2);
            e.Property(d => d.MedianaSaldoDiario).HasPrecision(29, 2);
            e.Property(d => d.SaldoMaximo).HasPrecision(29, 2);
            e.Property(d => d.SaldoMinimo).HasPrecision(29, 2);
            e.Property(d => d.ValorMovCredito).HasPrecision(20, 0);
            e.Property(d => d.PromedioMovCredito).HasPrecision(20, 0);
            e.Property(d => d.MedianaMovCredito).HasPrecision(20, 0);
            e.Property(d => d.ValorMovDebito).HasPrecision(20, 0);
            e.Property(d => d.PromedioMovDebito).HasPrecision(20, 0);
        });
    }
}
