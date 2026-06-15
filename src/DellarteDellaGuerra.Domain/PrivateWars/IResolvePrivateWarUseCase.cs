using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.PrivateWars
{
    public interface IResolvePrivateWarUseCase
    {
        ResolutionPlan Execute(
            PrivateWar war,
            PrivateWarOutcome outcome,
            IReadOnlyDictionary<string, string> currentOwners,
            Func<string, WarSide?> resolveSide);
    }
}
