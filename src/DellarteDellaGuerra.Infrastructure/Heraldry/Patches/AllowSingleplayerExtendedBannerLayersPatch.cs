using System.Collections.Generic;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.Heraldry.Patches;

internal static class SingleplayerExtendedBannerLayers
{
    public const int MaxBannerDataCount = 1024;

    private const int ValuesPerBannerData = 10;
    private const float RotationStep = 0.0027777778f;

    private static readonly FieldInfo BannerCodeField = AccessTools.Field(typeof(Banner), "_bannerCode");
    private static readonly FieldInfo BannerDataListField = AccessTools.Field(typeof(Banner), "_bannerDataList");

    public static bool ShouldUseVanillaLimit()
    {
        try
        {
            return GameNetwork.IsSessionActive;
        }
        catch
        {
            return true;
        }
    }

    public static MBList<BannerData> GetMutableBannerDataList(Banner banner) =>
        (MBList<BannerData>)BannerDataListField.GetValue(banner);

    public static void ClearCachedBannerCode(Banner banner)
    {
        BannerCodeField?.SetValue(banner, null);
    }

    public static bool TryParseBannerCode(string bannerCode, out List<BannerData> bannerDataList)
    {
        bannerDataList = new List<BannerData>();
        string[] values = bannerCode.Split('.');

        for (int i = 0; i + ValuesPerBannerData <= values.Length; i += ValuesPerBannerData)
        {
            if (!int.TryParse(values[i], out int meshId) ||
                !int.TryParse(values[i + 1], out int colorId) ||
                !int.TryParse(values[i + 2], out int colorId2) ||
                !int.TryParse(values[i + 3], out int sizeX) ||
                !int.TryParse(values[i + 4], out int sizeY) ||
                !int.TryParse(values[i + 5], out int positionX) ||
                !int.TryParse(values[i + 6], out int positionY) ||
                !int.TryParse(values[i + 7], out int drawStroke) ||
                !int.TryParse(values[i + 8], out int mirror) ||
                !int.TryParse(values[i + 9], out int rotation))
            {
                bannerDataList.Clear();
                return false;
            }

            if (bannerDataList.Count < MaxBannerDataCount)
            {
                bannerDataList.Add(new BannerData(
                    meshId,
                    colorId,
                    colorId2,
                    new Vec2(sizeX, sizeY),
                    new Vec2(positionX, positionY),
                    drawStroke == 1,
                    mirror == 1,
                    rotation * RotationStep));
            }
        }

        return true;
    }
}

public class AllowSingleplayerExtendedBannerCodeParsingPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(Banner), nameof(Banner.TryGetBannerDataFromCode));

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(AllowSingleplayerExtendedBannerCodeParsingPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Prefix(string bannerCode, ref List<BannerData> bannerDataList, ref bool __result)
    {
        if (SingleplayerExtendedBannerLayers.ShouldUseVanillaLimit())
        {
            return true;
        }

        __result = SingleplayerExtendedBannerLayers.TryParseBannerCode(bannerCode, out bannerDataList);
        return false;
    }
}

public class AllowSingleplayerExtendedBannerAppendLayerPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(Banner), nameof(Banner.AddIconData), new[] { typeof(BannerData) });

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(AllowSingleplayerExtendedBannerAppendLayerPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Prefix(Banner __instance, BannerData iconData)
    {
        if (SingleplayerExtendedBannerLayers.ShouldUseVanillaLimit())
        {
            return true;
        }

        MBList<BannerData> bannerDataList = SingleplayerExtendedBannerLayers.GetMutableBannerDataList(__instance);
        if (bannerDataList.Count < SingleplayerExtendedBannerLayers.MaxBannerDataCount)
        {
            SingleplayerExtendedBannerLayers.ClearCachedBannerCode(__instance);
            bannerDataList.Add(iconData);
        }

        return false;
    }
}

public class AllowSingleplayerExtendedBannerInsertLayerPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(Banner), nameof(Banner.AddIconData), new[] { typeof(BannerData), typeof(int) });

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(AllowSingleplayerExtendedBannerInsertLayerPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Prefix(Banner __instance, BannerData iconData, int index)
    {
        if (SingleplayerExtendedBannerLayers.ShouldUseVanillaLimit())
        {
            return true;
        }

        MBList<BannerData> bannerDataList = SingleplayerExtendedBannerLayers.GetMutableBannerDataList(__instance);
        if (bannerDataList.Count < SingleplayerExtendedBannerLayers.MaxBannerDataCount &&
            index > 0 &&
            index <= bannerDataList.Count)
        {
            SingleplayerExtendedBannerLayers.ClearCachedBannerCode(__instance);
            bannerDataList.Insert(index, iconData);
        }

        return false;
    }
}
