using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /**
     * <summary>
     *  Initialise/snapshot facade over the in-memory feudal registries, used by the campaign
     *  behaviours to persist state through Bannerlord saves. The Integration layer adapts the
     *  Infrastructure registries (InMemoryTitleRegistry et al.) to this interface.
     * </summary>
     */
    public interface IFeudalStateStore
    {
        void InitialiseTitles(IEnumerable<Title> titles);

        IReadOnlyList<Title> SnapshotTitles();

        void InitialiseClaims(IEnumerable<Claim> claims);

        IReadOnlyList<Claim> SnapshotClaims();

        void InitialiseTensions(IEnumerable<FeudalTension> tensions);

        IReadOnlyList<FeudalTension> SnapshotTensions();

        void InitialisePrivateWars(IEnumerable<PrivateWar> wars);

        IReadOnlyList<PrivateWar> SnapshotPrivateWars();
    }
}
