using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Visor;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Visor
{
    /// <summary>
    /// Resolves the helmet item that toggles a visor, or puts it in a requested state, from
    /// <see cref="VisorVariantCatalog"/>.
    /// </summary>
    public class VisorHelmetResolver
    {
        private readonly ILogger _logger;
        private readonly VisorVariantCatalog _catalog;

        public VisorHelmetResolver(ILoggerFactory loggerFactory, VisorVariantCatalog catalog)
        {
            _logger = loggerFactory.CreateLogger<VisorHelmetResolver>();
            _catalog = catalog;
        }

        /// <param name="open">The requested state, or null to toggle.</param>
        /// <returns>False when the helmet has no visor variant, is already in the requested state, or the variant item is missing.</returns>
        public bool TryResolve(ItemObject? currentHelmet, bool? open, out ItemObject nextItem, out bool isOpen)
        {
            nextItem = null!;
            isOpen = false;

            var currentItemId = currentHelmet?.StringId;
            if (currentItemId is null || !_catalog.TryFindPair(currentItemId, out var pair)) return false;

            var result = open is null
                ? VisorToggleResult.From(pair, currentItemId)
                : VisorToggleResult.ToState(pair, currentItemId, open.Value);
            if (result is null) return false;

            var item = MBObjectManager.Instance.GetObject<ItemObject>(result.NextItemId);
            if (item is null)
            {
                _logger.Error(
                    $"Could not toggle the visor of '{currentItemId}' because item '{result.NextItemId}' does not exist. " +
                    "Check dadg_visor_variants.xml against the item XML.");
                return false;
            }

            nextItem = item;
            isOpen = result.IsOpen;
            return true;
        }
    }
}
