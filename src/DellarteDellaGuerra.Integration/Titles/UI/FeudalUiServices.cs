using DellarteDellaGuerra.Domain.Levy.Port;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Integration.Titles.UI
{
    /**
     * <summary>
     *  Static service locator for the feudal UI layer, populated once at boot by the
     *  Integration layer alongside <c>FeudalServices</c>.
     * </summary>
     * <remarks>
     *  Gauntlet view models and encyclopedia page mixins are instantiated by the game's UI
     *  machinery (screen pushes, encyclopedia page construction), not by the DI container,
     *  so they resolve their read-only collaborators from this static holder. Every consumer
     *  must check <see cref="IsInitialised"/> and degrade gracefully (hide the feudal panels)
     *  when the locator has not been initialised (no campaign, unit tests).
     * </remarks>
     */
    public static class FeudalUiServices
    {
        public static ITitleRepository? Titles { get; private set; }
        public static IFeudalStructure? Structure { get; private set; }
        public static IGetSuzerainUseCase? GetSuzerain { get; private set; }
        public static IGetDirectVassalsUseCase? GetDirectVassals { get; private set; }
        public static IBuildFeudalMapUseCase? BuildFeudalMap { get; private set; }
        public static ILevyRepository? Levies { get; private set; }

        public static bool IsInitialised { get; private set; }

        public static void Initialise(
            ITitleRepository titles,
            IFeudalStructure structure,
            IGetSuzerainUseCase getSuzerain,
            IGetDirectVassalsUseCase getDirectVassals,
            IBuildFeudalMapUseCase buildFeudalMap,
            ILevyRepository levies)
        {
            Titles = titles;
            Structure = structure;
            GetSuzerain = getSuzerain;
            GetDirectVassals = getDirectVassals;
            BuildFeudalMap = buildFeudalMap;
            Levies = levies;
            IsInitialised = true;
        }

        public static void Reset()
        {
            Titles = null;
            Structure = null;
            GetSuzerain = null;
            GetDirectVassals = null;
            BuildFeudalMap = null;
            Levies = null;
            IsInitialised = false;
        }
    }
}