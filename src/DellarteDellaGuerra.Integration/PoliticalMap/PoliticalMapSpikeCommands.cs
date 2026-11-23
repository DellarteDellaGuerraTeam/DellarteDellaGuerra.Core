using System;
using System.Collections.Generic;
using System.Globalization;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>
    ///  Throwaway go/no-go spike for the political map overlay (doc/political-map-ui-proposal.md).
    ///  <para><c>polmap.decal [material] [heightScale]</c> lays one checkerboard decal over the whole campaign map.</para>
    ///  <para><c>polmap.hide</c> removes it.</para>
    ///  <para><c>polmap.political_color &lt;value&gt;</c> calls the unused native <c>set_political_color</c>.</para>
    /// </summary>
    public static class PoliticalMapSpikeCommands
    {
        private const int TextureSize = 512;
        private const int Cells = 16;

        private static GameEntity? _entity;
        private static Decal? _decal;

        [CommandLineFunctionality.CommandLineArgumentFunction("decal", "polmap")]
        public static string ShowDecal(List<string> args)
        {
            TaleWorlds.Engine.Scene? scene = MapSceneOrNull();
            if (scene is null) return "Open the campaign map first.";

            string materialName = args.Count > 0 ? args[0] : "decal_defender_ranged_siege";
            float heightScale = args.Count > 1 ? float.Parse(args[1], CultureInfo.InvariantCulture) : 2f;

            Material? source = Material.GetFromResource(materialName);
            if (source is null) return $"Material '{materialName}' not found.";

            Hide(scene);

            Material material = source.CreateCopy();
            material.SetTexture(Material.MBTextureType.DiffuseMap, Texture.CreateFromByteArray(Checkerboard(), TextureSize, TextureSize));

            ((MapScene)Campaign.Current.MapSceneWrapper).GetMapBorders(out Vec2 min, out Vec2 max, out float maxHeight);
            Vec2 size = max - min;
            Vec2 centre = (min + max) * 0.5f;

            MatrixFrame frame = MatrixFrame.Identity;
            frame.origin = new Vec3(centre.x, centre.y, maxHeight * 0.5f);
            frame.Scale(new Vec3(size.x, size.y, maxHeight * heightScale));

            _decal = Decal.CreateDecal();
            _decal.SetMaterial(material);
            _decal.SetFactor1(0xFFFFFFFF);
            _entity = GameEntity.CreateEmpty(scene, true, true, true);
            _entity.Name = "dadg_political_map_spike";
            _entity.AddComponent(_decal);
            _entity.SetGlobalFrame(frame);
            _entity.SetVisibilityExcludeParents(true);
            scene.AddDecalInstance(_decal, "editor_set", true);

            return $"Decal '{materialName}' over [{min.x:F0},{min.y:F0}]-[{max.x:F0},{max.y:F0}], maxHeight {maxHeight:F0}. " +
                   "Top-left cell should be RED, its right neighbour BLUE, a GREEN cross marks the centre.";
        }

        // Isolates which step of polmap.decal breaks, on a decal centred on the main party:
        // 0 = stock material (exactly what vanilla does), 1 = copied material, 2 = copy + raw checkerboard,
        // 3 = 2 + CheckAndRegisterToDecalSet, 4 = copy + diffuse of material [arg3] (another stock decal),
        // 5 = 4 + register, 6 = copy + PNG checkerboard via CreateFromMemory + register,
        // 7 = 2 + SetTextureAsAlwaysValid + blocking preload + register.
        [CommandLineFunctionality.CommandLineArgumentFunction("probe", "polmap")]
        public static string Probe(List<string> args)
        {
            TaleWorlds.Engine.Scene? scene = MapSceneOrNull();
            if (scene is null) return "Open the campaign map first.";

            int step = args.Count > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 0;
            float scale = args.Count > 1 ? float.Parse(args[1], CultureInfo.InvariantCulture) : 50f;
            string materialName = args.Count > 2 ? args[2] : "decal_city_circle_a";
            string textureSource = args.Count > 3 ? args[3] : "map_cursor_valid_decal";

            Material? material = Material.GetFromResource(materialName);
            if (material is null) return $"Material '{materialName}' not found.";
            if (step >= 1) material = material.CreateCopy();

            Texture? texture = null;
            if (step == 2 || step == 3 || step == 7) texture = Texture.CreateFromByteArray(Checkerboard(), TextureSize, TextureSize);
            if (step == 4 || step == 5)
            {
                Material? source = Material.GetFromResource(textureSource);
                if (source is null) return $"Texture source material '{textureSource}' not found.";
                texture = source.GetTexture(Material.MBTextureType.DiffuseMap);
            }
            if (step == 6) texture = Texture.CreateFromMemory(Png(Checkerboard(), TextureSize, TextureSize));
            if (step == 7)
            {
                texture!.SetTextureAsAlwaysValid();
                texture.PreloadTexture(true);
            }
            if (texture is not null) material.SetTexture(Material.MBTextureType.DiffuseMap, texture);
            bool register = step == 3 || step == 5 || step == 6 || step == 7;

            Hide(scene);

            MatrixFrame frame = MatrixFrame.Identity;
            frame.origin = MobileParty.MainParty.GetPositionAsVec3();
            frame.Scale(new Vec3(scale, scale, scale));

            _decal = Decal.CreateDecal();
            _decal.SetMaterial(material);
            _decal.SetFactor1(0xFFFFFFFF);
            _entity = GameEntity.CreateEmpty(scene, true, true, true);
            _entity.Name = "dadg_political_map_spike";
            _entity.AddComponent(_decal);
            _entity.SetGlobalFrame(frame);
            _entity.SetVisibilityExcludeParents(true);
            if (register) _decal.CheckAndRegisterToDecalSet();
            scene.AddDecalInstance(_decal, "editor_set", true);

            string textureInfo = texture is null ? "stock texture" : $"texture '{texture.Name}' {texture.Width}x{texture.Height} loaded={texture.IsLoaded()}";
            return $"Probe step {step}, scale {scale}, '{materialName}', {textureInfo}, registered={register}, at main party {frame.origin.x:F1},{frame.origin.y:F1},{frame.origin.z:F1}.";
        }

        // Decal fallback: a terrain-following grid mesh around the main party carrying the runtime checkerboard.
        // polmap.mesh [material] [slot 0|1] [texture: checker|<resource name>] [tableau 0|1] [size] [cells] [lift] [blend: MBAlphaBlendMode name|-] [alpha 0-255, vcolor only]
        // Vanilla puts runtime banner textures in slot 1 (DiffuseMap2) with the use_tableau_blending flag.
        [CommandLineFunctionality.CommandLineArgumentFunction("mesh", "polmap")]
        public static string ShowMesh(List<string> args)
        {
            TaleWorlds.Engine.Scene? scene = MapSceneOrNull();
            if (scene is null) return "Open the campaign map first.";

            string materialName = args.Count > 0 ? args[0] : "editor_map_border";
            var slot = (Material.MBTextureType)(args.Count > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 1);
            string textureName = args.Count > 2 ? args[2] : "checker";
            bool tableau = args.Count > 3 && args[3] == "1";
            float size = args.Count > 4 ? float.Parse(args[4], CultureInfo.InvariantCulture) : 50f;
            int cells = args.Count > 5 ? int.Parse(args[5], CultureInfo.InvariantCulture) : 32;
            float lift = args.Count > 6 ? float.Parse(args[6], CultureInfo.InvariantCulture) : 0.3f;
            string blendName = args.Count > 7 ? args[7] : "-";
            uint alpha = args.Count > 8 ? uint.Parse(args[8], CultureInfo.InvariantCulture) : 90u;

            // "mm:<metamesh>" borrows the first submesh's material, e.g. mm:campaign_banner_small (the map party banner).
            Material? source = materialName.StartsWith("mm:")
                ? MetaMesh.GetCopy(materialName.Substring(3), true, false)?.GetMeshAtIndex(0)?.GetMaterial()
                : Material.GetFromResource(materialName);
            if (source is null) return $"Material '{materialName}' not found.";
            string before = $"slot0='{source.GetTexture(Material.MBTextureType.DiffuseMap)?.Name}' slot1='{source.GetTexture(Material.MBTextureType.DiffuseMap2)?.Name}'";
            Material material = source.CreateCopy();
            Material.MBAlphaBlendMode stockBlend = material.GetAlphaBlendMode();
            if (blendName != "-") material.SetAlphaBlendMode((Material.MBAlphaBlendMode)Enum.Parse(typeof(Material.MBAlphaBlendMode), blendName, true));
            // "vcolor": no texture, the checker goes into per-vertex colours (vertex_color_mat), translucency via entity alpha.
            // "tint": no texture, the whole mesh is tinted red through Mesh.Color at 50% alpha.
            bool tint = textureName == "tint";
            bool vertexColor = textureName == "vcolor" || tint;
            Texture? texture = vertexColor ? null : textureName switch
            {
                "checker" => Texture.CreateFromByteArray(Checkerboard(), TextureSize, TextureSize),
                "png" => Texture.CreateFromMemory(Png(Checkerboard(), TextureSize, TextureSize)),
                "pngfile" => CheckerFromPlatformPath(),
                "pngload" => Texture.LoadTextureFromPath(CheckerFileName, WriteCheckerPng()),
                _ => Texture.GetFromResource(textureName)
            };
            string textureInfo = tint ? "mesh tint" : "vertex colours";
            if (!vertexColor)
            {
                if (texture is null) return $"Texture '{textureName}' not found.";
                texture.PreloadTexture(true);
                textureInfo = $"texture '{texture.Name}' {texture.Width}x{texture.Height} loaded={texture.IsLoaded()}";
                material.SetTexture(slot, texture);
            }
            if (tableau)
            {
                uint flag = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending", true);
                material.SetShaderFlags(material.GetShaderFlags() | flag);
            }

            Hide(scene);

            Vec3 centre = MobileParty.MainParty.GetPositionAsVec3();
            float step = size / cells;
            var corners = new Vec3[cells + 1, cells + 1];
            for (int i = 0; i <= cells; i++)
            for (int j = 0; j <= cells; j++)
            {
                var p = new Vec2(centre.x - size / 2 + i * step, centre.y - size / 2 + j * step);
                corners[i, j] = p.ToVec3(scene.GetTerrainHeight(p) + lift);
            }
            Vec3 Corner(int i, int j) => corners[i, j];
            Vec2 Uv(int i, int j) => new Vec2((float)i / cells, 1f - (float)j / cells);
            uint CellColor(int i, int j)
            {
                if (!vertexColor || tint) return uint.MaxValue;
                uint a = alpha << 24;
                if (i == cells / 2 || j == cells / 2) return a | 0x00FF00;
                return a | (((i / 2 + j / 2) % 2 == 0) ? 0xFF0000u : 0x0000FFu);
            }

            // Mesh.AddTriangle* cost grows with the mesh's size (64x64 cells: 1 s, 256x256: minutes), so the sheet is
            // split into chunk x chunk cell meshes on one entity.
            const int Chunk = 16;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            _entity = GameEntity.CreateEmpty(scene, true, true, true);
            _entity.Name = "dadg_political_map_spike";
            int meshCount = 0;
            for (int ci = 0; ci < cells; ci += Chunk)
            for (int cj = 0; cj < cells; cj += Chunk)
            {
                Mesh mesh = Mesh.CreateMesh();
                UIntPtr handle = mesh.LockEditDataWrite();
                for (int i = ci; i < Math.Min(ci + Chunk, cells); i++)
                for (int j = cj; j < Math.Min(cj + Chunk, cells); j++)
                {
                    uint c = CellColor(i, j);
                    mesh.AddTriangleWithVertexColors(Corner(i, j), Corner(i + 1, j), Corner(i + 1, j + 1), Uv(i, j), Uv(i + 1, j), Uv(i + 1, j + 1), c, c, c, handle);
                    mesh.AddTriangleWithVertexColors(Corner(i, j), Corner(i + 1, j + 1), Corner(i, j + 1), Uv(i, j), Uv(i + 1, j + 1), Uv(i, j + 1), c, c, c, handle);
                }
                mesh.SetMaterial(material);
                if (tint)
                {
                    mesh.Color = 0xFFFF0000;
                    mesh.SetColorAlpha(128);
                }
                mesh.CullingMode = MBMeshCullingMode.None;
                if (!vertexColor)
                {
                    mesh.ComputeNormals();
                    mesh.ComputeTangents();
                }
                mesh.RecomputeBoundingBox();
                mesh.UnlockEditDataWrite(handle);
                _entity.AddMesh(mesh);
                meshCount++;
            }
            _entity.SetVisibilityExcludeParents(true);
            _entity.SetReadyToRender(true);
            long totalMs = watch.ElapsedMilliseconds;

            return $"Mesh '{materialName}' = '{source.Name}' (stock {before}, shader '{material.GetShader()?.Name}', blend {stockBlend} -> {material.GetAlphaBlendMode()}), '{textureName}' ({textureInfo}) into slot {(int)slot}, tableau={tableau}, " +
                   $"{size}m, {cells}x{cells} cells, lift {lift}, built in {totalMs} ms ({meshCount} meshes), at main party {centre.x:F1},{centre.y:F1},{centre.z:F1}.";
        }

        // polmap.camera <settlementId | x y> [distance]: moves the map camera there for screenshots.
        [CommandLineFunctionality.CommandLineArgumentFunction("camera", "polmap")]
        public static string MoveCamera(List<string> args)
        {
            SandBox.View.Map.MapScreen? screen = SandBox.View.Map.MapScreen.Instance;
            if (screen is null || args.Count == 0) return "Usage: polmap.camera <settlementId | x y> [distance], on the campaign map.";

            Vec2 position;
            int next;
            if (float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) && args.Count > 1)
            {
                position = new Vec2(x, float.Parse(args[1], CultureInfo.InvariantCulture));
                next = 2;
            }
            else
            {
                TaleWorlds.CampaignSystem.Settlements.Settlement? settlement = TaleWorlds.CampaignSystem.Settlements.Settlement.Find(args[0]);
                if (settlement is null) return $"No settlement '{args[0]}'.";
                position = settlement.GetPosition2D;
                next = 1;
            }

            screen.FastMoveCameraToPosition(new CampaignVec2(position, true));
            if (args.Count > next)
            {
                float distance = float.Parse(args[next], CultureInfo.InvariantCulture);
                typeof(SandBox.View.Map.MapCameraView)
                    .GetProperty("TargetCameraDistance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .SetValue(screen.MapCameraView, distance);
            }

            return $"Camera to ({position.x:F1}, {position.y:F1}), distance now {screen.MapCameraView.CameraDistance:F1}.";
        }

        // polmap.check_sheet [count]: the overlay sheet against the engine's terrain height at random points.
        [CommandLineFunctionality.CommandLineArgumentFunction("check_sheet", "polmap")]
        public static string CheckSheet(List<string> args)
        {
            TaleWorlds.Engine.Scene? scene = MapSceneOrNull();
            var grids = Campaign.Current?.GetCampaignBehavior<DellarteDellaGuerra.PoliticalMap.Api.Campaign.PoliticalMapGridCampaignBehavior>();
            if (scene is null || grids?.Surface is not { } surface) return "Open the campaign map once the grid is built.";
            var (grid, sheet) = surface;

            int count = args.Count > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 20000;
            var random = new Random(1);
            int below = 0;
            float worst = float.MaxValue, total = 0f;
            for (int sample = 0; sample < count; sample++)
            {
                // A random point on a random cell, against the lowest of its cell's sheet corners.
                int column = random.Next(grid.Width), row = random.Next(grid.Height);
                var point = new Vec2((column + (float)random.NextDouble()) * grid.CellSize, (row + (float)random.NextDouble()) * grid.CellSize);
                float sheetHeight = Math.Min(
                    Math.Min(sheet.HeightAt(column, row), sheet.HeightAt(column + 1, row)),
                    Math.Min(sheet.HeightAt(column, row + 1), sheet.HeightAt(column + 1, row + 1)));
                float gap = sheetHeight - scene.GetTerrainHeight(point);
                if (gap < 0) below++;
                worst = Math.Min(worst, gap);
                total += gap;
            }

            return $"{count} points: sheet below terrain at {below}, smallest gap {worst:F3} m, mean gap {total / count:F3} m.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("hide", "polmap")]
        public static string HideDecal(List<string> args)
        {
            TaleWorlds.Engine.Scene? scene = MapSceneOrNull();
            if (scene is null) return "Open the campaign map first.";
            Hide(scene);
            return "Removed.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("political_color", "polmap")]
        public static string PoliticalColor(List<string> args)
        {
            TaleWorlds.Engine.Scene? scene = MapSceneOrNull();
            if (scene is null) return "Open the campaign map first.";
            if (args.Count < 1) return "Usage: polmap.political_color <value>";

            // The only managed wrapper of the native set_political_color is misnamed.
            MBMapScene.GetGlobalIlluminationOfString(scene, string.Join(" ", args));
            return $"set_political_color('{string.Join(" ", args)}') called.";
        }

        private static void Hide(TaleWorlds.Engine.Scene scene)
        {
            if (_decal is not null) scene.RemoveDecalInstance(_decal, "editor_set");
            _entity?.Remove(0);
            _decal = null;
            _entity = null;
        }

        private static TaleWorlds.Engine.Scene? MapSceneOrNull() => (Campaign.Current?.MapSceneWrapper as MapScene)?.Scene;

        // RGBA8, row 0 first. Alternating red/blue cells with half alpha, and a green centre cross,
        // so channel order, orientation, alpha and the decal's extent can all be read off the screen.
        private static byte[] Checkerboard()
        {
            var data = new byte[TextureSize * TextureSize * 4];
            int cell = TextureSize / Cells;
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                int i = (y * TextureSize + x) * 4;
                bool cross = System.Math.Abs(x - TextureSize / 2) < 4 || System.Math.Abs(y - TextureSize / 2) < 4;
                bool red = (x / cell + y / cell) % 2 == 0;
                data[i] = (byte)(cross ? 0 : red ? 255 : 0);
                data[i + 1] = (byte)(cross ? 255 : 0);
                data[i + 2] = (byte)(cross ? 0 : red ? 0 : 255);
                data[i + 3] = 128;
            }
            return data;
        }

        private const string CheckerFileName = "dadg_polmap_checker.png";

        // Writes the checkerboard PNG next to the module's bin folder and returns that directory.
        private static string WriteCheckerPng()
        {
            string directory = System.IO.Path.Combine(TaleWorlds.Library.BasePath.Name, "Modules", "DellarteDellaGuerra.Core") + "/";
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, CheckerFileName), Png(Checkerboard(), TextureSize, TextureSize));
            return directory;
        }

        private static Texture CheckerFromPlatformPath()
        {
            WriteCheckerPng();
            var directory = new PlatformDirectoryPath(PlatformFileType.Application, "Modules/DellarteDellaGuerra.Core/");
            return Texture.CreateTextureFromPath(new PlatformFilePath(directory, CheckerFileName));
        }

        // Minimal RGBA8 PNG: filter 0 rows in stored (uncompressed) deflate blocks. netstandard2.0 has no image encoder.
        private static byte[] Png(byte[] rgba, int width, int height)
        {
            var raw = new byte[(width * 4 + 1) * height];
            for (int y = 0; y < height; y++)
                System.Buffer.BlockCopy(rgba, y * width * 4, raw, y * (width * 4 + 1) + 1, width * 4);

            var zlib = new System.IO.MemoryStream();
            zlib.WriteByte(0x78);
            zlib.WriteByte(0x01);
            for (int offset = 0; offset < raw.Length; offset += 65535)
            {
                int length = System.Math.Min(65535, raw.Length - offset);
                zlib.WriteByte((byte)(offset + length == raw.Length ? 1 : 0));
                zlib.WriteByte((byte)length);
                zlib.WriteByte((byte)(length >> 8));
                zlib.WriteByte((byte)~length);
                zlib.WriteByte((byte)(~length >> 8));
                zlib.Write(raw, offset, length);
            }
            uint a = 1, b = 0;
            foreach (byte v in raw)
            {
                a = (a + v) % 65521;
                b = (b + a) % 65521;
            }
            WriteBigEndian(zlib, (b << 16) | a);

            var png = new System.IO.MemoryStream();
            png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var header = new System.IO.MemoryStream();
            WriteBigEndian(header, (uint)width);
            WriteBigEndian(header, (uint)height);
            header.Write(new byte[] { 8, 6, 0, 0, 0 }, 0, 5);
            WriteChunk(png, "IHDR", header.ToArray());
            WriteChunk(png, "IDAT", zlib.ToArray());
            WriteChunk(png, "IEND", new byte[0]);
            return png.ToArray();
        }

        private static void WriteChunk(System.IO.Stream stream, string type, byte[] data)
        {
            WriteBigEndian(stream, (uint)data.Length);
            byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes, 0, 4);
            stream.Write(data, 0, data.Length);
            uint crc = 0xFFFFFFFF;
            foreach (byte v in typeBytes) crc = Crc(crc, v);
            foreach (byte v in data) crc = Crc(crc, v);
            WriteBigEndian(stream, ~crc);
        }

        private static uint Crc(uint crc, byte value)
        {
            crc ^= value;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            return crc;
        }

        private static void WriteBigEndian(System.IO.Stream stream, uint value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }
    }
}
