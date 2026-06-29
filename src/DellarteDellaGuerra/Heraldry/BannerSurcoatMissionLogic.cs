using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace DellarteDellaGuerra.Heraldry
{
    /// <summary>
    /// Applies the agent's clan banner as a heraldic texture on any equipped item that has
    /// <c>using_tableau="true"</c> set in its XML definition (tabards, heraldic harnesses, surcoats…).
    /// Works in every scene/mission context (battles, settlements, tournaments …) by hooking
    /// <see cref="OnAgentBuild"/> — the safe, non-Harmony way to post-process agent visuals.
    /// </summary>
    public class BannerSurcoatMissionLogic : MissionLogic
    {
        // Armor slots that can carry heraldic cloth items.
        private static readonly EquipmentIndex[] ArmorSlots =
        {
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg,
        };

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (!agent.IsHuman)
                return;

            // agent.Character is BasicCharacterObject; cast to CharacterObject for campaign-side data.
            CharacterObject character = agent.Character as CharacterObject;

            // Always prefer the hero's own clan banner over whatever the spawn system passed.
            // Tournament systems pass team.Banner (tournament colour) via AgentBuildData.Banner(),
            // which would otherwise tint heraldic items with the team colour instead of clan heraldry.
            Banner clanBanner = character?.HeroObject?.Clan?.Banner ?? banner;

            if (clanBanner == null)
                return;

            // Materialize the mesh list NOW, synchronously, before the async tableau callback fires.
            // GetTableauTextureLarge is asynchronous; by the time its callback runs the agent may
            // have despawned and the skeleton's native objects could be freed.  ToArray() snapshots
            // the managed wrappers at build time so the callback never touches live native memory.
            GameEntity agentEntity = agent.AgentVisuals?.GetEntity();
            if (agentEntity == null)
                return;

            Mesh[] skeletonMeshes = agentEntity.Skeleton?.GetAllMeshes()?.ToArray();
            if (skeletonMeshes == null || skeletonMeshes.Length == 0)
                return;

            // Collect all mesh-name filters for equipped items that opt in to tableau rendering.
            var meshNameFilters = new List<string>();
            foreach (EquipmentIndex slot in ArmorSlots)
            {
                ItemObject item = agent.SpawnEquipment[slot].Item;
                if (item == null || !item.IsUsingTableau)
                    continue;

                string meshName = item.MultiMeshName;
                if (!string.IsNullOrEmpty(meshName))
                    meshNameFilters.Add(meshName);
            }

            if (meshNameFilters.Count == 0)
                return;

            BannerDebugInfo debugInfo = default;
            clanBanner.GetTableauTextureLarge(in debugInfo, bannerTexture =>
            {
                if (bannerTexture == null)
                    return;

                foreach (Mesh mesh in skeletonMeshes)
                {
                    if (mesh?.Name == null)
                        continue;

                    bool matches = false;
                    foreach (string filter in meshNameFilters)
                    {
                        if (mesh.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matches = true;
                            break;
                        }
                    }

                    if (matches)
                        ApplyBannerTexture(mesh, bannerTexture);
                }
            });
        }

        private static void ApplyBannerTexture(Mesh mesh, Texture bannerTexture)
        {
            Material baseMaterial = mesh.GetMaterial();
            if (baseMaterial == null)
                return;

            // CreateCopy so we don't accidentally modify a shared material resource.
            Material mat = baseMaterial.CreateCopy();

            uint blendingFlag = (uint)mat.GetShader()
                .GetMaterialShaderFlagMask("use_tableau_blending", true);

            if (blendingFlag == 0)
                return; // Shader does not support banner blending — skip silently.

            ulong flags = mat.GetShaderFlags();
            mat.SetShaderFlags(flags | blendingFlag);
            mat.SetTexture(Material.MBTextureType.DiffuseMap2, bannerTexture);
            mesh.SetMaterial(mat);
        }
    }
}
