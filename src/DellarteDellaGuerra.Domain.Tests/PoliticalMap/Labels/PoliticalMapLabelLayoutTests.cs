using DellarteDellaGuerra.Domain.PoliticalMap.Labels;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapLabelLayoutTests
    {
        private static PoliticalMapLabelCandidate Candidate(int id, float x, float y, float pixelRadius, int textLength, long priority) =>
            new(id, x, y, pixelRadius, textLength, priority);

        [Fact]
        public void ARegionTooSmallOnScreenGetsNoLabel()
        {
            Assert.Equal(-1, PoliticalMapLabelLayout.SizeIndexFor(5f));
            Assert.Equal(-1, PoliticalMapLabelLayout.SizeIndexFor(0f));
        }

        [Fact]
        public void ABiggerRegionGetsABiggerFont()
        {
            int small = PoliticalMapLabelLayout.SizeIndexFor(20f);
            int big = PoliticalMapLabelLayout.SizeIndexFor(40f);

            Assert.True(small >= 0);
            Assert.True(big > small);
        }

        [Fact]
        public void AHugeRegionTopsOutAtTheLargestFont()
        {
            Assert.Equal(PoliticalMapLabelLayout.FontSizes.Count - 1, PoliticalMapLabelLayout.SizeIndexFor(10000f));
        }

        [Fact]
        public void ALongNameIsPrintedAsLargeAsAShortOne()
        {
            const string longName = "Earldom of Warwick and Salisbury";
            var labels = PoliticalMapLabelLayout.Place(
                new[]
                {
                    Candidate(1, 960f, 300f, 40f, longName.Length, 1),
                    Candidate(2, 960f, 800f, 40f, "Kent".Length, 1)
                },
                1920f,
                1080f);

            Assert.Equal(2, labels.Count);
            Assert.Equal(labels[0].SizeIndex, labels[1].SizeIndex);
            Assert.Equal(PoliticalMapLabelLayout.SizeIndexFor(40f), labels[0].SizeIndex);
        }

        [Fact]
        public void ALongNameInASmallRegionIsStillShownWhenTheRegionShowsBigEnough()
        {
            // A 32-character name at size 13 runs 208 px, far past a 20 px radius: it is still drawn.
            var labels = PoliticalMapLabelLayout.Place(new[] { Candidate(1, 500f, 500f, 20f, 32, 1) }, 1920f, 1080f);

            Assert.Equal(PoliticalMapLabelLayout.SizeIndexFor(20f), Assert.Single(labels).SizeIndex);
        }

        [Fact]
        public void ALongLabelBlocksANeighbourItsTextRunsOver()
        {
            // At size 31 (radius 40), "Principality of Wales" (21 characters) is ~330 px wide: it
            // reaches a label 150 px to its side, which a 4-character name would not.
            int size = PoliticalMapLabelLayout.FontSizes[PoliticalMapLabelLayout.SizeIndexFor(40f)];
            Assert.True(PoliticalMapLabelLayout.TextWidthOf(size, 21) / 2f > 150f);

            var longFirst = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, 500f, 500f, 40f, 21, 2), Candidate(2, 650f, 500f, 40f, 4, 1) }, 1920f, 1080f);
            var shortFirst = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, 500f, 500f, 40f, 4, 2), Candidate(2, 650f, 500f, 40f, 4, 1) }, 1920f, 1080f);

            Assert.Equal(1, Assert.Single(longFirst).Id);
            Assert.Equal(2, shortFirst.Count);
        }

        [Fact]
        public void TheLowerPriorityLabelIsDroppedWhenTwoOverlap()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, 500f, 500f, 60f, 6, 1), Candidate(2, 510f, 505f, 60f, 6, 2) },
                1920f,
                1080f);

            Assert.Equal(2, Assert.Single(labels).Id);
        }

        [Fact]
        public void LabelsFarApartAreBothPlaced()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, 300f, 300f, 60f, 6, 1), Candidate(2, 1200f, 800f, 60f, 6, 2) },
                1920f,
                1080f);

            Assert.Equal(new[] { 1, 2 }, labels.Select(label => label.Id).OrderBy(id => id));
        }

        [Fact]
        public void ALabelOffScreenIsNotPlacedAndBlocksNothing()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, -900f, 500f, 60f, 6, 9), Candidate(2, 500f, 500f, 60f, 6, 1) },
                1920f,
                1080f);

            Assert.Equal(2, Assert.Single(labels).Id);
        }

        [Fact]
        public void ATooSmallLabelBlocksNothing()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, 500f, 500f, 2f, 6, 9), Candidate(2, 500f, 500f, 60f, 6, 1) },
                1920f,
                1080f);

            PoliticalMapPlacedLabel placed = Assert.Single(labels);
            Assert.Equal(2, placed.Id);
            Assert.Equal(PoliticalMapLabelLayout.SizeIndexFor(60f), placed.SizeIndex);
        }

        private static PoliticalMapLabelCandidate Grouped(int id, float x, float y, float pixelRadius, long priority, string group) =>
            new(id, x, y, pixelRadius, group.Length, priority, group);

        [Fact]
        public void APatchNearTheBiggestOneOfTheSameNameIsNotNamedAgain()
        {
            // Two patches of one kingdom 300 px apart on a 1920 px screen (closer than a third of it).
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Grouped(1, 500f, 500f, 30f, 1, "House of York"), Grouped(2, 800f, 700f, 30f, 9, "House of York") },
                1920f,
                1080f);

            Assert.Equal(2, Assert.Single(labels).Id);
        }

        [Fact]
        public void APatchFarFromItsTwinIsNamedAgain()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Grouped(1, 100f, 100f, 30f, 1, "York"), Grouped(2, 1800f, 1000f, 30f, 9, "York") },
                1920f,
                1080f);

            Assert.Equal(new[] { 1, 2 }, labels.Select(label => label.Id).OrderBy(id => id));
        }

        [Fact]
        public void APatchWhoseTwinIsOffScreenIsNamed()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Grouped(1, -400f, 500f, 60f, 9, "York"), Grouped(2, 100f, 500f, 30f, 1, "York") },
                1920f,
                1080f);

            Assert.Equal(2, Assert.Single(labels).Id);
        }

        [Fact]
        public void DifferentNamesCloseTogetherAreBothPlaced()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Grouped(1, 500f, 300f, 30f, 2, "York"), Grouped(2, 500f, 600f, 30f, 1, "Lancaster") },
                1920f,
                1080f);

            Assert.Equal(2, labels.Count);
        }

        [Fact]
        public void TheRepeatDistanceIsAShareOfTheScreenWidth()
        {
            // Two patches whose boxes are ~450 px apart: past a third of a 1280 px screen (427 px),
            // inside a third of a 3840 px one.
            PoliticalMapLabelCandidate[] patches = { Grouped(1, 200f, 200f, 30f, 2, "York"), Grouped(2, 700f, 200f, 30f, 1, "York") };

            Assert.Equal(2, PoliticalMapLabelLayout.Place(patches, 1280f, 720f).Count);
            Assert.Single(PoliticalMapLabelLayout.Place(patches, 3840f, 2160f));
        }

        [Fact]
        public void ZoomedFarOutAScatteredRealmIsNamedOnce()
        {
            // Six patches of one realm spread over 500 px, the biggest in the middle.
            var patches = new[]
            {
                Grouped(1, 900f, 500f, 40f, 100, "House of Lancaster"),
                Grouped(2, 700f, 400f, 20f, 10, "House of Lancaster"),
                Grouped(3, 1150f, 650f, 20f, 9, "House of Lancaster"),
                Grouped(4, 650f, 700f, 20f, 8, "House of Lancaster"),
                Grouped(5, 1100f, 300f, 20f, 7, "House of Lancaster"),
                Grouped(6, 900f, 800f, 20f, 6, "House of Lancaster")
            };

            Assert.Equal(1, Assert.Single(PoliticalMapLabelLayout.Place(patches, 1920f, 1080f)).Id);
        }

        [Fact]
        public void LabelsWithoutAGroupAreNeverDeduplicated()
        {
            var labels = PoliticalMapLabelLayout.Place(
                new[] { Candidate(1, 500f, 300f, 30f, 4, 2), Candidate(2, 500f, 600f, 30f, 4, 1) },
                1920f,
                1080f);

            Assert.Equal(2, labels.Count);
        }

        [Fact]
        public void ALabelWithNoTextIsNotPlaced()
        {
            Assert.Empty(PoliticalMapLabelLayout.Place(new[] { Candidate(1, 500f, 500f, 60f, 0, 1) }, 1920f, 1080f));
        }
    }
}
