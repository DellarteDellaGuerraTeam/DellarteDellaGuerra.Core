using DellarteDellaGuerra.Domain.PoliticalMap.Labels;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels;

public class PoliticalMapLabelWrappingTests
{
    [Theory]
    [InlineData("Neville of Middleham", 8, "Neville\nof\nMiddleham")]
    [InlineData("Neville of Middleham", 12, "Neville of\nMiddleham")]
    [InlineData("County of Northumberland", 12, "County of\nNorthumberland")]
    [InlineData("Duchy of York", 20, "Duchy of York")]
    [InlineData("Northumberland", 4, "Northumberland")]
    public void OversizedNamesWrapAtWordsWithoutShorteningThem(string text, int width, string expected)
    {
        Assert.Equal(expected, PoliticalMapLabelWrapping.Wrap(text, width));
    }

    [Fact]
    public void VeryLongTitlesUseAtMostThreeLinesAndKeepAllWords()
    {
        const string title = "Earldom of Warwick and Salisbury";
        string wrapped = PoliticalMapLabelWrapping.Wrap(title, 5);
        Assert.Equal(3, wrapped.Split('\n').Length);
        Assert.Equal(title, wrapped.Replace('\n', ' '));
    }

    [Fact]
    public void ANameReturnsToOneLineWhenItsTerritoryHasRoom()
    {
        const string name = "Neville of Middleham";
        Assert.Contains('\n', PoliticalMapLabelWrapping.Wrap(name, 8));
        Assert.Equal(name, PoliticalMapLabelWrapping.Wrap(name, 40));
    }
}
