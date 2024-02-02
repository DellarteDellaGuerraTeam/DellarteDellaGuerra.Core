using System.IO;

namespace DellarteDellaGuerra.Utils;

/**
 * <summary>
 *  A helper class for locating folders.
 * </summary>
 */
public static class FolderLocator
{
    /**
     * <summary>
     * Gets the module root folder.
     * </summary>
     */
    private static string GetModuleFolder()
    {
        return Path.Combine(typeof(FolderLocator).Assembly.Location, "..", "..", "..");
    }

    /**
     * <summary>
     * Gets the configuration folder.
     * </summary>
     */
    public static string GetConfigurationFolder()
    {
        return Path.Combine(GetModuleFolder(), "config");
    }
}