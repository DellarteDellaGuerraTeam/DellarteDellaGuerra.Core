using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.PoliticalMap.Rendering
{
    /// <summary>
    /// Draws the political map's geometry on the campaign map scene: one entity per territory, its
    /// meshes draped over the terrain on the <see cref="PoliticalMapSheet"/> in a copy of
    /// <c>vertex_color_mat</c> switched to real alpha blending, with a separate additive white frontier material.
    /// A territory is rebuilt into a new
    /// hidden entity a chunk at a time. Affected territories swap together after the batch finishes,
    /// so both sides of a smoothed frontier always use the same geometry.
    /// </summary>
    /// <remarks>
    /// The stock material blends <c>AddAlpha</c> (logged by the 2026-10-28 mesh spike): the vertex
    /// colour times its alpha is added to the terrain, so black adds nothing and saturated colours
    /// wash out to pastel over bright ground. <c>Modulate</c> is the source-alpha over
    /// inverse-source-alpha blend, so a fill at the wash opacity covers that share of the terrain.
    /// Frontiers use their own <c>AddAlpha</c> copy: opaque white brightens the ground regardless
    /// of the fill's blend mode, and its small extra lift keeps it above the painted sheet.
    /// Main thread only: meshes are built through the engine. The renderer belongs to one scene;
    /// when the campaign map scene goes, its entities go with it and the renderer is dropped
    /// without touching them.
    /// </remarks>
    public class PoliticalMapRenderer
    {
        private const string MaterialName = "vertex_color_mat";
        private const float Lift = 0.3f;
        private const float FrontierLift = 0.025f;

        private readonly PoliticalMapGrid _grid;
        private readonly PoliticalMapSheet _sheet;
        private readonly Material _material;
        private readonly Material _frontierMaterial;
        private readonly MeshBuilder _meshBuilder = new();
        private readonly Dictionary<string, GameEntity> _shown = new();
        private readonly Dictionary<string, GameEntity> _building = new();

        private PoliticalMapZoomBand _band = PoliticalMapZoomBand.Mid;
        private bool _visible = true;

        public PoliticalMapRenderer(TaleWorlds.Engine.Scene scene, PoliticalMapGrid grid, PoliticalMapSheet sheet)
        {
            Scene = scene;
            _grid = grid;
            _sheet = sheet;
            Material source = Material.GetFromResource(MaterialName);
            _material = source.CreateCopy();
            _material.SetAlphaBlendMode(Material.MBAlphaBlendMode.Modulate);
            _frontierMaterial = source.CreateCopy();
            _frontierMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.AddAlpha);
        }

        public TaleWorlds.Engine.Scene Scene { get; }

        /// <summary>Adds one chunk to the territory's rebuild; the first chunk drops any rebuild left unfinished.</summary>
        public void AddChunk(string territory, int chunkIndex, PoliticalMapMeshChunk chunk)
        {
            if (chunkIndex == 0) Discard(territory);
            if (!_building.TryGetValue(territory, out GameEntity? entity))
            {
                entity = GameEntity.CreateEmpty(Scene, true, true, true);
                entity.Name = "dadg_political_map";
                entity.SetVisibilityExcludeParents(false);
                _building[territory] = entity;
            }

            entity.AddMesh(BuildMesh(chunk));
        }

        /// <summary>Swaps the territory's finished rebuild in for what it showed.</summary>
        public void Commit(string territory)
        {
            if (_shown.TryGetValue(territory, out GameEntity? old)) old.Remove(0);
            _shown.Remove(territory);
            if (!_building.TryGetValue(territory, out GameEntity? built)) return;
            _building.Remove(territory);
            _shown[territory] = built;
            Apply(built);
        }

        /// <summary>Shows or hides the wash for the zoom band, and everything for the Off view.</summary>
        public void SetVisibility(PoliticalMapZoomBand band, bool visible)
        {
            if (band == _band && visible == _visible) return;
            _band = band;
            _visible = visible;
            foreach (GameEntity entity in _shown.Values) Apply(entity);
        }

        private void Apply(GameEntity entity)
        {
            entity.SetVisibilityExcludeParents(_visible && PoliticalMapZoom.ShowsWash(_band));
            entity.SetReadyToRender(true);
        }

        /// <summary>Discards an unfinished replacement; the currently shown territory is kept.</summary>
        public void Discard(string territory)
        {
            if (!_building.TryGetValue(territory, out GameEntity? entity)) return;
            entity.Remove(0);
            _building.Remove(territory);
        }

        private Mesh BuildMesh(PoliticalMapMeshChunk chunk)
        {
            // MeshBuilder collects indexed faces in managed memory and uploads them in one call.
            // AddTriangleWithVertexColors crosses into the engine and edits its mesh for every face.
            foreach (PoliticalMapQuad quad in chunk.Quads)
            {
                int left = quad.Column, right = quad.Column + quad.Width, bottom = quad.Row, top = quad.Row + quad.Height;
                uint colour = quad.Color;
                int a = AddCorner(left, bottom, colour, 0), b = AddCorner(right, bottom, colour, 0);
                int c = AddCorner(right, top, colour, 0), d = AddCorner(left, top, colour, 0);
                _meshBuilder.AddFace(a, b, c);
                _meshBuilder.AddFace(a, c, d);
            }

            foreach (PoliticalMapTriangle triangle in chunk.Triangles)
            {
                uint colour = triangle.Color;
                float lift = chunk.IsFrontier ? FrontierLift : 0;
                int a = AddCorner(triangle.A.Column, triangle.A.Row, colour, lift);
                int b = AddCorner(triangle.B.Column, triangle.B.Row, colour, lift);
                int c = AddCorner(triangle.C.Column, triangle.C.Row, colour, lift);
                _meshBuilder.AddFace(a, b, c);
            }

            Mesh mesh = _meshBuilder.Finalize();
            mesh.SetMaterial(chunk.IsFrontier ? _frontierMaterial : _material);
            mesh.CullingMode = MBMeshCullingMode.None;
            mesh.RecomputeBoundingBox();
            return mesh;
        }

        private int AddCorner(float column, float row, uint colour, float lift) =>
            _meshBuilder.AddFaceCorner(Corner(column, row, lift), Vec3.Up, Vec2.Zero, colour);

        // A cell corner on the sheet over the terrain.
        private Vec3 Corner(float column, float row, float lift) =>
            new(column * _grid.CellSize, row * _grid.CellSize, _sheet.HeightAt(column, row) + Lift + lift);
    }
}
