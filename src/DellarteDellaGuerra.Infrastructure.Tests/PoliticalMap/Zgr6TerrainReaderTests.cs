using System.IO.Compression;
using System.Text;
using DellarteDellaGuerra.Infrastructure.PoliticalMap;

namespace DellarteDellaGuerra.Infrastructure.Tests.PoliticalMap;

public class Zgr6TerrainReaderTests
{
    [Fact]
    public void AnLz4BlockCopiesLiteralsThenRepeatsAnOverlappingMatch()
    {
        // "ab" as literals, then a 6-byte match from 2 back that overlaps itself, then trailing input.
        byte[] block = { 0x22, (byte)'a', (byte)'b', 0x02, 0x00, 0xFF };

        Assert.Equal("abababab", Encoding.ASCII.GetString(Zgr6TerrainReader.Lz4Raw(block, 0, 8)));
    }

    [Fact]
    public void AGreyscale16PngDecodesBigEndianSamplesThroughItsRowFilters()
    {
        // Row 0 unfiltered, row 1 'Up' filtered: both rows end up 0x0102, 0xFFFE.
        byte[] rows = { 0, 0x01, 0x02, 0xFF, 0xFE, 2, 0, 0, 0, 0 };
        byte[] png = Png(2, 2, rows);

        ushort[] samples = Zgr6TerrainReader.DecodeGreyscale16Png(png, 0, png.Length, out int width, out int height);

        Assert.Equal((2, 2), (width, height));
        Assert.Equal(new ushort[] { 0x0102, 0xFFFE, 0x0102, 0xFFFE }, samples);
    }

    [Fact]
    public void ASceneTerrainTagGivesTheHeightRangeAndTheMapExtent()
    {
        string scene = "<scene><terrain enabled=\"true\" node_dimension_x=\"16\" node_dimension_y=\"16\" "
                       + "node_size=\"70.000\" min_height=\"0.000\" max_height=\"15.686\"></terrain></scene>";

        SceneTerrainTag tag = SceneTerrainTag.Read(new MemoryStream(Encoding.UTF8.GetBytes(scene)));

        Assert.Equal(new SceneTerrainTag(0f, 15.686f, 1120f), tag);
    }

    private static byte[] Png(int width, int height, byte[] filteredRows)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A });
        var header = new byte[13];
        WriteBigEndian(header, 0, width);
        WriteBigEndian(header, 4, height);
        header[8] = 16;
        Chunk(output, "IHDR", header);
        using var zlib = new MemoryStream();
        using (var deflate = new ZLibStream(zlib, CompressionLevel.Optimal, leaveOpen: true)) deflate.Write(filteredRows);
        Chunk(output, "IDAT", zlib.ToArray());
        Chunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    // The reader does not check CRCs, so they are left zero.
    private static void Chunk(Stream output, string type, byte[] body)
    {
        var length = new byte[4];
        WriteBigEndian(length, 0, body.Length);
        output.Write(length);
        output.Write(Encoding.ASCII.GetBytes(type));
        output.Write(body);
        output.Write(new byte[4]);
    }

    private static void WriteBigEndian(byte[] data, int position, int value)
    {
        data[position] = (byte)(value >> 24);
        data[position + 1] = (byte)(value >> 16);
        data[position + 2] = (byte)(value >> 8);
        data[position + 3] = (byte)value;
    }
}
