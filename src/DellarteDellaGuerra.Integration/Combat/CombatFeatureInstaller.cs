using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Combat.Port;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.Combat
{
    /// <summary>
    /// Adds the combat overhaul's game models, each wrapping the model registered before it, or leaves
    /// them out entirely when the overhaul is turned off in the configuration.
    ///
    /// The toggle is read once, on construction, so every phase agrees on it. Turning the overhaul on
    /// or off therefore takes effect on the next game start.
    /// </summary>
    public class CombatFeatureInstaller
    {
        private readonly CombatItemCatalog _catalog;
        private readonly AgentArmourReader _armourReader;
        private readonly ArmourAbsorption _absorption;
        private readonly HalfSword _halfSword;
        private readonly NonTipThrust _nonTipThrust;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public CombatFeatureInstaller(ICombatFeatureToggle featureToggle, CombatItemCatalog catalog,
            AgentArmourReader armourReader, ArmourAbsorption absorption, HalfSword halfSword,
            NonTipThrust nonTipThrust, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<CombatFeatureInstaller>();
            _catalog = catalog;
            _armourReader = armourReader;
            _absorption = absorption;
            _halfSword = halfSword;
            _nonTipThrust = nonTipThrust;
            IsEnabled = featureToggle.IsCombatOverhaulEnabled;
        }

        public bool IsEnabled { get; }

        public void InstallModels(IGameStarter starter)
        {
            if (!IsEnabled)
            {
                _logger.Info("The combat overhaul is disabled in the configuration. Its damage models and item tiers will not be installed.");
                return;
            }

            starter.AddModel(new DadgStrikeMagnitudeModel(_halfSword, _nonTipThrust));
            starter.AddModel(new DadgAgentApplyDamageModel(_armourReader, _absorption));
            starter.AddModel(new DadgItemValueModel(_catalog, _loggerFactory));
        }
    }
}
