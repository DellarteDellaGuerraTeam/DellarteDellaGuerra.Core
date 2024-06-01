using System;
using System.IO;
using System.Linq;
using TaleWorlds.ModuleManager;

namespace DellarteDellaGuerra.Infrastructure.Utils
{
    /**
     * <summary>
     *  A helper class for locating mod folders and files.
     * </summary>
     */
    public static class ResourceLocator
    {
        /**
         * <summary>
         * Gets the path to the mod's folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found with a valid submodule id or null if all are invalid.
         * </returns>
         */
        public static string? GetModuleFolder()
        {
            return GetResourceFromModModules("");
        }

        /**
         * <summary>
         * Gets the path to the configuration folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetConfigurationFolderPath()
        {
            return GetResourceFromModModules("config");
        }

        /**
         * <summary>
         * Gets the path to the settlements.xml file in the ModuleData folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetSettlementsFilePath()
        {
            return GetModuleDataFile("settlements.xml");
        }

        /**
         * <summary>
         * Gets the path to the settlements_distance_cache.bin file in the ModuleData folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetSettlementDistanceCacheFilePath()
        {
            return GetModuleDataFile("settlements_distance_cache.bin");
        }

        /**
         * <summary>
         * Gets the path to the dadg_battle_scenes.xml file in the ModuleData folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetBattleScenesFilePath()
        {
            return GetModuleDataFile("dadg_battle_scenes.xml");
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
}

