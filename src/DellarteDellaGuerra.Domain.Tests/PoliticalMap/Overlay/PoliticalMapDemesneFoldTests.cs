using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapDemesneFoldTests
    {
        private static readonly Dictionary<string, TitleRank> Ranks = new()
        {
            ["county_a"] = TitleRank.Earl,
            ["county_b"] = TitleRank.Earl,
            ["duchy_d"] = TitleRank.Duke,
            ["barony_e"] = TitleRank.Baron
        };

        // Rows from y = 0 up; each character is a settlement index (1-based), '.' is water.
        private static PoliticalMapGrid Grid(params string[] rows)
        {
            int width = rows[0].Length;
            var cells = new ushort[width * rows.Length];
            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    char c = rows[row][column];
                    cells[row * width + column] = c == '.' ? PoliticalMapGrid.None : (ushort)(c - '0');
                }
            }

            return new PoliticalMapGrid(width, rows.Length, 1f, new[] { "s1", "s2", "s3", "s4" }, cells);
        }

        private static IReadOnlyDictionary<string, string> Fold(PoliticalMapGrid grid, params string?[] territories) =>
            PoliticalMapDemesneFold.Fold(grid, territories, territory => Ranks.TryGetValue(territory, out TitleRank rank) ? rank : null);

        [Fact]
        public void ADemesneIsPaintedAsTheCountySharingTheLongestBorderWithIt()
        {
            PoliticalMapGrid grid = Grid(
                "1112",
                "1332",
                "1332",
                "1112");

            // The duchy's 2x2 block touches county_a on 6 sides and county_b on 2.
            Assert.Equal(
                new Dictionary<string, string> { ["duchy_d"] = "county_a" },
                Fold(grid, "county_a", "county_b", "duchy_d"));
        }

        [Fact]
        public void ARimCellTouchingTheDemesneTwiceCountsOnce()
        {
            PoliticalMapGrid grid = Grid(
                "332",
                "312");

            // county_b's one cell sits in the crook of the L and touches the duchy on two sides;
            // counted once it ties with county_a's one cell, and the tie goes to county_a.
            Assert.Equal("county_a", Fold(grid, "county_b", "county_a", "duchy_d")["duchy_d"]);
        }

        [Fact]
        public void ATieGoesToTheSmallerCountyId()
        {
            PoliticalMapGrid grid = Grid("132");

            Assert.Equal("county_a", Fold(grid, "county_b", "county_a", "duchy_d")["duchy_d"]);
        }

        [Fact]
        public void OnlyCountiesAndOnlyDemesnesTakePart()
        {
            PoliticalMapGrid grid = Grid(
                "4431",
                "4431");

            // The barony and the duchy are both demesnes: the barony touches no county, so it stays
            // as it is, and the duchy folds into the county it touches.
            IReadOnlyDictionary<string, string> folds = Fold(grid, "county_a", "county_b", "duchy_d", "barony_e");

            Assert.Equal(new Dictionary<string, string> { ["duchy_d"] = "county_a" }, folds);
        }

        [Fact]
        public void WaterAndUntitledSettlementsAreNotCounties()
        {
            PoliticalMapGrid grid = Grid(".34");

            Assert.Empty(Fold(grid, "county_a", "county_b", "duchy_d", null));
        }
    }
}
