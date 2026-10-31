using System.IO;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Port;
using TaleWorlds.ModuleManager;

namespace DellarteDellaGuerra.Infrastructure.PoliticalMap;

/**
 * <summary>
 * The campaign map's terrain, read from the same Main_map scene the game loads: the last active
 * module that ships <c>SceneObj/Main_map/scene.xscene</c>, as vanilla <c>MapScene</c> picks it.
 * </summary>
 * <remarks>
 * Only files are read, never the engine, so this runs on a worker thread. Both files ship with
 * the map module, so the political map follows the coastline the player is actually given.
 * </remarks>
 */
public class MainMapTerrain : IPoliticalMapTerrain
{
    private const string SceneName = "Main_map";

    public PoliticalMapHeightField Load()
    {
        string sceneFolder = ModuleHelper.GetActiveModules()
            .Where(module => module.IsActive)
            .Select(module => Path.Combine(module.FolderPath, "SceneObj", SceneName))
            .LastOrDefault(folder => File.Exists(Path.Combine(folder, "scene.xscene")))
            ?? throw new FileNotFoundException($"No active module ships SceneObj/{SceneName}/scene.xscene.");

        return Load(sceneFolder);
    }

    /// <summary>Reads the terrain of the scene in <paramref name="sceneFolder"/>.</summary>
    public static PoliticalMapHeightField Load(string sceneFolder)
    {
        SceneTerrainTag tag;
        using (FileStream scene = File.OpenRead(Path.Combine(sceneFolder, "scene.xscene"))) tag = SceneTerrainTag.Read(scene);

        return Zgr6TerrainReader.Read(File.ReadAllBytes(Path.Combine(sceneFolder, "terrain.bin")), tag);
    }
}
