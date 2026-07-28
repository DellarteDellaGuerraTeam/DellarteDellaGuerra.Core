using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Models
{
    /**
     * <summary>
     *  The configuration for the mod.
     * </summary>
     */
    [XmlRoot(ElementName="DadgConfiguration")]
    public class DadgConfig
    {
        /**
         * <summary>
         * Gets or sets a value indicating whether the shader compilation notifications are enabled.
         * </summary>
         */
        [XmlElement(ElementName = "EnableShaderCompilationNotifications")]
        public bool EnableShaderCompilationNotifications { get; set; } = true;

        /**
         * <summary>
         * Gets or sets a value indicating whether women can lead parties.
         * </summary>
         */
        [XmlElement(ElementName = "EnableFemalePartyLeaders")]
        public bool EnableFemalePartyLeaders { get; set; }

        /**
         * <summary>
         * Gets or sets the skill minimums required to participate in a jousting tournament.
         * </summary>
         */
        [XmlElement(ElementName = "JoustingConfig")]
        public JoustingConfig JoustingConfig { get; set; } = new JoustingConfig();
    }
}
