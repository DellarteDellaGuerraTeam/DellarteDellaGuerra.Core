using System;
using System.IO;
using System.IO.Compression;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Infrastructure.PoliticalMap;

/**
 * <summary>
 * Reads the heightmap out of a scene's terrain.bin (the ZGR6 format the scene editor writes) and
 * the height range and extent out of the scene's <c>&lt;terrain&gt;</c> tag.
 * </summary>
 * <remarks>
 * terrain.bin: 'ZGR6RTRN', u32 version, then five 20-byte section descriptors
 * ([tag][u32 count][u32 offset][u32 compressed size][u32 size]) for MIDX, HGHT, NRML, WGHT and
 * PHYM. A section starts with a table of u32 node offsets into its decompressed stream (strictly
 * increasing from 0), then a 32-byte header {u32 mode = 3, u32, u64 size, u64, u64}, 8 bytes of
 * runtime garbage and an LZ4 raw block. Each HGHT node decodes to {u32 length, a 129x129 16-bit
 * greyscale PNG}; the nodes tile the map row by row with a one-sample shared border. The same
 * layout is read by Navmesh Studio's <c>blnav/terrain.py</c>.
 * </remarks>
 */
public static class Zgr6TerrainReader
{
    private const int DescriptorsOffset = 0x0C;
    private const int DescriptorSize = 20;
    private const int SectionCount = 5;

    /// <summary>The square heightmap of the scene's terrain, row-major from the map's y = 0 edge up.</summary>
    public static PoliticalMapHeightField Read(byte[] terrainBin, SceneTerrainTag terrain)
    {
        if (terrainBin.Length < DescriptorsOffset + SectionCount * DescriptorSize
            || ReadAscii(terrainBin, 0, 8) != "ZGR6RTRN")
        {
            throw new InvalidDataException("Not a ZGR6 terrain.bin.");
        }

        for (int i = 0; i < SectionCount; i++)
        {
            int descriptor = DescriptorsOffset + i * DescriptorSize;
            if (ReadAscii(terrainBin, descriptor, 4) != "HGHT") continue;

            int offset = (int)BitConverter.ToUInt32(terrainBin, descriptor + 8);
            return Assemble(ReadSection(terrainBin, offset, out int[] nodeOffsets), nodeOffsets, terrain);
        }

        throw new InvalidDataException("terrain.bin has no HGHT section.");
    }

    private static PoliticalMapHeightField Assemble(byte[] stream, int[] nodeOffsets, SceneTerrainTag terrain)
    {
        int nodesPerSide = (int)Math.Round(Math.Sqrt(nodeOffsets.Length));
        if (nodesPerSide * nodesPerSide != nodeOffsets.Length)
        {
            throw new InvalidDataException($"{nodeOffsets.Length} heightmap nodes is not a square.");
        }

        ushort[]? samples = null;
        int side = 0;
        int tile = 0;
        for (int node = 0; node < nodeOffsets.Length; node++)
        {
            int start = nodeOffsets[node];
            int pngLength = (int)BitConverter.ToUInt32(stream, start);
            ushort[] tileSamples = DecodeGreyscale16Png(stream, start + 4, pngLength, out int width, out int height);
            if (samples is null)
            {
                tile = width;
                side = (tile - 1) * nodesPerSide + 1;
                samples = new ushort[side * side];
            }

            if (width != tile || height != tile) throw new InvalidDataException($"Heightmap node {node} is {width}x{height}.");

            int top = node / nodesPerSide * (tile - 1);
            int left = node % nodesPerSide * (tile - 1);
            for (int row = 0; row < tile; row++)
            {
                Array.Copy(tileSamples, row * tile, samples, (top + row) * side + left, tile);
            }
        }

        return new PoliticalMapHeightField(side, samples!, terrain.MinHeight, terrain.MaxHeight, terrain.MapSize);
    }

    private static byte[] ReadSection(byte[] data, int offset, out int[] nodeOffsets)
    {
        var table = new System.Collections.Generic.List<int>();
        int position = offset;
        while (true)
        {
            int value = (int)BitConverter.ToUInt32(data, position);
            if ((table.Count == 0 && value != 0) || (table.Count > 0 && value <= table[table.Count - 1])) break;
            table.Add(value);
            position += 4;
        }

        if (BitConverter.ToUInt32(data, position) != 3) throw new InvalidDataException("Unexpected terrain stream mode.");

        long size = BitConverter.ToInt64(data, position + 8);
        nodeOffsets = table.ToArray();
        return Lz4Raw(data, position + 32 + 8, (int)size);
    }

    /// <summary>Decodes an LZ4 raw block until it has produced <paramref name="size"/> bytes; trailing input is ignored.</summary>
    internal static byte[] Lz4Raw(byte[] input, int position, int size)
    {
        var output = new byte[size];
        int written = 0;
        while (written < size)
        {
            byte token = input[position++];
            int literals = token >> 4;
            if (literals == 15)
            {
                byte more;
                do literals += more = input[position++];
                while (more == 255);
            }

            Buffer.BlockCopy(input, position, output, written, Math.Min(literals, size - written));
            position += literals;
            written += literals;
            if (written >= size) break;

            int matchOffset = input[position] | (input[position + 1] << 8);
            position += 2;
            if (matchOffset == 0 || matchOffset > written) throw new InvalidDataException("Corrupt LZ4 block.");

            int match = token & 15;
            if (match == 15)
            {
                byte more;
                do match += more = input[position++];
                while (more == 255);
            }

            match = Math.Min(match + 4, size - written);
            // Byte by byte: a match may overlap the bytes it is copying.
            for (int from = written - matchOffset; match > 0; match--) output[written++] = output[from++];
        }

        return output;
    }

    /// <summary>A non-interlaced 16-bit greyscale PNG's samples, row by row from its first row.</summary>
    internal static ushort[] DecodeGreyscale16Png(byte[] data, int start, int length, out int width, out int height)
    {
        int end = start + length;
        int position = start + 8;
        width = height = 0;
        using var idat = new MemoryStream();
        while (position + 8 <= end)
        {
            int chunkLength = ReadBigEndianInt(data, position);
            string type = ReadAscii(data, position + 4, 4);
            int body = position + 8;
            if (type == "IHDR")
            {
                width = ReadBigEndianInt(data, body);
                height = ReadBigEndianInt(data, body + 4);
                if (data[body + 8] != 16 || data[body + 9] != 0 || data[body + 12] != 0)
                {
                    throw new InvalidDataException("Heightmap PNG is not non-interlaced 16-bit greyscale.");
                }
            }
            else if (type == "IDAT")
            {
                idat.Write(data, body, chunkLength);
            }
            else if (type == "IEND")
            {
                break;
            }

            position = body + chunkLength + 4;
        }

        int stride = width * 2;
        var raw = new byte[(stride + 1) * height];
        // Past the 2-byte zlib header, the stream is raw deflate.
        idat.Position = 2;
        using (var inflater = new DeflateStream(idat, CompressionMode.Decompress))
        {
            int read = 0;
            while (read < raw.Length)
            {
                int count = inflater.Read(raw, read, raw.Length - read);
                if (count == 0) throw new InvalidDataException("Truncated heightmap PNG.");
                read += count;
            }
        }

        var samples = new ushort[width * height];
        var previous = new byte[stride];
        var current = new byte[stride];
        for (int row = 0; row < height; row++)
        {
            int rowStart = row * (stride + 1);
            Unfilter(raw[rowStart], raw, rowStart + 1, current, previous, bytesPerPixel: 2);
            for (int column = 0; column < width; column++)
            {
                samples[row * width + column] = (ushort)((current[2 * column] << 8) | current[2 * column + 1]);
            }

            (previous, current) = (current, previous);
        }

        return samples;
    }

    private static void Unfilter(byte filter, byte[] raw, int offset, byte[] current, byte[] previous, int bytesPerPixel)
    {
        for (int i = 0; i < current.Length; i++)
        {
            int left = i >= bytesPerPixel ? current[i - bytesPerPixel] : 0;
            int up = previous[i];
            int upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
            int predictor = filter switch
            {
                0 => 0,
                1 => left,
                2 => up,
                3 => (left + up) / 2,
                4 => Paeth(left, up, upLeft),
                _ => throw new InvalidDataException($"Unknown PNG filter {filter}."),
            };
            current[i] = (byte)(raw[offset + i] + predictor);
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static int ReadBigEndianInt(byte[] data, int position) =>
        (data[position] << 24) | (data[position + 1] << 16) | (data[position + 2] << 8) | data[position + 3];

    private static string ReadAscii(byte[] data, int position, int length) =>
        System.Text.Encoding.ASCII.GetString(data, position, length);
}
