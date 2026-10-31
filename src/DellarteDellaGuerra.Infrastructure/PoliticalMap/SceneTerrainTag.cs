using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DellarteDellaGuerra.Infrastructure.PoliticalMap;

/**
 * <summary>
 * What a scene.xscene's <c>&lt;terrain&gt;</c> tag says about its heightmap: the height a sample of
 * 0 and of 65535 stand for, and the terrain's extent (node count times node size).
 * </summary>
 */
public sealed record SceneTerrainTag(float MinHeight, float MaxHeight, float MapSize)
{
    private const string Opening = "<terrain ";

    /// <summary>
    /// Scans the scene for its terrain tag. The campaign map's scene runs to about 20 MB with the
    /// tag near its end, so it is streamed rather than loaded whole.
    /// </summary>
    public static SceneTerrainTag Read(Stream scene)
    {
        using var reader = new StreamReader(scene, Encoding.UTF8);
        var buffer = new char[1 << 16];
        var window = new StringBuilder();
        int read;
        int tagStart = -1;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            window.Append(buffer, 0, read);
            string text = window.ToString();
            if (tagStart < 0)
            {
                tagStart = text.IndexOf(Opening, StringComparison.Ordinal);
                if (tagStart < 0)
                {
                    // Keep just enough to catch a tag split across two reads.
                    window.Remove(0, Math.Max(0, window.Length - Opening.Length));
                    continue;
                }
            }

            int tagEnd = text.IndexOf('>', tagStart);
            if (tagEnd >= 0) return Parse(text.Substring(tagStart, tagEnd - tagStart + 1));
        }

        throw new InvalidDataException("The scene has no <terrain> tag.");
    }

    internal static SceneTerrainTag Parse(string tag)
    {
        float Attribute(string name)
        {
            Match match = Regex.Match(tag, $"\\b{name}=\"([^\"]+)\"");
            if (!match.Success) throw new InvalidDataException($"The scene's <terrain> tag has no {name}.");
            return float.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        return new SceneTerrainTag(
            Attribute("min_height"),
            Attribute("max_height"),
            Attribute("node_dimension_x") * Attribute("node_size"));
    }
}
