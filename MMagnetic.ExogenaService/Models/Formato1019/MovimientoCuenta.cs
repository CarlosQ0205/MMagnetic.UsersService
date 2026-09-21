using System.Collections.Generic;
using System.Xml.Serialization;

namespace MMagnetic.ExogenaService.Models.Formato1019;

/// <summary>Registro "movcta" (Movimiento en Cuenta Corriente y/o Ahorro) del Formato 1019.</summary>
public class MovimientoCuenta
{
    [XmlAttribute(AttributeName = "tdoc")]
    public int Tdoc { get; set; }

    [XmlAttribute(AttributeName = "nid")]
    public string Nid { get; set; } = string.Empty;

    // XmlSerializer no admite "int?" en [XmlAttribute] (lanza InvalidOperationException
    // al reflejar el tipo); el patrón soportado es el par Valor/ValorSpecified.
    [XmlAttribute(AttributeName = "dv")]
    public int Dv { get; set; }

    [XmlIgnore]
    public bool DvSpecified { get; set; }

    [XmlAttribute(AttributeName = "apl1")]
    public string? Apl1 { get; set; }

    [XmlAttribute(AttributeName = "apl2")]
    public string? Apl2 { get; set; }

    [XmlAttribute(AttributeName = "nom1")]
    public string? Nom1 { get; set; }

    [XmlAttribute(AttributeName = "nom2")]
    public string? Nom2 { get; set; }

    [XmlAttribute(AttributeName = "raz")]
    public string? Raz { get; set; }

    [XmlAttribute(AttributeName = "dir")]
    public string? Dir { get; set; }

    // El anexo indica que dpto/mun deben incluir ceros a la izquierda (código DANE),
    // pero el propio XSD de la DIAN los tipa como xs:int (0-99 / 0-999), por lo que
    // aquí se respeta el tipo del esquema; si más adelante se genera el XML de salida,
    // hay que formatear con padding ("D2"/"D3") al serializar.
    [XmlAttribute(AttributeName = "dpto")]
    public int Dpto { get; set; }

    [XmlIgnore]
    public bool DptoSpecified { get; set; }

    [XmlAttribute(AttributeName = "mun")]
    public int Mun { get; set; }

    [XmlIgnore]
    public bool MunSpecified { get; set; }

    [XmlAttribute(AttributeName = "pais")]
    public int Pais { get; set; }

    [XmlIgnore]
    public bool PaisSpecified { get; set; }

    [XmlAttribute(AttributeName = "cta")]
    public string Cta { get; set; } = string.Empty;

    [XmlAttribute(AttributeName = "tipcta")]
    public int TipCta { get; set; }

    [XmlAttribute(AttributeName = "codex")]
    public int Codex { get; set; }

    [XmlAttribute(AttributeName = "sal")]
    public decimal Sal { get; set; }

    [XmlAttribute(AttributeName = "psaldof")]
    public decimal PSaldoF { get; set; }

    [XmlAttribute(AttributeName = "meddia")]
    public decimal MedDia { get; set; }

    [XmlAttribute(AttributeName = "smax")]
    public decimal SMax { get; set; }

    [XmlAttribute(AttributeName = "smin")]
    public decimal SMin { get; set; }

    // vcred/procre/medcre/vmovdeb/nmovdeb/pordeb están tipados xs:double en el XSD de
    // la DIAN, pero restringidos por un pattern a solo dígitos (sin signo ni decimales).
    // Se usa "decimal" en vez de "double" para evitar que .NET los serialice en
    // notación científica (ej. "1.2E+19"), lo que rompería ese pattern.
    [XmlAttribute(AttributeName = "vcred")]
    public decimal VCred { get; set; }

    [XmlAttribute(AttributeName = "movcre")]
    public decimal MovCre { get; set; }

    [XmlAttribute(AttributeName = "procre")]
    public decimal ProCre { get; set; }

    [XmlAttribute(AttributeName = "medcre")]
    public decimal MedCre { get; set; }

    [XmlAttribute(AttributeName = "vmovdeb")]
    public decimal VMovDeb { get; set; }

    [XmlAttribute(AttributeName = "nmovdeb")]
    public decimal NMovDeb { get; set; }

    [XmlAttribute(AttributeName = "pordeb")]
    public decimal PorDeb { get; set; }

    [XmlElement(ElementName = "titSec")]
    public List<TitularSecundario> TitularesSecundarios { get; set; } = new();
}
