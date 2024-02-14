using System;
using System.IO;
using System.Linq;
using DellarteDellaGuerra.Logging;
using NLog;
using TaleWorlds.ModuleManager;

namespace DellarteDellaGuerra.Utils;

/**
 * <summary>
 *  A helper class for locating mod folders and files.
 * </summary>
 */
public static class ResourceLocator
{
    /**
     * <summary>
     * Gets the existing path to the mod's folder.
     * </summary>
     * <returns>
     * The first found where the submodule id is valid, return null otherwise.
     * </returns>
     */
    public static string? GetModuleFolder()
    {
        return GetResourceFromModModules("");
    }

    /**
     * <summary>
     * Gets the existing path to the configuration folder.
     * </summary>
     * <returns>
     * The first found among all of the mod's modules; otherwise null will be returned.
     * </returns>
     */
    public static string? GetConfigurationFolderPath()
    {
        return GetResourceFromModModules("config");
    }

    /**
     * <summary>
     * Gets the existing path to the settlements.xml file in the ModuleData folder.
     * </summary>
     * <returns>
     * The first found among all of the mod's modules; otherwise null will be returned.
     * </returns>
     */
    public static string? GetModuleDataSettlementFilePath()
    {
        return GetModuleDataFile("settlements.xml");
    }

    /**
     * <summary>
     * Gets the existing path to the settlements_distance_cache.bin file in the ModuleData folder.
     * </summary>
     * <returns>
     * The first found among all of the mod's modules; otherwise null will be returned.
     * </returns>
     */
    public static string? GetSettlementDistanceCacheFilePath()
    {
        return GetModuleDataFile("settlements_distance_cache.bin");
    }
    
    private static string? GetModuleDataFile(string filename)
    {
        return GetResourceFromModModules(Path.Combine("ModuleData", filename));
    }

    private static string? GetResourceFromModModules(string pathRelativeToModFolder)
    {
        return Enum.GetNames(typeof(ModuleId))
            .Select(moduleId => Path.Combine(ModuleHelper.GetModuleFullPath(moduleId), pathRelativeToModFolder))
            .FirstOrDefault(path => File.Exists(path) || Directory.Exists(path));
    }
}
