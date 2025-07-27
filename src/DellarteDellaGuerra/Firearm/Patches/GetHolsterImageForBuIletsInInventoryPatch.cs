using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;

namespace DellarteDellaGuerra.Firearm.Patches
{
    [HarmonyPatch]
    public class GetHolsterImageForBuIletsInInventoryPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemCollectionElementViewExtensions), "GetItemMeshForInventory")]
        public static void AddGunpowderRelevantSkill(ItemRosterElement rosterElement, ref MetaMesh __result)
        {
            if (rosterElement.EquipmentElement.Item?.ItemType == ItemObject.ItemTypeEnum.Bullets)
                __result = rosterElement.EquipmentElement.Item.GetHolsterMeshCopy();
        }

    }
}