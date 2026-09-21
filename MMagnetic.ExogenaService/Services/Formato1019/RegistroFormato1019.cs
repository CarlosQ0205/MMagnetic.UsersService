using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>Un "movcta" ya ensamblado, junto con el contexto de origen (Cliente/cuenta) y los errores de homologación DIAN encontrados al armarlo.</summary>
public record RegistroFormato1019(
    Guid ClienteId,
    Guid DatoFinancieroId,
    string NumeroCuenta,
    int PeriodoAno,
    MovimientoCuenta Movimiento,
    IReadOnlyList<string> ErroresHomologacion);
