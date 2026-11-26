using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using DellarteDellaGuerra.Integration.Initialisation;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.ModuleManager;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.Naval;

// Without War Sails no settlement has a port, so the nameplate's native port icon slot is free. Show it,
// with a DADG anchor in place of NavalDLC's sprite, on the settlements whose shore landing the generator opened.
internal static class LandingNameplates
{
    private static readonly Type[] PrefabPatches = { typeof(LargeIcon), typeof(MediumIcon), typeof(SmallIcon) };

    internal static void DisableIconsWithNavalDLC(DadgUIExtenderRegistrar registrar)
    {
        if (!ModuleHelper.IsModuleActive("NavalDLC")) return;
        foreach (var patch in PrefabPatches) registrar.Disable(patch);
    }

    private const string Icon = "descendant::Widget[@Id='PortIconWidget']";

    internal abstract class LandingIcon : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new() { new Attribute("Sprite", "DADG\\Map\\landing_anchor") };
    }

    [PrefabExtension("SettlementNameplateItemLarge", Icon)]
    internal sealed class LargeIcon : LandingIcon { }

    [PrefabExtension("SettlementNameplateItemMedium", Icon)]
    internal sealed class MediumIcon : LandingIcon { }

    [PrefabExtension("SettlementNameplateItemSmall", Icon)]
    internal sealed class SmallIcon : LandingIcon { }
}

/// <summary>Shows the landing icon on the settlements listed beside the generated navmesh.</summary>
public class MarkLandingNameplatesPatch : IPatch
{
    private static readonly AccessTools.FieldRef<SettlementNameplateVM, bool> HasPort =
        AccessTools.FieldRefAccess<SettlementNameplateVM, bool>("_bindHasPort");
    private static HashSet<string> _landings = new();

    public MarkLandingNameplatesPatch()
    {
        // Written by Scripts/non_warsails_navigation/install_variant.py beside the generated navmesh.
        var file = Path.Combine(ModuleHelper.GetModuleFullPath("DellarteDellaGuerraMap"), "ModuleData", "dadg_no_warsails_landings.txt");
        if (File.Exists(file)) _landings = new HashSet<string>(File.ReadAllLines(file));
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(SettlementNameplateVM), nameof(SettlementNameplateVM.RefreshDynamicProperties));

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(MarkLandingNameplatesPatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(SettlementNameplateVM __instance)
    {
        if (_landings.Contains(__instance.Settlement.StringId)) HasPort(__instance) = true;
    }
}
