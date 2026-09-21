namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>
/// Mapea la abreviatura de tipo de documento usada en Clientes/Cotitulares (ej. "CC", "NIT")
/// al código numérico DIAN que exige el atributo "tdoc"/"tdocs" de movcta/titSec.
///
/// IMPORTANTE: este catálogo NO viene del Anexo No. 2 (Formato 1019) ni de las tablas de
/// MM_DIAN que existen hoy (solo tienen país/departamento/municipio, no tipos de documento).
/// Es el catálogo general de tipos de documento que la DIAN usa en sus formatos de
/// información exógena, pero antes de usarlo en producción hay que confirmarlo contra el
/// anexo oficial correspondiente (o crear una tabla DIAN_TiposDocumento en MM_DIAN).
/// </summary>
public static class CatalogoTiposDocumento
{
    private static readonly Dictionary<string, int> Mapa = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RC"] = 11,   // Registro civil
        ["TI"] = 12,   // Tarjeta de identidad
        ["CC"] = 13,   // Cédula de ciudadanía
        ["TE"] = 21,   // Tarjeta de extranjería
        ["CE"] = 22,   // Cédula de extranjería
        ["NIT"] = 31,  // NIT
        ["PA"] = 41,   // Pasaporte
        ["DIE"] = 42,  // Documento de identificación extranjero
        ["PEP"] = 47,  // Permiso especial de permanencia
        ["PPT"] = 48,  // Permiso por protección temporal
        ["NITE"] = 50, // NIT de otro país
        ["NUIP"] = 91, // NUIP
    };

    /// <summary>Devuelve el código DIAN, o null si la abreviatura no está en el catálogo (no lanza excepción).</summary>
    public static int? Mapear(string? tipoDocumento)
        => !string.IsNullOrWhiteSpace(tipoDocumento) && Mapa.TryGetValue(tipoDocumento.Trim(), out var codigo)
            ? codigo
            : null;
}
