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
         * Gets or sets a value indicating whether field battle cannon placement during deployment is enabled.
         * </summary>
         */
        [XmlElement(ElementName = "EnableFieldBattleCannonPlacement")]
        public bool EnableFieldBattleCannonPlacement { get; set; } = true;

        /**
         * <summary>
         * Gets or sets the maximum number of cannons the player can place during field battle deployment.
         * </summary>
         */
        [XmlElement(ElementName = "FieldBattleCannonPlacementLimit")]
        public int FieldBattleCannonPlacementLimit { get; set; } = 2;
    }
}
