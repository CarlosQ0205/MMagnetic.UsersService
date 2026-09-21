namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>
/// Estándar del nombre de archivo del Anexo 2 (sección 1):
/// Dmuisca_ccmmmmmvvaaaacccccccc.xml
///   cc       : Concepto (01 = Inserción, 02 = Reemplazo)
///   mmmmm    : Formato (Movimiento en cuenta corriente y/o ahorro = 01019)
///   vv       : Versión del formato (09)
///   aaaa     : Año de envío
///   cccccccc : Consecutivo de envío por año
/// </summary>
public static class Formato1019FileNaming
{
    private const string CodigoFormato = "01019";
    private const string CodigoVersion = "09";

    public static string ConstruirNombreArchivo(int codCpt, int periodoAno, int numEnvio)
        => $"Dmuisca_{codCpt:D2}{CodigoFormato}{CodigoVersion}{periodoAno:D4}{numEnvio:D8}.xml";
}
