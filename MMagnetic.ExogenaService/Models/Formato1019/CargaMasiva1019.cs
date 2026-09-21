using System.Collections.Generic;
using System.Xml.Serialization;

namespace MMagnetic.ExogenaService.Models.Formato1019;

[XmlRoot(ElementName = "mas")]
public class CargaMasiva1019
{
    [XmlElement(ElementName = "Cab")]
    public CabeceraFormato1019 Cab { get; set; } = new();

    [XmlElement(ElementName = "movcta")]
    public List<MovimientoCuenta> Movimientos { get; set; } = new();
}
