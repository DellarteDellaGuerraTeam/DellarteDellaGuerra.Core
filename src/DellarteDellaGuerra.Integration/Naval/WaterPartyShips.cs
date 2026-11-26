using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony.DependencyInjection.Patches;
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

    internal static void MarkChangedParties()
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

    internal static void RemoveShip(MobilePartyVisual visual)
    {
        if (!Ships.TryGetValue(visual, out var ship)) return;
        Ships.Remove(visual);
        visual.StrategicEntity?.RemoveChild(ship, false, false, false, 0);
    }

    internal static bool AddShip(MobilePartyVisual visual, PartyBase party)
    {
        var mobile = party.MobileParty;
        if (mobile == null || !States.TryGetValue(mobile, out var state) || !state.OnWater) return true;
        var ship = GameEntity.Instantiate(visual.StrategicEntity.Scene, ShipFor(mobile), false, false);
        if (ship == null) return true;
        visual.StrategicEntity.AddChild(ship);
        Ships.Add(visual, ship);
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

/// <summary>Redraws a party's icon when it moves between land and water.</summary>
public class MarkWaterPartiesPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(MobilePartyVisualManager), nameof(MobilePartyVisualManager.OnTick));

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(MarkWaterPartiesPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    // Runs on the main thread before the parallel visual tick, so the dirty flag is seen this frame.
    private static void Prefix() => WaterPartyShips.MarkChangedParties();
}

/// <summary>Drops the ship along with the rest of the party icon.</summary>
public class RemoveWaterPartyShipPatch : IPatch
{
    public MethodInfo? TargetMethod => AccessTools.Method(typeof(MobilePartyVisual), "ResetPartyIcon");

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(RemoveWaterPartyShipPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static void Prefix(MobilePartyVisual __instance) => WaterPartyShips.RemoveShip(__instance);
}

/// <summary>Builds a ship in place of the land icon for a party on water.</summary>
public class AddWaterPartyShipPatch : IPatch
{
    public MethodInfo? TargetMethod => AccessTools.Method(typeof(MobilePartyVisual), "AddMobileIconComponents");

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(AddWaterPartyShipPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Prefix(MobilePartyVisual __instance, PartyBase party) =>
        WaterPartyShips.AddShip(__instance, party);
}
