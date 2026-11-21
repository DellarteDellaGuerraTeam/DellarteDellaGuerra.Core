using System;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Scene
{
    public sealed class ScottishFortificationLod : ScriptComponentBehavior
    {
        protected override void OnInit()
        {
            base.OnInit();
            DelayRampartLods();
        }

        protected override void OnEditorInit()
        {
            base.OnEditorInit();
            DelayRampartLods();
        }

        private void DelayRampartLods()
        {
            if (!GameEntity.Name.StartsWith("scottish_rollout_dadg_sco_", StringComparison.Ordinal))
                return;

            for (var i = 0; i < GameEntity.MultiMeshComponentCount; i++)
            {
                var mesh = GameEntity.GetMetaMesh(i);
                if (mesh.MeshCount == 0)
                    continue;
                var name = mesh.GetMeshAtIndex(0).Name;
                // Keep crenellations through normal zoom while retaining distant LODs.
                if (name == "mi_stu_wall_2s.0" || name == "mi_stu_mini_gatehouse_2.0")
                    mesh.SetLodBias(-2);
            }
        }
    }
}
