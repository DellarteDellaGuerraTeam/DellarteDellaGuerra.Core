using System.Xml.Linq;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils
{
    public interface IXmlProcessor
    {
        /**
         * <summary>
         *    Returns the xml nodes after the merge of all xml files having rootElementName as root element
         *    and applying the associated xsl files.
         * </summary>
         * <param name="rootElementName">The root element name of the xml node.</param>
         * <returns>The xml node after processing all of the modules xml and xsl files.</returns>
         */
        XNode GetXmlNodes(string rootElementName);
    }
}
