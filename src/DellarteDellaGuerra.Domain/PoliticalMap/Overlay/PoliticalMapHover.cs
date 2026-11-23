namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// What the hover readout says about the land under the cursor. Only the fields the view
    /// shows are set: the kingdom view the faction; the owners view the owner clan, the highest
    /// title it holds (raw, with its string id, for a TextObject) and the clan it answers to
    /// (<see cref="PoliticalMapVassalHierarchy.LiegeOf"/>). The settlement is always set.
    /// </summary>
    public sealed record PoliticalMapHoverInfo(
        PoliticalMapView View,
        string SettlementName,
        string? FactionName = null,
        string? OwnerClanName = null,
        string? TitleName = null,
        string? LiegeClanName = null);

    /// <summary>Works out the hover readout for a settlement's land in a view.</summary>
    public class PoliticalMapHoverDescriber
    {
        private readonly PoliticalMapPainter _painter;

        public PoliticalMapHoverDescriber(PoliticalMapPainter painter)
        {
            _painter = painter;
        }

        /// <summary>The readout for the cells of <paramref name="settlementId"/>; null when the view draws nothing.</summary>
        public PoliticalMapHoverInfo? Describe(
            PoliticalMapView view,
            string settlementId,
            IPoliticalMapWorld world,
            IPoliticalMapNames names,
            PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.SubVassals)
        {
            string settlement = names.SettlementName(settlementId) ?? settlementId;
            switch (view)
            {
                case PoliticalMapView.Kingdom:
                    return new PoliticalMapHoverInfo(view, settlement, FactionName: NameOf(world.FactionOf(settlementId), names.FactionName));
                case PoliticalMapView.DeJure:
                    return new PoliticalMapHoverInfo(view, settlement,
                        TitleName: _painter.GeographicLabelNameOf(_painter.GeographicLabelKeyOf(settlementId, world, level)));
                case PoliticalMapView.Owners:
                    if (world.OwnerClanOf(settlementId) is not { } owner) return new PoliticalMapHoverInfo(view, settlement);

                    PoliticalMapVassalHierarchy hierarchy = _painter.HierarchyOf(world);
                    return new PoliticalMapHoverInfo(
                        view, settlement,
                        OwnerClanName: NameOf(owner, names.ClanName),
                        TitleName: hierarchy.HighestTitleOf(owner)?.Name,
                        LiegeClanName: NameOf(hierarchy.LiegeOf(owner), names.ClanName));
                default:
                    return null;
            }
        }

        private static string? NameOf(string? id, System.Func<string, string?> nameOf) =>
            id is null ? null : nameOf(id) ?? id;
    }
}
