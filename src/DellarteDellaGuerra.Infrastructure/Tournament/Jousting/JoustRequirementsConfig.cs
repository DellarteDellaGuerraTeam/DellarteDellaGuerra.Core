using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.Tournament.Jousting
{
    public class JoustRequirementsConfig : IJoustRequirementsProvider
    {
        private const int DefaultSkillMinimum = 100;
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public JoustRequirementsConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public JoustRequirements GetRequirements()
        {
            var config = _configProvider.Config?.JoustingConfig;
            return new JoustRequirements(new List<JoustSkillRequirement>
            {
                new JoustSkillRequirement(JoustSkillRequirement.Riding, config?.MinimumRidingSkill ?? DefaultSkillMinimum),
                new JoustSkillRequirement(JoustSkillRequirement.Polearm, config?.MinimumPolearmSkill ?? DefaultSkillMinimum),
            });
        }
    }
}
