using System;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>Softens the unlit overlay at night without changing its wash opacity.</summary>
    public static class PoliticalMapLighting
    {
        public static uint FactorFor(float hour)
        {
            if (float.IsNaN(hour) || float.IsInfinity(hour)) return 0xFFFFFFFFu;
            hour = (hour % 24f + 24f) % 24f;
            float daylight = Math.Max(0f, Math.Min(1f, Math.Min((hour - 6f) / 2f, (20f - hour) / 2f)));
            daylight = daylight * daylight * (3f - 2f * daylight);
            uint channel = (uint)Math.Round(255f * (0.65f + 0.35f * daylight));
            return 0xFF000000u | channel << 16 | channel << 8 | channel;
        }
    }
}
