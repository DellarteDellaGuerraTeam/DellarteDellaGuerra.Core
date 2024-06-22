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
         */
        XNode GetMergedXmlCharacterNodes();
    }
}
