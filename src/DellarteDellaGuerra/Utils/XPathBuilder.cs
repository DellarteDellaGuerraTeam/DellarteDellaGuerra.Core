using System.Linq;
using System.Text;

namespace DellarteDellaGuerra.Utils;

/**
 * <summary>
 * This class provides a way to build an XPath.
 * </summary>
 */
public class XPathBuilder
{
    private readonly StringBuilder _xpath = new ();

    /**
     * <summary>
     * This method adds the root element to the XPath.
     * If the XPath is empty and the element name is not empty, then the element name is added as the root element.
     * </summary>
     * <param name="elementName">The root element name</param>
     * <returns>The XPathBuilder instance.</returns>
     */
    public XPathBuilder WithRoot(string elementName)
    {
        if (_xpath.Length == 0 && !string.IsNullOrWhiteSpace(elementName))
        {
            _xpath.Append($"{elementName}");
        }
        return this;
    }

    /**
     * <summary>
     * This method adds a child node to the XPath.
     * This basically prefixes the descendant name with "/".
     * </summary>
     * <param name="elementName">The child node name</param>
     * <returns>The XPathBuilder instance.</returns>
     */
    public XPathBuilder WithChildNode(string elementName)
    {
        if (string.IsNullOrWhiteSpace(elementName)) return this;

        _xpath.Append($"/{elementName}");
        return this;
    }

    /**
     * <summary>
     * This method adds child nodes to the XPath.
     * It means that the the xpath will get the given elements at the same node level.
     * It prefixes the element name with "/".
     * </summary>
     */
    public XPathBuilder WithChildNodes(params string[] elementNames)
    {
        var validElementNames = elementNames.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if(validElementNames.Count == 0) return this;

        _xpath.Append("/*[");
        for (int i = 0; i < elementNames.Length; i++)
        {
            _xpath.Append($"name()='{elementNames[i]}'");
            if (i < elementNames.Length - 1)
            {
                _xpath.Append(" or ");
            }
        }
        _xpath.Append("]");
        return this;
    }

    /**
     * <summary>
     * This method adds an attribute to the XPath.
     * </summary>
     * <param name="attributeName">The attribute name</param>
     * <param name="attributeValue">The attribute value</param>
     * <returns>The XPathBuilder instance.</returns>
     * <remarks>
     * If the attribute name is empty, then the method just returns the current instance.
     * </remarks>
     */
    public XPathBuilder WithAttribute(string attributeName, string attributeValue)
    {
        if (string.IsNullOrEmpty(attributeName)) return this;

        _xpath.Append($"[@{attributeName}='{attributeValue ?? ""}']");
        return this;
    }

    /**
     * <summary>
     * This method adds a descendant to the XPath.
     * This basically prefixes the descendant name with "//".
     * </summary>
     * <param name="descendant">The descendant name</param>
     * <returns>The XPathBuilder instance.</returns>
     * <remarks>
     * If the descendant name is empty, then the method just returns the current instance.
     * </remarks>
     */
    public XPathBuilder WithDescendant(string descendant)
    {
        if (string.IsNullOrWhiteSpace(descendant)) return this;

        _xpath.Append($"//{descendant}");
        return this;
    }

    public string Build()
    {
        return _xpath.ToString();
    }
}
