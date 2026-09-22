using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>
/// Descompone un "movcta" en sus conceptos numéricos individuales (un valor por concepto),
/// usado tanto para persistir el modelo EAV de Formato_1019/F_1019_Definitivo como para
/// generar el Excel plano de esas mismas tablas.
/// </summary>
internal static class Formato1019ConceptoExtractor
{
    public static IEnumerable<(string Codigo, decimal Valor)> ObtenerConceptos(MovimientoCuenta mov)
    {
        yield return ("tdoc", mov.Tdoc);
        if (mov.DvSpecified) yield return ("dv", mov.Dv);
        if (mov.DptoSpecified) yield return ("dpto", mov.Dpto);
        if (mov.MunSpecified) yield return ("mun", mov.Mun);
        if (mov.PaisSpecified) yield return ("pais", mov.Pais);
        yield return ("tipcta", mov.TipCta);
        yield return ("codex", mov.Codex);
        yield return ("sal", mov.Sal);
        yield return ("psaldof", mov.PSaldoF);
        yield return ("meddia", mov.MedDia);
        yield return ("smax", mov.SMax);
        yield return ("smin", mov.SMin);
        yield return ("vcred", mov.VCred);
        yield return ("movcre", mov.MovCre);
        yield return ("procre", mov.ProCre);
        yield return ("medcre", mov.MedCre);
        yield return ("vmovdeb", mov.VMovDeb);
        yield return ("nmovdeb", mov.NMovDeb);
        yield return ("pordeb", mov.PorDeb);
    }
}
