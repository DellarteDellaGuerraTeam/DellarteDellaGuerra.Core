using DellarteDellaGuerra.Domain.Church.Ledger;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /**
     * <summary>
     *  Static service locator for the church UI layer, populated once at campaign game start by
     *  the Integration layer.
     * </summary>
     * <remarks>
     *  Gauntlet view models are instantiated by the game's UI machinery (screen pushes), not by
     *  the DI container, so they resolve their read-only collaborators from this static holder.
     *  Consumers must null-check and degrade gracefully (empty screen) when the locator has not
     *  been initialised (no campaign, unit tests).
     * </remarks>
     */
    public static class ChurchUiServices
    {
        public static IBuildChurchLedgerUseCase? BuildChurchLedger { get; private set; }

        public static void Initialise(IBuildChurchLedgerUseCase buildChurchLedger)
        {
            BuildChurchLedger = buildChurchLedger;
        }
    }
}
