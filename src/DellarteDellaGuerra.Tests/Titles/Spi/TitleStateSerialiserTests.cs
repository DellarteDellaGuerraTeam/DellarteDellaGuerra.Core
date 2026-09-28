using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Titles.Spi;

namespace DellarteDellaGuerra.Tests.Titles.Spi
{
    public class TitleStateSerialiserTests
    {
        [Fact]
        public void DeserialiseTitles_RestoresTheLedgerAndTheOccupation()
        {
            var title = new Title("county", "County", TitleRank.Count, "s_c", "old_lord")
                .GrantTo("lord", 12.5f)
                .InheritBy(null, 30f)
                .WithOccupant("clan_occupier", 31.25f);

            var restored = TitleStateSerialiser.DeserialiseTitles(TitleStateSerialiser.SerialiseTitles(new[] { title }));

            Assert.Equal(title, Assert.Single(restored));
        }

        [Fact]
        public void DeserialiseTitles_OpensALedger_ForASaveWrittenBeforeIt()
        {
            var restored = TitleStateSerialiser.DeserialiseTitles(new List<string>
            {
                "county|County|Count|s_c|old_lord|clan_occupier|3",
                "barony|Barony|Baron|s_b|baron"
            });

            Assert.Equal(new[]
            {
                new Title("county", "County", TitleRank.Count, "s_c", "old_lord", "clan_occupier", 3f),
                new Title("barony", "Barony", TitleRank.Baron, "s_b", "baron")
            }, restored);
        }

        [Fact]
        public void DeserialiseTitles_SkipsAMalformedLedger_AndKeepsTheRest()
        {
            var restored = TitleStateSerialiser.DeserialiseTitles(new List<string>
            {
                "county|County|Count|s_c|lord|||lord|0|Initial",
                "barony|Barony|Baron|s_b|baron|||baron|0",
                "duchy|Duchy|Duke|s_d|duke|||duke|0|Stolen"
            });

            Assert.Equal("county", Assert.Single(restored).Id);
        }
    }
}
