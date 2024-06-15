using System.Xml.Linq;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Utils
{
    public interface INpcCharacterXmlProcessor
    {
        /**
         * <summary>
         *    Returns the character xml nodes after the merge of all mod characters via xml and xsl.
         * </summary>
         * <returns>The character xml node after processing all of the modules xml and xsl files.</returns>
         * <exception cref="ArgumentException">Thrown when the root node name is invalid.</exception>
         * <exception cref="IOException">Thrown when the file is not found or can not be read.</exception>
         */
        XNode GetMergedXmlCharacterNodes();
    }
}
