using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.PrivateWars;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Titles.Api.Campaign;

namespace DellarteDellaGuerra.Integration.Titles
{
    // Adapts the in-memory Infrastructure registries to the IFeudalStateStore interface
    // expected by the campaign behaviour layer, keeping Infrastructure free of game-API deps.
    public class FeudalStateStoreAdapter : IFeudalStateStore
    {
        private readonly InMemoryTitleRegistry _titleRegistry;
        private readonly InMemoryClaimRegistry _claimRegistry;
        private readonly InMemoryTensionRegistry _tensionRegistry;
        private readonly InMemoryPrivateWarRegistry _privateWarRegistry;

        public FeudalStateStoreAdapter(
            InMemoryTitleRegistry titleRegistry,
            InMemoryClaimRegistry claimRegistry,
            InMemoryTensionRegistry tensionRegistry,
            InMemoryPrivateWarRegistry privateWarRegistry)
        {
            _titleRegistry = titleRegistry;
            _claimRegistry = claimRegistry;
            _tensionRegistry = tensionRegistry;
            _privateWarRegistry = privateWarRegistry;
        }

        public void InitialiseTitles(IEnumerable<Title> titles) => _titleRegistry.Initialise(titles);
        public IReadOnlyList<Title> SnapshotTitles() => _titleRegistry.Snapshot();
        public void InitialiseClaims(IEnumerable<Claim> claims) => _claimRegistry.Initialise(claims);
        public IReadOnlyList<Claim> SnapshotClaims() => _claimRegistry.Snapshot();
        public void InitialiseTensions(IEnumerable<FeudalTension> tensions) => _tensionRegistry.Initialise(tensions);
        public IReadOnlyList<FeudalTension> SnapshotTensions() => _tensionRegistry.Snapshot();
        public void InitialisePrivateWars(IEnumerable<PrivateWar> wars) => _privateWarRegistry.Initialise(wars);
        public IReadOnlyList<PrivateWar> SnapshotPrivateWars() => _privateWarRegistry.Snapshot();
    }
}
