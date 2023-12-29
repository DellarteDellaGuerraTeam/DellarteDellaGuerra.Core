using HarmonyLib;
using SandBox.View.Missions;
using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;

namespace DellarteDellaGuerra.HarmonyPatches
{
    [HarmonyPatch]
    public static class GeneralPatches
    {
        /// <summary>
        /// Needed to prevent the disabling of cloth simulation when heraldry texture is applied 
        /// to bd_banner_b tagged entities
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MissionSettlementPrepareView), "SetOwnerBanner")]
        public static bool MissionSettlementPrepareView_SetOwnerBanner_Patch(MissionSettlementPrepareView __instance)
        {
            Campaign campaign = Campaign.Current;
            if (campaign != null && campaign.GameMode == CampaignGameMode.Campaign)
            {
                Settlement currentSettlement = Settlement.CurrentSettlement;
                bool flag;
                if (currentSettlement == null)
                {
                    flag = (null != null);
                }
                else
                {
                    Clan ownerClan = currentSettlement.OwnerClan;
                    flag = (((ownerClan != null) ? ownerClan.Banner : null) != null);
                }
                if (flag && __instance.Mission.Scene != null)
                {
                    bool flag2 = false;
                    foreach (GameEntity gameEntity in __instance.Mission.Scene.FindEntitiesWithTag("bd_banner_b"))
                    {
                        Action<Texture> setAction = delegate (Texture tex)
                        {
                            Material material = Mesh.GetFromResource("bd_banner_b").GetMaterial();
                            uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending", true);
                            ulong shaderFlags = material.GetShaderFlags();
                            material.SetShaderFlags(shaderFlags | (ulong)num);
                            material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
                        };
                        Settlement.CurrentSettlement.OwnerClan.Banner.GetTableauTextureLarge(setAction);
                        flag2 = true;
                    }
                    if (flag2)
                    {
                        // this here is literally the only line that needed changing
                        // in a class that only has one method....
                        __instance.Mission.Scene.SetClothSimulationState(true);
                    }
                }
            }
            return false;
        }
    }
}
