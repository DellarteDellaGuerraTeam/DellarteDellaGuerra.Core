using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class RenderFeudalMapUseCaseTests
    {
        private static FeudalMap CreateMap()
        {
            var barony = new FeudalMapEntry(
                "barony_b", "Barony of B", TitleRank.Baron, "castle_b", null, new List<FeudalMapEntry>());
            var county = new FeudalMapEntry(
                "county_c", "County of C", TitleRank.Count, "town_c", "clan_count",
                new List<FeudalMapEntry> { barony });
            var duchy = new FeudalMapEntry(
                "duchy_d", "Duchy of D", TitleRank.Duke, "town_d", "clan_duke",
                new List<FeudalMapEntry> { county });
            var kingdom = new FeudalMapEntry(
                "kingdom_k", "Kingdom of K", TitleRank.King, "town_k", "clan_king",
                new List<FeudalMapEntry> { duchy });
            return new FeudalMap(new List<FeudalMapEntry> { kingdom });
        }

        [Fact]
        public void RenderMarkdown_StartsWithTitleHeading_AndOneSectionPerRealm()
        {
            var markdown = new RenderFeudalMapUseCase().RenderMarkdown(CreateMap());

            Assert.StartsWith("# Feudal map\n", markdown);
            Assert.Contains("## Kingdom of K\n", markdown);
        }

        [Fact]
        public void RenderMarkdown_RendersEntryLines_WithTwoSpaceIndentPerLevel()
        {
            var markdown = new RenderFeudalMapUseCase().RenderMarkdown(CreateMap());

            Assert.Contains(
                "- **Kingdom of K** (`kingdom_k`) — seat: `town_k` — held by: `clan_king`\n", markdown);
            Assert.Contains(
                "\n  - **Duchy of D** (`duchy_d`) — seat: `town_d` — held by: `clan_duke`\n", markdown);
            Assert.Contains(
                "\n    - **County of C** (`county_c`) — seat: `town_c` — held by: `clan_count`\n", markdown);
            Assert.Contains("\n      - **Barony of B** (`barony_b`)", markdown);
        }

        [Fact]
        public void RenderMarkdown_RendersVacant_WhenHolderIsNull()
        {
            var markdown = new RenderFeudalMapUseCase().RenderMarkdown(CreateMap());

            Assert.Contains(
                "- **Barony of B** (`barony_b`) — seat: `castle_b` — held by: *vacant*\n", markdown);
        }

        [Fact]
        public void RenderMermaid_DeclaresFlowchart_WithNodePerTitle()
        {
            var mermaid = new RenderFeudalMapUseCase().RenderMermaid(CreateMap());

            Assert.StartsWith("flowchart TD\n", mermaid);
            Assert.Contains("kingdom_k[\"Kingdom of K\\n(clan_king)\"]", mermaid);
            Assert.Contains("duchy_d[\"Duchy of D\\n(clan_duke)\"]", mermaid);
            Assert.Contains("county_c[\"County of C\\n(clan_count)\"]", mermaid);
        }

        [Fact]
        public void RenderMermaid_RendersVacantLabel_WhenHolderIsNull()
        {
            var mermaid = new RenderFeudalMapUseCase().RenderMermaid(CreateMap());

            Assert.Contains("barony_b[\"Barony of B\\n(vacant)\"]", mermaid);
        }

        [Fact]
        public void RenderMermaid_RendersEdges_FromSuzerainToVassal()
        {
            var mermaid = new RenderFeudalMapUseCase().RenderMermaid(CreateMap());

            Assert.Contains("kingdom_k --> duchy_d", mermaid);
            Assert.Contains("duchy_d --> county_c", mermaid);
            Assert.Contains("county_c --> barony_b", mermaid);
        }

        [Fact]
        public void RenderMermaid_SanitisesNodeIds_ReplacingNonAlphanumerics()
        {
            var entry = new FeudalMapEntry(
                "kingdom.k-1", "Kingdom", TitleRank.King, "town_k", null, new List<FeudalMapEntry>());
            var mermaid = new RenderFeudalMapUseCase().RenderMermaid(
                new FeudalMap(new List<FeudalMapEntry> { entry }));

            Assert.Contains("kingdom_k_1[\"Kingdom\\n(vacant)\"]", mermaid);
        }

        [Fact]
        public void RenderMermaid_IsDeterministic_AndOrdersNodesBeforeEdges()
        {
            var renderer = new RenderFeudalMapUseCase();
            var first = renderer.RenderMermaid(CreateMap());
            var second = renderer.RenderMermaid(CreateMap());

            Assert.Equal(first, second);
            Assert.True(
                first.IndexOf("barony_b[\"", StringComparison.Ordinal) <
                first.IndexOf("kingdom_k --> duchy_d", StringComparison.Ordinal));
        }
    }
}
