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
         * Gets or sets the skill minimums required to participate in a jousting tournament.
         * </summary>
         */
        [XmlElement(ElementName = "JoustingConfig")]
        public JoustingConfig JoustingConfig { get; set; } = new JoustingConfig();

        /**
         * <summary>
         * Gets or sets the balance values for the church feature.
         * </summary>
         */
        [XmlElement(ElementName = "ChurchConfig")]
        public ChurchConfig ChurchConfig { get; set; } = new ChurchConfig();
    }
}
