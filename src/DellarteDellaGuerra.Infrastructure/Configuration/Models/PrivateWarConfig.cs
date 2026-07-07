using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Models
{
    public class PrivateWarConfig
    {
        // ARGB hex string for nameplates belonging to a private-war enemy of the player.
        // Null when absent from dadg.config.xml; the domain use case then applies the default
        // (vivid orange) and all validation. Override for colorblind accessibility.
        [XmlElement(ElementName = "PrivateWarEnemyNameplateColorArgb")]
        public string? PrivateWarEnemyNameplateColorArgb { get; set; }

        // ARGB hex string for nameplates belonging to a private-war ally of the player.
        // Null when absent from dadg.config.xml; the domain use case then applies the default
        // (BlueViolet) and all validation. Override for colorblind accessibility.
        [XmlElement(ElementName = "PrivateWarAllyNameplateColorArgb")]
        public string? PrivateWarAllyNameplateColorArgb { get; set; }
    }
}
