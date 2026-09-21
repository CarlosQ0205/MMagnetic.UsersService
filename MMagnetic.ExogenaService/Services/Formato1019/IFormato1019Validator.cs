using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public interface IFormato1019Validator
{
    /// <summary>Valida un archivo completo (encabezado + todos los movcta) contra las reglas del Anexo 2.</summary>
    Formato1019ValidationResult Validar(CargaMasiva1019 carga);

    /// <summary>
    /// Valida un único registro "movcta" de forma aislada (sin las reglas de encabezado
    /// ni la llave única entre registros, que solo tienen sentido a nivel de lote/envío).
    /// Pensado para el pipeline de base de datos, que clasifica cuenta por cuenta.
    /// </summary>
    Formato1019ValidationResult ValidarRegistro(MovimientoCuenta movimiento);
}
