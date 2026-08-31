using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Titles.Api
{
    /**
     * <summary>
     *  Static service locator for the feudal titles system, populated once at boot by the
     *  Integration layer.
     * </summary>
     * <remarks>
     *  Consumers instantiated by the game rather than by the DI container (campaign
     *  behaviours restored from a save, static helpers) resolve their collaborators from
     *  this holder instead of keeping instance references. Every consumer must null-check
     *  (or check <see cref="IsInitialised"/>) and silently fall back to vanilla behaviour
     *  when the locator has not been initialised (unit tests, unexpected load order).
     * </remarks>
     */
    public static class FeudalServices
    {
        public static ITitleRepository? Titles { get; private set; }
        public static IClaimRepository? Claims { get; private set; }
        public static IFeudalStructure? Structure { get; private set; }
        public static IAssignTitleUseCase? AssignTitle { get; private set; }
        public static IGetSuzerainUseCase? GetSuzerain { get; private set; }
        public static IEvaluateClaimUseCase? EvaluateClaim { get; private set; }

        public static bool IsInitialised { get; private set; }

        public static void Initialise(
            ITitleRepository titles,
            IClaimRepository claims,
            IFeudalStructure structure,
            IAssignTitleUseCase assignTitle,
            IGetSuzerainUseCase getSuzerain,
            IEvaluateClaimUseCase evaluateClaim)
        {
            Titles = titles;
            Claims = claims;
            Structure = structure;
            AssignTitle = assignTitle;
            GetSuzerain = getSuzerain;
            EvaluateClaim = evaluateClaim;
            IsInitialised = true;
        }

        public static void Reset()
        {
            Titles = null;
            Claims = null;
            Structure = null;
            AssignTitle = null;
            GetSuzerain = null;
            EvaluateClaim = null;
            IsInitialised = false;
        }
    }
}
