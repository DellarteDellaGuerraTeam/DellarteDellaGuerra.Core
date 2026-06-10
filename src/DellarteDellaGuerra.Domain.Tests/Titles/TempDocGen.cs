using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class TempDocGen
    {
        [Fact]
        public void Generate()
        {
            var structure = new FakeFeudalStructure()
                .AddTitle("kingdom_england", TitleRank.King, null, "town_london", "England")
                .AddTitle("duchy_york", TitleRank.Duke, "kingdom_england", "town_york", "Duchy of York")
                .AddTitle("county_richmond", TitleRank.Count, "duchy_york", "town_richmond", "County of Richmond")
                .AddTitle("barony_middleham", TitleRank.Baron, "county_richmond", "castle_middleham", "Barony of Middleham")
                .AddTitle("barony_barnard", TitleRank.Baron, "county_richmond", "castle_barnard", "Barony of Barnard")
                .AddTitle("county_northumberland", TitleRank.Count, "duchy_york", "town_alnwick", "County of Northumberland")
                .AddTitle("barony_bamburgh", TitleRank.Baron, "county_northumberland", "castle_bamburgh", "Barony of Bamburgh")
                .AddTitle("duchy_lancaster", TitleRank.Duke, "kingdom_england", "town_lancaster", "Duchy of Lancaster")
                .AddTitle("county_derby", TitleRank.Count, "duchy_lancaster", "town_derby", "County of Derby")
                .AddTitle("barony_tutbury", TitleRank.Baron, "county_derby", "castle_tutbury", "Barony of Tutbury")
                .AddTitle("county_somerset", TitleRank.Count, "duchy_lancaster", "town_bristol", "County of Somerset")
                .AddTitle("barony_farleigh", TitleRank.Baron, "county_somerset", "castle_farleigh", "Barony of Farleigh")
                .AddTitle("barony_dunster", TitleRank.Baron, "county_somerset", "castle_dunster", "Barony of Dunster")
                .AddTitle("duchy_gloucester", TitleRank.Duke, "kingdom_england", "town_gloucester", "Duchy of Gloucester")
                .AddTitle("county_warwick", TitleRank.Count, "duchy_gloucester", "town_warwick", "County of Warwick")
                .AddTitle("barony_kenilworth", TitleRank.Baron, "county_warwick", "castle_kenilworth", "Barony of Kenilworth")
                .AddTitle("county_hereford", TitleRank.Count, "duchy_gloucester", "town_hereford", "County of Hereford")
                .AddTitle("barony_raglan", TitleRank.Baron, "county_hereford", "castle_raglan", "Barony of Raglan")
                .AddTitle("barony_goodrich", TitleRank.Baron, "county_hereford", "castle_goodrich", "Barony of Goodrich")
                .AddTitle("county_kent", TitleRank.Count, "kingdom_england", "town_canterbury", "County of Kent")
                .AddTitle("barony_dover", TitleRank.Baron, "county_kent", "castle_dover", "Barony of Dover")
                .AddTitle("county_norfolk", TitleRank.Count, "kingdom_england", "town_norwich", "County of Norfolk")
                .AddTitle("barony_framlingham", TitleRank.Baron, "county_norfolk", "castle_framlingham", "Barony of Framlingham");

            var repository = new FakeTitleRepository(
                new Title("kingdom_england", "England", TitleRank.King, "town_london", "clan_lancaster"),
                new Title("duchy_york", "Duchy of York", TitleRank.Duke, "town_york", "clan_york"),
                new Title("county_richmond", "County of Richmond", TitleRank.Count, "town_richmond", "clan_neville"),
                new Title("barony_middleham", "Barony of Middleham", TitleRank.Baron, "castle_middleham", "clan_neville_cadet"),
                new Title("barony_barnard", "Barony of Barnard", TitleRank.Baron, "castle_barnard", null),
                new Title("county_northumberland", "County of Northumberland", TitleRank.Count, "town_alnwick", "clan_percy"),
                new Title("barony_bamburgh", "Barony of Bamburgh", TitleRank.Baron, "castle_bamburgh", "clan_percy_cadet"),
                new Title("duchy_lancaster", "Duchy of Lancaster", TitleRank.Duke, "town_lancaster", "clan_lancaster"),
                new Title("county_derby", "County of Derby", TitleRank.Count, "town_derby", "clan_stanley"),
                new Title("barony_tutbury", "Barony of Tutbury", TitleRank.Baron, "castle_tutbury", null),
                new Title("county_somerset", "County of Somerset", TitleRank.Count, "town_bristol", "clan_beaufort"),
                new Title("barony_farleigh", "Barony of Farleigh", TitleRank.Baron, "castle_farleigh", "clan_beaufort_cadet"),
                new Title("barony_dunster", "Barony of Dunster", TitleRank.Baron, "castle_dunster", null),
                new Title("duchy_gloucester", "Duchy of Gloucester", TitleRank.Duke, "town_gloucester", "clan_york_gloucester"),
                new Title("county_warwick", "County of Warwick", TitleRank.Count, "town_warwick", "clan_neville_warwick"),
                new Title("barony_kenilworth", "Barony of Kenilworth", TitleRank.Baron, "castle_kenilworth", null),
                new Title("county_hereford", "County of Hereford", TitleRank.Count, "town_hereford", "clan_herbert"),
                new Title("barony_raglan", "Barony of Raglan", TitleRank.Baron, "castle_raglan", "clan_herbert_cadet"),
                new Title("barony_goodrich", "Barony of Goodrich", TitleRank.Baron, "castle_goodrich", null),
                new Title("county_kent", "County of Kent", TitleRank.Count, "town_canterbury", "clan_woodville"),
                new Title("barony_dover", "Barony of Dover", TitleRank.Baron, "castle_dover", null),
                new Title("county_norfolk", "County of Norfolk", TitleRank.Count, "town_norwich", "clan_howard"),
                new Title("barony_framlingham", "Barony of Framlingham", TitleRank.Baron, "castle_framlingham", "clan_howard_cadet"));

            FeudalMap map = new BuildFeudalMapUseCase(repository, structure).Execute();
            var renderer = new RenderFeudalMapUseCase();
            var content = renderer.RenderMarkdown(map) + "\n```mermaid\n" + renderer.RenderMermaid(map) + "```\n";
            File.WriteAllText("/tmp/feudal-map-doc.md", content);
        }
    }
}
