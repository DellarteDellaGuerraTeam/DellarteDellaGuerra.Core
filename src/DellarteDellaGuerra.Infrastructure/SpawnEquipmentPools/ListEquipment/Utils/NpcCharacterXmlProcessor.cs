using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Logging.Port;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Utils
{
    public class NpcCharacterXmlProcessor : INpcCharacterXmlProcessor
    {
        private readonly ILogger _logger;

        public NpcCharacterXmlProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NpcCharacterXmlProcessor>();
        }
        
        private const string NpcCharacterRootTag = "NPCCharacters";

        public XNode GetMergedXmlCharacterNodes()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var characterDocument = MBObjectManager.GetMergedXmlForManaged(NpcCharacterRootTag, true);
                sw.Stop();
                _logger.Debug($"GetMergedXmlForManaged took: {sw.ElapsedMilliseconds}ms");
                return XDocument.Parse(characterDocument.OuterXml);
            }
            catch (IOException e)
            {
                _logger.Error($"Failed to get merged XML character nodes: {e}");
                return new XDocument();
            }
        }
    }
}