using System.Xml.Serialization;

namespace DellarteDellaGuerra.Configuration.Models
{

    [XmlRoot(ElementName = "DadgConfiguration")]
    public class DadgConfig
    {
        [XmlElement(ElementName = "EnableShaderCompilationNotifications")]
        public bool EnableShaderCompilationNotifications { get; set; } = true;
    }
}