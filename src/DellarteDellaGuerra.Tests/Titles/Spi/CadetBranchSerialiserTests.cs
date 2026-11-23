using DellarteDellaGuerra.Titles.Spi;

namespace DellarteDellaGuerra.Tests.Titles.Spi
{
    public class CadetBranchSerialiserTests
    {
        [Fact]
        public void Deserialise_RestoresEveryBranchItWasGiven()
        {
            var branches = new Dictionary<string, string>
            {
                ["percy_cadet_henry"] = "percy",
                ["neville_cadet_john"] = "neville"
            };

            var restored = CadetBranchSerialiser.Deserialise(CadetBranchSerialiser.Serialise(branches));

            Assert.Equal(branches, restored);
        }

        [Fact]
        public void Deserialise_RestoresNothing_FromAnEmptySave()
        {
            Assert.Empty(CadetBranchSerialiser.Deserialise(new List<string>()));
        }

        [Fact]
        public void Deserialise_SkipsAMalformedEntry_AndKeepsTheRest()
        {
            var restored = CadetBranchSerialiser.Deserialise(new List<string>
            {
                "percy_cadet_henry|percy",
                "neville_cadet_john"
            });

            Assert.Equal(new Dictionary<string, string> { ["percy_cadet_henry"] = "percy" }, restored);
        }
    }
}
