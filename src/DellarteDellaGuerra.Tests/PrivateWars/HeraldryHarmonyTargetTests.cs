using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class HeraldryHarmonyTargetTests
{
    [Fact]
    public void HeraldryTargets_ResolveExact146Overloads()
    {
        AssertTarget(
            typeof(Banner).GetMethod(nameof(Banner.TryGetBannerDataFromCode), BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(List<BannerData>).MakeByRefType()], null)!,
            nameof(Banner.TryGetBannerDataFromCode),
            typeof(bool),
            true,
            typeof(string),
            typeof(List<BannerData>).MakeByRefType());
        AssertTarget(
            typeof(Banner).GetMethod(nameof(Banner.AddIconData), BindingFlags.Public | BindingFlags.Instance, null, [typeof(BannerData)], null)!,
            nameof(Banner.AddIconData),
            typeof(void),
            false,
            typeof(BannerData));
        AssertTarget(
            typeof(Banner).GetMethod(nameof(Banner.AddIconData), BindingFlags.Public | BindingFlags.Instance, null, [typeof(BannerData), typeof(int)], null)!,
            nameof(Banner.AddIconData),
            typeof(void),
            false,
            typeof(BannerData),
            typeof(int));
    }

    [Fact]
    public void HeraldryWrappers_SpecifyEveryParameterType()
    {
        var source = ReadHeraldryPatchSource();

        Assert.Contains("new[] { typeof(string), typeof(List<BannerData>).MakeByRefType() }", source, StringComparison.Ordinal);
        Assert.Contains("new[] { typeof(BannerData) }", source, StringComparison.Ordinal);
        Assert.Contains("new[] { typeof(BannerData), typeof(int) }", source, StringComparison.Ordinal);
    }

    private static void AssertTarget(
        MethodInfo target,
        string name,
        Type returnType,
        bool isStatic,
        params Type[] parameterTypes)
    {
        Assert.Equal(typeof(Banner), target.DeclaringType);
        Assert.Equal(name, target.Name);
        Assert.Equal(returnType, target.ReturnType);
        Assert.True(target.IsPublic);
        Assert.Equal(isStatic, target.IsStatic);
        Assert.Equal(parameterTypes, target.GetParameters().Select(parameter => parameter.ParameterType));
    }

    private static string ReadHeraldryPatchSource([CallerFilePath] string testPath = "")
    {
        var sourceRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testPath)!, "..", ".."));
        return File.ReadAllText(Path.Combine(
            sourceRoot,
            "DellarteDellaGuerra.Infrastructure",
            "Heraldry",
            "Patches",
            "AllowSingleplayerExtendedBannerLayersPatch.cs"));
    }
}
