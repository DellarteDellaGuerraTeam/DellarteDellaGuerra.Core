using System;
using System.Text.RegularExpressions;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Scene
{
    public sealed class ScottishBurghRoof : ScriptComponentBehavior
    {
        private static readonly Regex LowerLodRoof =
            new Regex(@"^mi_vla_house_d\.lod[1-5]\.1$", RegexOptions.CultureInvariant);
        private MetaMesh? _roofCopy;

        protected override void OnInit()
        {
            base.OnInit();
            RestoreRoofMaterial();
        }

        protected override void OnEditorInit()
        {
            base.OnEditorInit();
            RestoreRoofMaterial();
        }

        private void RestoreRoofMaterial()
        {
            var name = GameEntity.Name;
            if (_roofCopy != null || GameEntity.MultiMeshComponentCount != 1 ||
                !name.StartsWith("scottish_rollout_dadg_sco_", StringComparison.Ordinal) ||
                !(name.EndsWith("__north_frontage_4", StringComparison.Ordinal) ||
                  name.EndsWith("__south_frontage_3", StringComparison.Ordinal)))
                return;

            var source = GameEntity.GetMetaMesh(0);
            if (source.MeshCount != 19)
                return;
            var roofCount = 0;
            for (var i = 0; i < source.MeshCount; i++)
            {
                var mesh = source.GetMeshAtIndex(i);
                if (!mesh.Name.StartsWith("mi_vla_house_d", StringComparison.Ordinal))
                    return;
                if (!LowerLodRoof.IsMatch(mesh.Name))
                    continue;
                if (mesh.GetMaterial().Name != "map_icon_hideouts")
                    return;
                roofCount++;
            }
            if (roofCount != 5)
                return;

            // The five lower-LOD roofs have the hideout atlas instead of the
            // native LOD0 roof material. Restore that atlas and its snow shader.
            var material = Material.GetFromResource("arab_map_icons");
            var copy = MetaMesh.CreateMetaMesh("dadg_scottish_burgh_house_d");
            source.CopyTo(copy, true);
            copy.Frame = source.Frame;
            if (copy.MeshCount != source.MeshCount)
                return;
            for (var i = 0; i < source.MeshCount; i++)
            {
                if (copy.GetMeshAtIndex(i).Pointer == source.GetMeshAtIndex(i).Pointer ||
                    copy.GetLodMaskForMeshAtIndex(i) != source.GetLodMaskForMeshAtIndex(i))
                    return;
            }
            for (var i = 0; i < copy.MeshCount; i++)
            {
                var mesh = copy.GetMeshAtIndex(i);
                if (LowerLodRoof.IsMatch(mesh.Name))
                    mesh.SetMaterial(material);
            }

            if (!GameEntity.RemoveMultiMesh(source))
                return;
            GameEntity.AddMultiMesh(copy, true);
            _roofCopy = copy;
        }
    }
}
