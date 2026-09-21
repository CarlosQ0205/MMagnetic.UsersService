using System.Xml.Serialization;

namespace MMagnetic.ExogenaService.Models.Formato1019;

/// <summary>Elemento "titSec": titular secundario y/o firma autorizada de una cuenta.</summary>
public class TitularSecundario
{
    [XmlAttribute(AttributeName = "cpts")]
    public int Cpts { get; set; }

    [XmlAttribute(AttributeName = "tdocs")]
    public int Tdocs { get; set; }

    [XmlAttribute(AttributeName = "nids")]
    public string Nids { get; set; } = string.Empty;

    [XmlAttribute(AttributeName = "dvs")]
    public int Dvs { get; set; }

    [XmlIgnore]
    public bool DvsSpecified { get; set; }

    [XmlAttribute(AttributeName = "apl1s")]
    public string? Apl1s { get; set; }

    [XmlAttribute(AttributeName = "apl2s")]
    public string? Apl2s { get; set; }

    [XmlAttribute(AttributeName = "nom1s")]
    public string? Nom1s { get; set; }

    [XmlAttribute(AttributeName = "nom2s")]
    public string? Nom2s { get; set; }

    [XmlAttribute(AttributeName = "razs")]
    public string? Razs { get; set; }
}
