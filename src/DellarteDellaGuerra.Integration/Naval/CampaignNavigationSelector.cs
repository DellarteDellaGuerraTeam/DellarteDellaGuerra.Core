using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using Vec2 = TaleWorlds.Library.Vec2;
using TaleWorlds.ModuleManager;
using Path = System.IO.Path;
using EngineScene = TaleWorlds.Engine.Scene;

namespace DellarteDellaGuerra.Integration.Naval;

internal static class CampaignNavigationSelector
{
    private const string LandScene = "dadg_main_map_no_warsails";
    private static WeakReference<EngineScene>? _landMap;
    private static string _landCache = string.Empty;

    internal static void Install()
    {
        if (ModuleHelper.IsModuleActive("NavalDLC")) return;

        var harmony = new HarmonyLib.Harmony("dadg.non_naval_campaign_map");
        harmony.Patch(AccessTools.Method(typeof(SettlementPositionScript), "ReadNavigationCacheForNavigationTypeOnGameLoad"),
            prefix: new HarmonyMethod(typeof(CampaignNavigationSelector), nameof(ReadCache)));
        harmony.Patch(AccessTools.Method(typeof(MapScene), nameof(MapScene.Load)),
            transpiler: new HarmonyMethod(typeof(CampaignNavigationSelector), nameof(ReplaceMapRead)));
        harmony.Patch(AccessTools.Method(typeof(MobileParty), "DoAiPathMode"),
            postfix: new HarmonyMethod(typeof(CampaignNavigationSelector), nameof(StopDirectMoveAtShore)));
        harmony.Patch(AccessTools.PropertySetter(typeof(MobileParty), nameof(MobileParty.IsCurrentlyAtSea)),
            prefix: new HarmonyMethod(typeof(CampaignNavigationSelector), nameof(KeepOnLand)));
        LandingNameplates.Install(harmony);
        WaterPartyShips.Install(harmony);
    }

    private static readonly AccessTools.FieldRef<MobileParty, bool> PathMode =
        AccessTools.FieldRefAccess<MobileParty, bool>("_pathMode");

    // Without a path, vanilla moves straight at the target and accepts any step that lands on a face of the
    // same island. The generated sea shares the land's island, so that step can jump the thin shore wall.
    // Stop the straight move where it first meets terrain a land party cannot cross.
    private static void StopDirectMoveAtShore(MobileParty __instance)
    {
        if (PathMode(__instance) || __instance.IsTransitionInProgress || !__instance.CurrentNavigationFace.IsValid()) return;
        Vec2 from = __instance.Position.ToVec2(), to = __instance.NextTargetPosition.ToVec2();
        if ((to - from).LengthSquared < 1e-6f) return;
        var invalid = Campaign.Current.Models.PartyNavigationModel
            .GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default);
        // A party already on such terrain (e.g. under a bridge) would be clamped in place forever.
        var face = (int)Campaign.Current.MapSceneWrapper.GetFaceTerrainType(__instance.CurrentNavigationFace);
        if (Array.IndexOf(invalid, face) >= 0) return;
        var edge = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(
            __instance.CurrentNavigationFace, from, to, invalid);
        if ((edge - to).LengthSquared < 1e-6f) return;
        var back = from - edge;
        float length = back.Normalize();
        __instance.NextTargetPosition = new CampaignVec2(edge + back * Math.Min(0.05f, length), true);
    }

    // The generated sea is land terrain, so no party is ever at sea without War Sails. A stray flag would send
    // the next settlement encounter to NavalDLC's port_menu, which does not exist here.
    private static void KeepOnLand(ref bool value) => value = false;

    private static IEnumerable<CodeInstruction> ReplaceMapRead(IEnumerable<CodeInstruction> instructions)
    {
        var read = AccessTools.Method(typeof(EngineScene), nameof(EngineScene.Read),
            new[] { typeof(string), typeof(string), typeof(SceneInitializationData).MakeByRefType(), typeof(string) });
        var replacement = AccessTools.Method(typeof(CampaignNavigationSelector), nameof(ReadMap));
        bool found = false;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(read))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                found = true;
            }
            yield return instruction;
        }
        if (!found) throw new MissingMethodException("Campaign map loader no longer calls the module-qualified Scene.Read.");
    }

    private static void ReadMap(EngineScene scene, string name, string moduleId, ref SceneInitializationData init, string atmosphere)
    {
        _landMap = null;
        var module = ModuleHelper.GetModuleFullPath(moduleId);
        var variant = Path.Combine(module, "SceneObj", LandScene);
        if (Directory.Exists(variant))
        {
            CopyMainMapSidecars(module, variant);
            var cache = Path.Combine(module, "ModuleData", "DistanceCaches", "settlements_distance_cache_NoWarSails.bin");
            foreach (var file in new[] { Path.Combine(variant, "scene.xscene"), Path.Combine(variant, "navmesh.bin"), cache })
                if (!File.Exists(file)) throw new FileNotFoundException("Incomplete DADG non-naval campaign map", file);

            // Scene.Read can initialize the settlement script before returning.
            _landCache = cache;
            _landMap = new WeakReference<EngineScene>(scene);
            name = LandScene;
        }
        scene.Read(name, moduleId, ref init, atmosphere);
    }

    // Only navmesh.bin is shipped in the variant folder. The engine resolves a scene's other files from the
    // folder named after it, so mirror Main_map there, renaming the scene root. Each copy carries its source's
    // write time, so any Main_map change (even to an older-dated file) is mirrored again.
    private static void CopyMainMapSidecars(string module, string variant)
    {
        foreach (var from in Directory.GetFiles(Path.Combine(module, "SceneObj", "Main_map")))
        {
            var name = Path.GetFileName(from);
            var to = Path.Combine(variant, name);
            if (name == "navmesh.bin" ||
                File.Exists(to) && File.GetLastWriteTimeUtc(to) == File.GetLastWriteTimeUtc(from)) continue;
            var temp = to + ".tmp";
            if (name == "scene.xscene")
            {
                var xml = File.ReadAllText(from);
                const string root = "<scene name=\"Main_map\"";
                int at = xml.IndexOf(root, StringComparison.Ordinal);
                if (at < 0) throw new InvalidDataException($"Missing Main_map scene root in {from}");
                File.WriteAllText(temp, xml.Substring(0, at) + $"<scene name=\"{LandScene}\"" + xml.Substring(at + root.Length));
            }
            else File.Copy(from, temp, true);
            File.SetLastWriteTimeUtc(temp, File.GetLastWriteTimeUtc(from));
            File.Copy(temp, to, true);
            File.Delete(temp);
        }
    }

    private static bool ReadCache(SettlementPositionScript __instance, MobileParty.NavigationType navigationCapability,
        ref SandBoxNavigationCache __result)
    {
        if (navigationCapability != MobileParty.NavigationType.Default || _landMap == null ||
            !_landMap.TryGetTarget(out var scene) || scene != __instance.Scene) return true;

        // Native deserialization reads these CRCs but does not enforce them in 1.5.7.
        using (var header = new BinaryReader(File.OpenRead(_landCache)))
            if (header.ReadUInt32() != scene.GetSceneXMLCRC() || header.ReadUInt32() != scene.GetNavigationMeshCRC())
                throw new InvalidDataException($"DADG land cache disagrees with the loaded campaign scene: {_landCache}");

        __result = new SandBoxNavigationCache(navigationCapability);
        __result.Deserialize(_landCache);
        return false;
    }
}
