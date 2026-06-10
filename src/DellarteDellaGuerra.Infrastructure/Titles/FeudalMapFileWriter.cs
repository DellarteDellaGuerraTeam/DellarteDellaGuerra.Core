using System.IO;
using System.Text;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * Writes rendered feudal map text to files in a given output folder.
 * <br/>
 * <br/>
 * Plain IO only - the rendering itself is done by the domain's
 * <see cref="IRenderFeudalMapUseCase"/>.
 * </summary>
 */
public class FeudalMapFileWriter
{
    private const string MapFileName = "feudal-map.md";

    private readonly string _outputDirectoryPath;

    public FeudalMapFileWriter(string outputDirectoryPath)
    {
        _outputDirectoryPath = outputDirectoryPath;
    }

    /**
     * <summary>
     * Creates a writer targeting the mod's log folder, or null when no module
     * provides one.
     * </summary>
     */
    public static FeudalMapFileWriter? CreateInLogFolder()
    {
        string? logFolderPath = ResourceLocator.GetLogFolderPath();
        return logFolderPath is null ? null : new FeudalMapFileWriter(logFolderPath);
    }

    /**
     * <summary>
     * Writes the given content to the given file name in the output folder,
     * creating the folder when needed.
     * </summary>
     */
    public void Write(string fileName, string content)
    {
        Directory.CreateDirectory(_outputDirectoryPath);
        File.WriteAllText(Path.Combine(_outputDirectoryPath, fileName), content);
    }

    /**
     * <summary>
     * Writes "feudal-map.md" containing the markdown rendering of the map followed by
     * a mermaid fenced code block of its graph rendering.
     * </summary>
     */
    public void WriteMap(FeudalMap map, IRenderFeudalMapUseCase renderer)
    {
        var content = new StringBuilder()
            .Append(renderer.RenderMarkdown(map))
            .Append('\n')
            .Append("```mermaid\n")
            .Append(renderer.RenderMermaid(map))
            .Append("```\n")
            .ToString();

        Write(MapFileName, content);
    }
}
