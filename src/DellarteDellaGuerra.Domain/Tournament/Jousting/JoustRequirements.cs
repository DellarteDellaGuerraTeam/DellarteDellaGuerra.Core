using System;
using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.Tournament.Jousting
{
    public class JoustRequirements
    {
        public IReadOnlyList<JoustSkillRequirement> RequiredSkills { get; }

        public JoustRequirements(IReadOnlyList<JoustSkillRequirement> requiredSkills)
        {
            RequiredSkills = requiredSkills;
        }

        public bool IsMet(Func<string, int> getSkillValue) =>
            RequiredSkills.All(req => req.IsMet(getSkillValue(req.Name)));
    }
}
