using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;

namespace DellarteDellaGuerra.Infrastructure.Firearm.Patches;

public class GetHolsterImageForBuIletsInInventoryPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(ItemCollectionElementViewExtensions), "GetItemMeshForInventory");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(GetHolsterImageForBuIletsInInventoryPatch), nameof(AddHolsterImageForBullets));

    public PatchType PatchType => PatchType.Postfix;

    private static void AddHolsterImageForBullets(ItemRosterElement rosterElement, ref MetaMesh __result)
    {
        if (rosterElement.EquipmentElement.Item?.ItemType == ItemObject.ItemTypeEnum.Bullets)
            __result = rosterElement.EquipmentElement.Item.GetHolsterMeshCopy();
    }
}
