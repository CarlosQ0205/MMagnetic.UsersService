using System;
using System.Xml.Serialization;

namespace MMagnetic.ExogenaService.Models.Formato1019;

/// <summary>Encabezado "Cab" del Formato 1019 (tipo "CabType" en el XSD de la DIAN).</summary>
[XmlType(TypeName = "CabType")]
public class CabeceraFormato1019
{
    [XmlElement(ElementName = "Ano")]
    public int Ano { get; set; }

    [XmlElement(ElementName = "CodCpt")]
    public int CodCpt { get; set; }

    [XmlElement(ElementName = "Formato")]
    public int Formato { get; set; } = 1019;

    [XmlElement(ElementName = "Version")]
    public int Version { get; set; } = 9;

    [XmlElement(ElementName = "NumEnvio")]
    public int NumEnvio { get; set; }

    // xs:dateTime exige el formato exacto AAAA-MM-DDTHH:MM:SS; el serializador de .NET
    // por defecto agrega fracciones de segundo y offset de zona horaria, por eso se
    // expone como texto controlado en FecEnvioXml en vez de dejar que XmlSerializer
    // formatee el DateTime directamente.
    [XmlIgnore]
    public DateTime FecEnvio { get; set; }

    [XmlElement(ElementName = "FecEnvio")]
    public string FecEnvioXml
    {
        get => FecEnvio.ToString("yyyy-MM-ddTHH:mm:ss");
        set => FecEnvio = DateTime.Parse(value);
    }

    [XmlIgnore]
    public DateTime FecInicial { get; set; }

    [XmlElement(ElementName = "FecInicial")]
    public string FecInicialXml
    {
        get => FecInicial.ToString("yyyy-MM-dd");
        set => FecInicial = DateTime.Parse(value);
    }

    [XmlIgnore]
    public DateTime FecFinal { get; set; }

    [XmlElement(ElementName = "FecFinal")]
    public string FecFinalXml
    {
        get => FecFinal.ToString("yyyy-MM-dd");
        set => FecFinal = DateTime.Parse(value);
    }

    // Según el Anexo 2 (Res. 000162/2023), ValorTotal corresponde a la sumatoria del
    // atributo "codex" de todos los "movcta" del archivo (no a un valor monetario).
    [XmlElement(ElementName = "ValorTotal")]
    public decimal ValorTotal { get; set; }

    [XmlElement(ElementName = "CantReg")]
    public int CantReg { get; set; }
}
