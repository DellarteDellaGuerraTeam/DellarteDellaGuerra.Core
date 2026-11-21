using TaleWorlds.Engine;
using ComponentType = TaleWorlds.Engine.GameEntity.ComponentType;

namespace DellarteDellaGuerra.Scene
{
    public sealed class ScottishCourtyardSnow : ScriptComponentBehavior
    {
        private Material? _material;
        private MetaMesh? _projector;

        protected override void OnInit()
        {
            base.OnInit();
            ApplySnowShader();
        }

        protected override void OnEditorInit()
        {
            base.OnEditorInit();
            ApplySnowShader();
        }

        private void ApplySnowShader()
        {
            if (_material != null || GameEntity.GetComponentCount(ComponentType.Decal) != 1)
                return;

            var decal = (Decal)GameEntity.GetComponentAtIndex(0, ComponentType.Decal);
            var source = Material.GetFromResource("decal_city_sturgia_ground");
            _material = Material.GetFromResource("deferred_decal_material_worldmap").CreateCopy();
            _material.Name = "dadg_scottish_courtyard_ground";
            foreach (var slot in new[] { 0, 2, 4 })
                _material.SetTextureAtSlot(slot, source.GetTextureWithSlot(slot));
            // Shader flag bit positions can differ, so reapply the source flags by name.
            _material.SetShaderFlags(0);
            foreach (var flag in new[] { "use_albedo", "use_normalmap", "use_specular",
                         "render_on_terrain", "do_not_use_alpha", "separate_displacement_map" })
                _material.AddMaterialShaderFlag(flag, false);
            // World-map snow renders through the native mesh pass, while the
            // Decal component stays green. The unit box preserves its footprint.
            _projector = MetaMesh.GetCopy("decal_mesh");
            _projector.SetMaterial(_material);
            _projector.SetVectorArgument(1, 1, 0, 0);
            GameEntity.AddMultiMesh(_projector, true);
            decal.SetIsVisible(false);
        }
    }
}
