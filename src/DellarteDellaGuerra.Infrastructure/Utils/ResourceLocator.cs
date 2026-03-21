using System.Collections.Generic;
using System.IO;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
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
        private static readonly string ConfigFolderName = "config";

        /**
         * <summary>
         *     Gets the path to the log folder.
         *     Its existence is checked.
         * </summary>
         * <returns>
         *     The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetLogFolderPath()
        {
            return GetResourceFromModModules("log");
        }

        /**
         * <summary>
         *     Gets the path to the configuration folder.
         *     Its existence is checked.
         * </summary>
         * <returns>
         *     The first found among all the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetConfigurationFolderPath()
        {
            return GetResourceFromModModules(ConfigFolderName);
        }
        
        /**
         * <summary>
         * Gets the path to the configuration file.
         * </summary>
         * <returns>
         * The first found among all the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetConfigurationFilePath(string configFileName)
        {
            return GetResourceFromModModules(Path.Combine(ConfigFolderName, configFileName));
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
         * Gets the path to the cannons.xml file in the ModuleData/CustomXml folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetCannonXmlFilePath()
        {
            return GetCustomXmlFile("cannons.xml");
        }

        /**
         * <summary>
         * Gets the path to the cannons.xml file in the ModuleData/CustomXml folder with logging.
         * Its existence is checked.
         * </summary>
         * <param name="logger">The logger to use for missing file warnings</param>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetCannonXmlFilePath(ILogger? logger)
        {
            var path = GetCustomXmlFile("cannons.xml");
            if (path == null && logger != null)
            {
                logger.Warn("cannons.xml file not found in any module's ModuleData/CustomXml folder");
            }
            return path;
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

        /**
         * <summary>
         * Gets the path to the soundtrack.xml file in the music folder.
         * Its existence is checked.
         * </summary>
         * <returns>
         * The first found among all of the mod's modules or null if not found.
         * </returns>
         */
        public static string? GetSoundtrackFilePath()
        {
            return GetResourceFromModModules(Path.Combine("music", "soundtrack.xml"));
        }

        private static string? GetCustomXmlFile(string filename)
        {
            return GetModuleDataFile(Path.Combine("CustomXml", filename));
        }
        
        private static string? GetModuleDataFile(string filename)
        {
            return GetResourceFromModModules(Path.Combine("ModuleData", filename));
        }

        private static string? GetResourceFromModModules(string pathRelativeToModFolder)
        {
            return GetModModuleIds()
                .Select(moduleId => Path.Combine(ModuleHelper.GetModuleFullPath(moduleId), pathRelativeToModFolder))
                .FirstOrDefault(path => File.Exists(path) || Directory.Exists(path));
        }

        private static List<string> GetModModuleIds()
        {
            var moduleIds = ModuleHelper.GetModules().Select(module => module.Id);

            return ModuleHelper.GetSortedModules(moduleIds.ToArray()).Select(module => module.Id)
                .Where(moduleId => moduleId.Contains(ModuleIdHelper.GetModuleIdPrefix())).ToList();
        }
    }
}

