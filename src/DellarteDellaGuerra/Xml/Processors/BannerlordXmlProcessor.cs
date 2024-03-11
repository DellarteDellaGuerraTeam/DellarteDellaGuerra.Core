using System.Xml.Linq;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Xml.Processors
{
    public class BannerlordXmlProcessor : IXmlProcessor
    {
        private readonly MBObjectManager _objectManager;
        
        public BannerlordXmlProcessor(MBObjectManager objectManager)
        {
            _objectManager = objectManager;
        }
        
        public XNode GetMergedXmlCharacterNodes()
        {
            MBObjectManager.Instance.
        }
    }
}