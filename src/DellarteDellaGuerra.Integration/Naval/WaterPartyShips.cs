using System.Runtime.CompilerServices;
using HarmonyLib;
using SandBox.View.Map.Visuals;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.Naval;

// Without War Sails the generated sea and rivers are Fording terrain and parties keep their land icon there.
// Show a ship instead, from the static prefabs Scripts/non_warsails_navigation/build_map_ships.py derives.
internal static class WaterPartyShips
{
    private const int SmallPartyLimit = 30;
    private static readonly int Fording = (int)TerrainType.Fording;
    private static readonly ConditionalWeakTable<MobileParty, Water> States = new();
    private static readonly ConditionalWeakTable<MobilePartyVisual, GameEntity> Ships = new();

    private sealed class Water
    {
        internal int Face = -1;
        internal bool OnWater;
    }

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(MobilePartyVisualManager), nameof(MobilePartyVisualManager.OnTick)),
            prefix: new HarmonyMethod(typeof(WaterPartyShips), nameof(MarkChangedParties)));
        harmony.Patch(AccessTools.Method(typeof(MobilePartyVisual), "ResetPartyIcon"),
            prefix: new HarmonyMethod(typeof(WaterPartyShips), nameof(RemoveShip)));
        harmony.Patch(AccessTools.Method(typeof(MobilePartyVisual), "AddMobileIconComponents"),
            prefix: new HarmonyMethod(typeof(WaterPartyShips), nameof(AddShip)));
    }

    // Runs on the main thread before the parallel visual tick, so the dirty flag is seen this frame.
    private static void MarkChangedParties()
    {
        foreach (var party in MobileParty.All)
        {
            var face = party.CurrentNavigationFace;
            var state = States.GetOrCreateValue(party);
            if (face.FaceIndex == state.Face) continue;
            state.Face = face.FaceIndex;
            bool onWater = face.IsValid() && (int)Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face) == Fording;
            if (onWater == state.OnWater) continue;
            state.OnWater = onWater;
            party.Party.SetVisualAsDirty();
        }
    }

    private static void RemoveShip(MobilePartyVisual __instance)
    {
        if (!Ships.TryGetValue(__instance, out var ship)) return;
        Ships.Remove(__instance);
        __instance.StrategicEntity?.RemoveChild(ship, false, false, false, 0);
    }

    private static bool AddShip(MobilePartyVisual __instance, PartyBase party)
    {
        var mobile = party.MobileParty;
        if (mobile == null || !States.TryGetValue(mobile, out var state) || !state.OnWater) return true;
        var ship = GameEntity.Instantiate(__instance.StrategicEntity.Scene, ShipFor(mobile), false, false);
        if (ship == null) return true;
        __instance.StrategicEntity.AddChild(ship);
        Ships.Add(__instance, ship);
        return false;
    }

    private static string ShipFor(MobileParty party)
    {
        if (party.IsMainParty || party.IsLordParty && party.LeaderHero != null) return "dadg_map_ship_carrack";
        if (party.IsCaravan) return "dadg_map_ship_cog";
        if (party.IsVillager || party.Party.PartySizeLimit < SmallPartyLimit) return "dadg_map_ship_english_boat";
        return "dadg_map_ship_cog";
    }
}
