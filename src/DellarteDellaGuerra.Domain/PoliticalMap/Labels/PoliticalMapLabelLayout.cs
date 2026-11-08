using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>A region label as projected this frame, before it is sized and checked for room.</summary>
    /// <param name="Id">The caller's handle for the label.</param>
    /// <param name="X">The label's centre on screen, in pixels.</param>
    /// <param name="Y">The label's centre on screen, in pixels.</param>
    /// <param name="PixelRadius">The region's inscribed radius as it shows on screen, in pixels.</param>
    /// <param name="TextLength">How many characters the label's longest line has.</param>
    /// <param name="Priority">Higher wins a collision: tier, then title rank, then region size.</param>
    /// <param name="Group">
    /// What the label says (its text, or any key standing for it); null for none. A label is not
    /// repeated near one of its own group already placed (<see cref="PoliticalMapLabelLayout.RepeatDistance"/>).
    /// </param>
    /// <param name="LineCount">Number of lines in the label, for collision checks.</param>
    /// <param name="SizeIndex">Optional hierarchy-selected font step; otherwise uses region size.</param>
    public sealed record PoliticalMapLabelCandidate(
        int Id, float X, float Y, float PixelRadius, int TextLength, long Priority, string? Group = null, int LineCount = 1,
        int? SizeIndex = null);

    /// <summary>A label that is drawn this frame, in one of <see cref="PoliticalMapLabelLayout.FontSizes"/>.</summary>
    public sealed record PoliticalMapPlacedLabel(int Id, float X, float Y, int SizeIndex);

    /// <summary>
    /// Sizes region labels to their regions as they show on screen and keeps them from piling up.
    /// The font follows how big the region shows (its inscribed radius on screen at this zoom),
    /// never how long its name is: long names can wrap without shrinking their font.
    /// A region too small on screen gets no label. Labels are placed
    /// by tier, title rank and region size, each dropped when its text box overlaps one placed.
    /// A label is also dropped when one saying the same thing (same <see cref="PoliticalMapLabelCandidate.Group"/>)
    /// is already placed nearby: a kingdom split into patches, or a clan holding scattered fiefs,
    /// is named once on its biggest patch in view, not on every patch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Gauntlet cannot bind a font size, so sizes come in fixed steps, each its own text widget.
    /// </para>
    /// <para>
    /// The text box, used only to keep labels apart, is estimated from the character count. The
    /// advance is calibrated on the game's <c>GUI/GauntletUI/Fonts/Galahad/Galahad.fnt</c> (size 32,
    /// no kerning pairs): capitals average 0.58 em, lower case 0.42 em, a space 0.25 em, and real
    /// region names come to 0.36 to 0.42 em a character. <see cref="CharWidth"/> takes 0.5 em, a
    /// margin for the outline and for names heavy in capitals. The glyphs' ink, accents and
    /// descenders included, is about 0.9 em tall (<see cref="TextHeight"/>).
    /// </para>
    /// </remarks>
    public static class PoliticalMapLabelLayout
    {
        /// <summary>The font sizes a label can take, smallest first.</summary>
        public static readonly IReadOnlyList<int> FontSizes = new[] { 13, 16, 20, 25, 31, 39, 48 };

        /// <summary>Glyph advance as a share of the font size: Galahad's mixed-case mean (~0.42) plus a margin.</summary>
        public const float CharWidth = 0.5f;

        /// <summary>The text's ink height as a share of the font size.</summary>
        public const float TextHeight = 0.9f;

        /// <summary>The font size a region earns per pixel of its inscribed radius on screen.</summary>
        public const float FontPerRadius = 0.8f;

        /// <summary>
        /// How close, as a share of the screen width, a label may come to one of its own group
        /// already placed before it is dropped: measured box edge to box edge, so a long name's
        /// width counts. A third of the screen (640 px on 1920) keeps one name per kingdom or clan
        /// in a typical view: zoomed far out a whole realm fits inside that, so its outlying patches
        /// go; zoomed in, a patch a screen-third away is far enough that the first name reads as
        /// another place's, and it is named again. A patch whose kept twin is off screen is named.
        /// </summary>
        public const float RepeatDistance = 1f / 3f;

        // Between two labels' boxes, in pixels.
        private const float Gap = 4f;

        /// <summary>
        /// The largest font step a region earns from its inscribed radius on screen, or -1 when it
        /// shows too small for even the smallest. The name's length plays no part.
        /// </summary>
        public static int SizeIndexFor(float pixelRadius)
        {
            if (pixelRadius <= 0f) return -1;

            float wanted = FontPerRadius * pixelRadius;
            for (int index = FontSizes.Count - 1; index >= 0; index--)
            {
                if (FontSizes[index] <= wanted) return index;
            }

            return -1;
        }

        /// <summary>The estimated width of a text of <paramref name="textLength"/> characters, in pixels.</summary>
        public static float TextWidthOf(int fontSize, int textLength) => CharWidth * fontSize * textLength;

        /// <summary>The labels to draw this frame, in no particular order.</summary>
        public static IReadOnlyList<PoliticalMapPlacedLabel> Place(
            IReadOnlyList<PoliticalMapLabelCandidate> candidates, float screenWidth, float screenHeight)
        {
            var ordered = new List<PoliticalMapLabelCandidate>(candidates);
            ordered.Sort((a, b) => a.Priority != b.Priority ? b.Priority.CompareTo(a.Priority) : a.Id.CompareTo(b.Id));

            float repeatDistance = RepeatDistance * screenWidth;
            var placed = new List<PoliticalMapPlacedLabel>();
            var boxes = new List<Box>();
            var groups = new List<string?>();
            foreach (PoliticalMapLabelCandidate candidate in ordered)
            {
                if (candidate.TextLength <= 0) continue;
                int size = candidate.SizeIndex ?? SizeIndexFor(candidate.PixelRadius);
                if (size < 0) continue;

                Box box = BoxOf(candidate, FontSizes[size]);
                if (box.Right < 0f || box.Left > screenWidth || box.Bottom < 0f || box.Top > screenHeight) continue;
                if (boxes.Exists(other => other.Overlaps(box))) continue;
                if (candidate.Group is { } group && RepeatsNearby(group, box)) continue;

                boxes.Add(box);
                groups.Add(candidate.Group);
                placed.Add(new PoliticalMapPlacedLabel(candidate.Id, candidate.X, candidate.Y, size));
            }

            bool RepeatsNearby(string group, Box box)
            {
                for (int index = 0; index < boxes.Count; index++)
                {
                    if (string.Equals(groups[index], group, System.StringComparison.Ordinal)
                        && boxes[index].DistanceTo(box) < repeatDistance) return true;
                }

                return false;
            }

            return placed;
        }

        private static Box BoxOf(PoliticalMapLabelCandidate candidate, int fontSize)
        {
            float halfWidth = TextWidthOf(fontSize, candidate.TextLength) / 2f + Gap;
            float halfHeight = (TextHeight + System.Math.Max(0, candidate.LineCount - 1)) * fontSize / 2f + Gap;
            return new Box(candidate.X - halfWidth, candidate.Y - halfHeight, candidate.X + halfWidth, candidate.Y + halfHeight);
        }

        private readonly struct Box
        {
            public Box(float left, float top, float right, float bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public float Left { get; }
            public float Top { get; }
            public float Right { get; }
            public float Bottom { get; }

            public bool Overlaps(Box other) =>
                Left < other.Right && other.Left < Right && Top < other.Bottom && other.Top < Bottom;

            /// <summary>The gap between the two boxes, edge to edge; 0 when they touch or overlap.</summary>
            public float DistanceTo(Box other)
            {
                float dx = System.Math.Max(0f, System.Math.Max(other.Left - Right, Left - other.Right));
                float dy = System.Math.Max(0f, System.Math.Max(other.Top - Bottom, Top - other.Bottom));
                return (float)System.Math.Sqrt(dx * dx + dy * dy);
            }
        }
    }
}
