using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Church;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;

namespace DellarteDellaGuerra.Infrastructure.Tests.Church;

public class ChurchSettlementsXmlProviderTests
{
    [Fact]
    public void MapAndValidate_ValidConfiguration_PreservesDiocesesAndFoundationData()
    {
        var config = Configuration(
            Diocese("see_ely", "See of Ely",
                Foundation("village_Ely_Cathedral", "Cathedral"),
                Foundation("village_Walsingham_Abbey", "Abbey", shrine: true)),
            Diocese("see_llandaff", "See of Llandaff",
                Foundation("village_Llandaff_Cathedral", "Cathedral"),
                Foundation("village_Tintern_Abbey", "Abbey")));

        var dioceses = ChurchSettlementsXmlProvider.MapAndValidate(config, new RecordingLogger());

        Assert.Equal(new[] { "see_ely", "see_llandaff" }, dioceses.Select(diocese => diocese.Id));
        var walsingham = dioceses.SelectMany(diocese => diocese.Members)
            .Single(member => member.SettlementId == "village_Walsingham_Abbey");
        Assert.Equal(ChurchSettlementKind.Abbey, walsingham.Kind);
        Assert.True(walsingham.IsShrine);
    }

    [Fact]
    public void MapAndValidate_DuplicateDioceseId_DisablesChurchData()
    {
        var logger = new RecordingLogger();
        var config = Configuration(
            Diocese("see_ely", "See of Ely", Foundation("ely_cathedral", "Cathedral")),
            Diocese("see_ely", "Duplicate See", Foundation("duplicate_cathedral", "Cathedral")));

        var dioceses = ChurchSettlementsXmlProvider.MapAndValidate(config, logger);

        Assert.Empty(dioceses);
        Assert.Contains(logger.Warnings, warning => warning.Contains("Duplicate diocese id 'see_ely'"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void MapAndValidate_DioceseWithoutExactlyOneCathedral_DisablesChurchData(int cathedralCount)
    {
        var logger = new RecordingLogger();
        var foundations = Enumerable.Range(1, cathedralCount)
            .Select(index => Foundation($"cathedral_{index}", "Cathedral"))
            .Append(Foundation("abbey", "Abbey"))
            .ToArray();

        var dioceses = ChurchSettlementsXmlProvider.MapAndValidate(
            Configuration(Diocese("see_ely", "See of Ely", foundations)),
            logger);

        Assert.Empty(dioceses);
        Assert.Contains(logger.Warnings, warning => warning.Contains("must have exactly one Cathedral"));
    }

    [Fact]
    public void MapAndValidate_SettlementInMoreThanOneDiocese_DisablesChurchData()
    {
        var logger = new RecordingLogger();
        var config = Configuration(
            Diocese("see_ely", "See of Ely",
                Foundation("ely_cathedral", "Cathedral"),
                Foundation("shared_abbey", "Abbey")),
            Diocese("see_llandaff", "See of Llandaff",
                Foundation("llandaff_cathedral", "Cathedral"),
                Foundation("shared_abbey", "Abbey")));

        var dioceses = ChurchSettlementsXmlProvider.MapAndValidate(config, logger);

        Assert.Empty(dioceses);
        Assert.Contains(logger.Warnings, warning => warning.Contains("'shared_abbey' belongs to more than one diocese"));
    }

    [Fact]
    public void MapAndValidate_NoShrineConfigured_KeepsChurchDataActive()
    {
        var dioceses = ChurchSettlementsXmlProvider.MapAndValidate(
            Configuration(Diocese("see_ely", "See of Ely",
                Foundation("ely_cathedral", "Cathedral"),
                Foundation("battle_abbey", "Abbey"))),
            new RecordingLogger());

        Assert.Single(dioceses);
        Assert.DoesNotContain(dioceses.SelectMany(diocese => diocese.Members), member => member.IsShrine);
    }

    [Fact]
    public void MapAndValidate_MoreThanOneShrine_DisablesChurchData()
    {
        var logger = new RecordingLogger();
        var config = Configuration(Diocese("see_ely", "See of Ely",
            Foundation("ely_cathedral", "Cathedral"),
            Foundation("walsingham_abbey", "Abbey", shrine: true),
            Foundation("battle_abbey", "Abbey", shrine: true)));

        var dioceses = ChurchSettlementsXmlProvider.MapAndValidate(config, logger);

        Assert.Empty(dioceses);
        Assert.Contains(logger.Warnings, warning => warning.Contains("More than one shrine"));
    }

    [Fact]
    public void MapAndValidate_UnknownFoundationKind_SkipsEntryAndKeepsValidDiocese()
    {
        var logger = new RecordingLogger();
        var config = Configuration(Diocese("see_ely", "See of Ely",
            Foundation("ely_cathedral", "Cathedral"),
            Foundation("unknown_foundation", "Collegiate")));

        var diocese = Assert.Single(ChurchSettlementsXmlProvider.MapAndValidate(config, logger));

        Assert.Equal("ely_cathedral", Assert.Single(diocese.Members).SettlementId);
        Assert.Contains(logger.Warnings, warning => warning.Contains("Unknown church settlement kind 'Collegiate'"));
    }

    private static ChurchSettlementsConfig Configuration(params ChurchDioceseConfig[] dioceses) =>
        new() { Dioceses = dioceses.ToList() };

    private static ChurchDioceseConfig Diocese(
        string id,
        string name,
        params ChurchSettlementConfig[] foundations) =>
        new() { Id = id, Name = name, Settlements = foundations.ToList() };

    private static ChurchSettlementConfig Foundation(string id, string kind, bool shrine = false) =>
        new() { Id = id, Kind = kind, Shrine = shrine };

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Warnings { get; } = new();

        public void Debug(string message, Exception? exception = null)
        {
        }

        public void Info(string message, Exception? exception = null)
        {
        }

        public void Warn(string message, Exception? exception = null) => Warnings.Add(message);

        public void Error(string message, Exception? exception = null)
        {
        }

        public void Fatal(string message, Exception? exception = null)
        {
        }
    }
}
