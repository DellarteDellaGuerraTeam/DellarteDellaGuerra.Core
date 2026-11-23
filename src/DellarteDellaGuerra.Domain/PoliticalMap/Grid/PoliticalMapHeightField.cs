using System;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Grid
{
    /// <summary>
    /// The campaign map's terrain heightmap: a square of 16-bit samples spread evenly from one map
    /// corner to the other, row-major from the map's y = 0 edge up. A sample of 0 is the scene's
    /// minimum height and 65535 its maximum.
    /// </summary>
    public sealed class PoliticalMapHeightField
    {
        public PoliticalMapHeightField(int side, ushort[] samples, float minHeight, float maxHeight, float mapSize)
        {
            if (side < 2) throw new ArgumentOutOfRangeException(nameof(side), side, "A heightmap needs at least 2x2 samples.");
            if (samples.Length != side * side)
            {
                throw new ArgumentException($"{samples.Length} samples is not a {side}x{side} heightmap.", nameof(samples));
            }

            Side = side;
            Samples = samples;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            MapSize = mapSize;
        }

        /// <summary>Samples along each edge.</summary>
        public int Side { get; }

        public ushort[] Samples { get; }

        public float MinHeight { get; }

        public float MaxHeight { get; }

        /// <summary>The map's width and height in meters.</summary>
        public float MapSize { get; }

        /// <summary>The height at a map position, interpolated bilinearly between the four samples around it.</summary>
        public float HeightAt(double x, double y)
        {
            double scale = (Side - 1) / (double)MapSize;
            double column = Clamp(x * scale);
            double row = Clamp(y * scale);
            int column0 = Math.Min((int)column, Side - 2);
            int row0 = Math.Min((int)row, Side - 2);
            double dx = column - column0;
            double dy = row - row0;

            double bottom = Height(row0, column0) * (1 - dx) + Height(row0, column0 + 1) * dx;
            double top = Height(row0 + 1, column0) * (1 - dx) + Height(row0 + 1, column0 + 1) * dx;
            return (float)(bottom * (1 - dy) + top * dy);
        }

        // Each sample is widened to a float height first, as the engine does.
        private float Height(int row, int column) =>
            MinHeight + Samples[row * Side + column] / 65535f * (MaxHeight - MinHeight);

        private double Clamp(double index) => index < 0 ? 0 : index > Side - 1 ? Side - 1 : index;
    }
}
