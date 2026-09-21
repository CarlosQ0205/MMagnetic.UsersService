namespace MMagnetic.ExogenaService.Models.Clientes;

/// <summary>
/// Entidad "dbo.Datos_Financieros" (MM_Clientes). Las columnas heredadas (Saldo,
/// IngresosAnuales, etc.) sirven a otros propósitos; las agregadas en la
/// Migración 001 (NumeroCuenta en adelante) son las que representan un "movcta"
/// del Formato 1019 — un registro por cuenta y período (PeriodoAno).
/// </summary>
public class DatoFinanciero
{
    public Guid DatoFinancieroId { get; set; }
    public Guid ClienteId { get; set; }
    public string? TipoProducto { get; set; }
    public string? Entidad { get; set; }
    public decimal? Saldo { get; set; }
    public decimal? IngresosAnuales { get; set; }
    public decimal? EgresosAnuales { get; set; }
    public decimal? Activos { get; set; }
    public decimal? Pasivos { get; set; }
    public decimal? Patrimonio { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; }

    // --- Campos agregados en la Migración 001 para el Formato 1019 ---
    public int? PeriodoAno { get; set; }
    public string? NumeroCuenta { get; set; }
    public byte? TipoCuenta { get; set; }
    public byte? CodigoExencion { get; set; }
    public decimal? PromedioSaldoFinal { get; set; }
    public decimal? MedianaSaldoDiario { get; set; }
    public decimal? SaldoMaximo { get; set; }
    public decimal? SaldoMinimo { get; set; }
    public decimal? ValorMovCredito { get; set; }
    public int? NumMovCredito { get; set; }
    public decimal? PromedioMovCredito { get; set; }
    public decimal? MedianaMovCredito { get; set; }
    public decimal? ValorMovDebito { get; set; }
    public int? NumMovDebito { get; set; }
    public decimal? PromedioMovDebito { get; set; }
}
