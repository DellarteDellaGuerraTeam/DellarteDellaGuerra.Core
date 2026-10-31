using System;
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
    /// <c>vertex_color_mat</c> switched to real alpha blending. A territory is rebuilt into a new
    /// hidden entity a chunk at a time. Affected territories swap together after the batch finishes,
    /// so both sides of a smoothed frontier always use the same geometry.
    /// </summary>
    /// <remarks>
    /// The stock material blends <c>AddAlpha</c> (logged by the 2026-10-28 mesh spike): the vertex
    /// colour times its alpha is added to the terrain, so black adds nothing and saturated colours
    /// wash out to pastel over bright ground. <c>Modulate</c> is the source-alpha over
    /// inverse-source-alpha blend, so a fill at the wash opacity covers that share of the terrain.
    /// Main thread only: meshes are built through the engine. The renderer belongs to one scene;
    /// when the campaign map scene goes, its entities go with it and the renderer is dropped
    /// without touching them.
    /// </remarks>
    public class PoliticalMapRenderer
    {
        private const string MaterialName = "vertex_color_mat";
        private const float Lift = 0.3f;

        private readonly PoliticalMapGrid _grid;
        private readonly PoliticalMapSheet _sheet;
        private readonly Material _material;
        private readonly Dictionary<string, GameEntity> _shown = new();
        private readonly Dictionary<string, GameEntity> _building = new();

        private PoliticalMapZoomBand _band = PoliticalMapZoomBand.Mid;
        private bool _visible = true;

        public PoliticalMapRenderer(TaleWorlds.Engine.Scene scene, PoliticalMapGrid grid, PoliticalMapSheet sheet)
        {
            Scene = scene;
            _grid = grid;
            _sheet = sheet;
            _material = Material.GetFromResource(MaterialName).CreateCopy();
            _material.SetAlphaBlendMode(Material.MBAlphaBlendMode.Modulate);
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
            Mesh mesh = Mesh.CreateMesh();
            UIntPtr handle = mesh.LockEditDataWrite();
            foreach (PoliticalMapQuad quad in chunk.Quads)
            {
                int left = quad.Column, right = quad.Column + quad.Width, bottom = quad.Row, top = quad.Row + quad.Height;
                Vec3 a = Corner(left, bottom), b = Corner(right, bottom), c = Corner(right, top), d = Corner(left, top);
                uint colour = quad.Color;
                mesh.AddTriangleWithVertexColors(a, b, c, Vec2.Zero, Vec2.Zero, Vec2.Zero, colour, colour, colour, handle);
                mesh.AddTriangleWithVertexColors(a, c, d, Vec2.Zero, Vec2.Zero, Vec2.Zero, colour, colour, colour, handle);
            }

            foreach (PoliticalMapTriangle triangle in chunk.Triangles)
            {
                Vec3 a = Corner(triangle.A.Column, triangle.A.Row);
                Vec3 b = Corner(triangle.B.Column, triangle.B.Row);
                Vec3 c = Corner(triangle.C.Column, triangle.C.Row);
                uint colour = triangle.Color;
                mesh.AddTriangleWithVertexColors(a, b, c, Vec2.Zero, Vec2.Zero, Vec2.Zero, colour, colour, colour, handle);
            }

            mesh.SetMaterial(_material);
            mesh.CullingMode = MBMeshCullingMode.None;
            mesh.RecomputeBoundingBox();
            mesh.UnlockEditDataWrite(handle);
            return mesh;
        }

        // A cell corner on the sheet over the terrain.
        private Vec3 Corner(float column, float row) =>
            new(column * _grid.CellSize, row * _grid.CellSize, _sheet.HeightAt(column, row) + Lift);
    }
}
