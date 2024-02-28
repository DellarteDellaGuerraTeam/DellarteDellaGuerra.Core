using System.IO;
using DellarteDellaGuerra.Utils;

namespace DellarteDellaGuerra.Configuration.Providers
{

    /**
     * <summary>
     *  A provider for the path to the logger configuration file.
     * </summary>
     */
    public class LoggerConfigPathProvider : IConfigurationProvider<string>
    {
        private const string LogConfigFileName = "nlog.config";

        /**
         * <summary>
         * Gets the path to the logger configuration file.
         * </summary>
         * <returns>
         * The path to the logger configuration file.
         * </returns>
         */
        public string Config
        {
            get => Path.Combine(ResourceLocator.GetConfigurationFolderPath(), LogConfigFileName);
        }
    }
}