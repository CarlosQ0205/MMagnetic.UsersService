namespace MMagnetic.ExogenaService.Models.Dto;

/// <summary>
/// DTO de entrada para crear/actualizar un Dato_Financiero (una cuenta-período). El
/// cliente se identifica por número de documento, igual que en CotitularDto.
/// </summary>
public class DatoFinancieroDto
{
    public string NumeroDocumentoCliente { get; set; } = string.Empty;
    public string? TipoProducto { get; set; }
    public string? Entidad { get; set; }
    public string? Observaciones { get; set; }

    public decimal? Saldo { get; set; }
    public decimal? IngresosAnuales { get; set; }
    public decimal? EgresosAnuales { get; set; }
    public decimal? Activos { get; set; }
    public decimal? Pasivos { get; set; }
    public decimal? Patrimonio { get; set; }

    // Campos del Formato 1019 (Migración 001).
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
