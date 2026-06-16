using DellarteDellaGuerra.Domain.PrivateWars.Port;
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
     *  Bannerlord serialises <c>KingdomDecision</c> instances (and their outcomes) into save
     *  games. A decision deserialised from a save cannot receive constructor-injected services,
     *  so decisions and replaced game models must resolve their collaborators from this static
     *  holder instead of keeping instance references. Every consumer must null-check (or check
     *  <see cref="IsInitialised"/>) and silently fall back to vanilla behaviour when the locator
     *  has not been initialised (unit tests, unexpected load order).
     * </remarks>
     */
    public static class FeudalServices
    {
        public static ITitleRepository? Titles { get; private set; }
        public static IClaimRepository? Claims { get; private set; }
        public static ITensionRepository? Tensions { get; private set; }
        public static IFeudalStructure? Structure { get; private set; }
        public static IAssignTitleUseCase? AssignTitle { get; private set; }
        public static IGetSuzerainUseCase? GetSuzerain { get; private set; }
        public static IEvaluateClaimUseCase? EvaluateClaim { get; private set; }
        public static IComputeFeudalSupportUseCase? ComputeSupport { get; private set; }
        public static IComputeInfluenceTierBonusUseCase? ComputeInfluenceTierBonus { get; private set; }
        public static IAccumulateTensionUseCase? AccumulateTension { get; private set; }
        public static IPrivateWarRepository? PrivateWars { get; private set; }
        public static IPrivateWarHostility? PrivateWarHostility { get; private set; }

        public static bool IsInitialised { get; private set; }

        public static void Initialise(
            ITitleRepository titles,
            IClaimRepository claims,
            ITensionRepository tensions,
            IFeudalStructure structure,
            IAssignTitleUseCase assignTitle,
            IGetSuzerainUseCase getSuzerain,
            IEvaluateClaimUseCase evaluateClaim,
            IComputeFeudalSupportUseCase computeSupport,
            IComputeInfluenceTierBonusUseCase computeInfluenceTierBonus,
            IAccumulateTensionUseCase accumulateTension,
            IPrivateWarRepository privateWars,
            IPrivateWarHostility privateWarHostility)
        {
            Titles = titles;
            Claims = claims;
            Tensions = tensions;
            Structure = structure;
            AssignTitle = assignTitle;
            GetSuzerain = getSuzerain;
            EvaluateClaim = evaluateClaim;
            ComputeSupport = computeSupport;
            ComputeInfluenceTierBonus = computeInfluenceTierBonus;
            AccumulateTension = accumulateTension;
            PrivateWars = privateWars;
            PrivateWarHostility = privateWarHostility;
            IsInitialised = true;
        }

        public static void Reset()
        {
            Titles = null;
            Claims = null;
            Tensions = null;
            Structure = null;
            AssignTitle = null;
            GetSuzerain = null;
            EvaluateClaim = null;
            ComputeSupport = null;
            ComputeInfluenceTierBonus = null;
            AccumulateTension = null;
            PrivateWars = null;
            PrivateWarHostility = null;
            IsInitialised = false;
        }
    }
}
