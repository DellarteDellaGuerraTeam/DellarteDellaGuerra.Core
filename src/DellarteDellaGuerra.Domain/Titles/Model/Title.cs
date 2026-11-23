using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// HolderHeroId is the de jure holder of the dignity, a hero whose clan is derived through
    /// IGenealogy.GetClanOf; OccupantClanId is the de facto holder of the seat when the two
    /// diverge (conquest) — it stays a clan because de facto occupation derives from
    /// Settlement.OwnerClan, which has no hero equivalent. ContestedSinceDay is the campaign
    /// day the current occupation began; both are null when the title is uncontested.
    /// </summary>
    /// <remarks>
    /// Holders is the title's ledger, oldest first, and its last entry is the current holder.
    /// A title changes hands only through its own methods, which also settle what becomes of
    /// the occupation, so the ledger, the holder and the occupation cannot disagree.
    /// </remarks>
    public record Title
    {
        public string Id { get; }
        public string Name { get; }
        public TitleRank Rank { get; }
        public string SeatSettlementId { get; }
        public IReadOnlyList<TitleHolder> Holders { get; private init; }
        public string? OccupantClanId { get; private init; }
        public float? ContestedSinceDay { get; private init; }

        /// <summary>
        /// A title as the campaign starts: its ledger opens with the holder on heldSinceDay,
        /// the day their house took the title (day 0 when unknown).
        /// </summary>
        public Title(
            string id,
            string name,
            TitleRank rank,
            string seatSettlementId,
            string? holderHeroId,
            string? occupantClanId = null,
            float? contestedSinceDay = null,
            float heldSinceDay = 0f)
            : this(id, name, rank, seatSettlementId,
                new[] { new TitleHolder(holderHeroId, heldSinceDay, TitleAcquisition.Initial) },
                occupantClanId, contestedSinceDay)
        {
        }

        private Title(
            string id,
            string name,
            TitleRank rank,
            string seatSettlementId,
            IReadOnlyList<TitleHolder> holders,
            string? occupantClanId,
            float? contestedSinceDay)
        {
            Id = id;
            Name = name;
            Rank = rank;
            SeatSettlementId = seatSettlementId;
            Holders = holders;
            OccupantClanId = occupantClanId;
            ContestedSinceDay = contestedSinceDay;
        }

        /// <summary>A title restored from a save, ledger and all. The ledger is never empty.</summary>
        public static Title Restore(
            string id,
            string name,
            TitleRank rank,
            string seatSettlementId,
            IReadOnlyList<TitleHolder> holders,
            string? occupantClanId,
            float? contestedSinceDay) =>
            new(id, name, rank, seatSettlementId, holders.ToList(), occupantClanId, contestedSinceDay);

        public string? HolderHeroId => Holders[Holders.Count - 1].HeroId;

        /// <summary>
        /// The day the current holder's line took the title. An heir carries on the tenure of
        /// the holder they inherited from, so a death does not make an old title look new.
        /// </summary>
        public float HeldSinceDay
        {
            get
            {
                int index = Holders.Count - 1;
                while (index > 0 && Holders[index].Acquisition == TitleAcquisition.Inherited) index--;
                return Holders[index].SinceDay;
            }
        }

        public bool IsContested => OccupantClanId is not null;

        /// <summary>A lawful grant: the new holder takes the seat too, which ends any occupation.</summary>
        public Title GrantTo(string? heroId, float day) =>
            PassTo(heroId, day, TitleAcquisition.Granted).WithOccupant(null, null);

        /// <summary>A vacant title taken by force: the conqueror holds the seat, which ends any occupation.</summary>
        public Title ConqueredBy(string? heroId, float day) =>
            PassTo(heroId, day, TitleAcquisition.Conquered).WithOccupant(null, null);

        /// <summary>
        /// An inheritance: whoever occupies the seat goes on occupying it, unless the occupier is the
        /// heir's own clan, which now holds it.
        /// </summary>
        public Title InheritBy(string? heirId, string? heirClanId, float day)
        {
            Title inherited = PassTo(heirId, day, TitleAcquisition.Inherited);
            return heirClanId is not null && inherited.OccupantClanId == heirClanId
                ? inherited.WithOccupant(null, null)
                : inherited;
        }

        /// <summary>
        /// A won claim: the occupation ends only when the winner's own clan occupies the seat. Another
        /// house occupying it goes on contesting the new holder.
        /// </summary>
        public Title AwardTo(string heroId, string clanId, float day)
        {
            Title awarded = PassTo(heroId, day, TitleAcquisition.Awarded);
            return awarded.OccupantClanId == clanId ? awarded.WithOccupant(null, null) : awarded;
        }

        public Title WithOccupant(string? occupantClanId, float? contestedSinceDay) =>
            this with { OccupantClanId = occupantClanId, ContestedSinceDay = contestedSinceDay };

        private Title PassTo(string? heroId, float day, TitleAcquisition acquisition) =>
            this with { Holders = Holders.Append(new TitleHolder(heroId, day, acquisition)).ToList() };

        // A record compares lists by reference, so the ledger is compared entry by entry.
        public virtual bool Equals(Title? other) =>
            other is not null
            && Id == other.Id
            && Name == other.Name
            && Rank == other.Rank
            && SeatSettlementId == other.SeatSettlementId
            && Holders.SequenceEqual(other.Holders)
            && OccupantClanId == other.OccupantClanId
            && ContestedSinceDay == other.ContestedSinceDay;

        public override int GetHashCode() => Id.GetHashCode();
    }
}
