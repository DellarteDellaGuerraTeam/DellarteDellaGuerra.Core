using System.Xml.Serialization;

namespace DellarteDellaGuerra.Configuration.Models;

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
}