using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    public sealed record PoliticalMapLabelAnchor(float X, float Y);

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
    /// <param name="LineWidths">Measured advance of each centred line, divided by its font size.</param>
    /// <param name="LineAdvance">Baseline spacing divided by font size, matching the text renderer.</param>
    public sealed record PoliticalMapLabelCandidate(
        int Id, float X, float Y, float PixelRadius, int TextLength, long Priority, string? Group = null, int LineCount = 1,
        int? SizeIndex = null, IReadOnlyList<float>? LineWidths = null, float LineAdvance = 1f,
        IReadOnlyList<PoliticalMapLabelAnchor>? AlternativeAnchors = null);

    /// <summary>A label that is drawn this frame, in one of <see cref="PoliticalMapLabelLayout.FontSizes"/>.</summary>
    public sealed record PoliticalMapPlacedLabel(int Id, float X, float Y, int SizeIndex);

    /// <summary>
    /// Sizes region labels to their regions as they show on screen and keeps them from piling up.
    /// The font follows how big the region shows (its inscribed radius on screen at this zoom),
    /// never how long its name is: long names can wrap without shrinking their font.
    /// A region too small on screen gets no label. Labels are placed
    /// by tier, title rank and region size, each dropped when its measured lines overlap one placed.
    /// A label is also dropped when one saying the same thing (same <see cref="PoliticalMapLabelCandidate.Group"/>)
    /// has a preferred patch nearby: a kingdom split into patches, or a clan holding scattered fiefs,
    /// keeps its name on that patch even when a different title temporarily crowds it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Gauntlet cannot bind a font size, so sizes come in fixed steps, each its own text widget.
    /// </para>
    /// <para>
    /// When measured line widths are supplied, collisions use one box per line. Otherwise the
    /// text box, used only to keep labels apart, is estimated from the character count. The
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
        /// another place's, and it is named again. With whole-map layout, an offscreen twin still counts.
        /// </summary>
        public const float RepeatDistance = 1f / 3f;

        // Between two labels' boxes, in pixels.
        private const float Gap = 4f;
        // A hidden label needs clear room before returning; a visible one keeps the normal gap.
        private const float ReentryClearance = 12f;

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
            IReadOnlyList<PoliticalMapLabelCandidate> candidates, float screenWidth, float screenHeight,
            IReadOnlyCollection<int>? previouslyShown = null, bool includeOffscreen = false)
        {
            var ordered = new List<PoliticalMapLabelCandidate>(candidates);
            ordered.Sort((a, b) => a.Priority != b.Priority ? b.Priority.CompareTo(a.Priority) : a.Id.CompareTo(b.Id));

            float repeatDistance = RepeatDistance * screenWidth;
            var placed = new List<PoliticalMapPlacedLabel>();
            var boxes = new List<Box>();
            var repeatBoxes = new List<Box>();
            var groups = new List<string>();
            foreach (PoliticalMapLabelCandidate candidate in ordered)
            {
                if (candidate.TextLength <= 0) continue;
                int size = candidate.SizeIndex ?? SizeIndexFor(candidate.PixelRadius);
                if (size < 0) continue;

                Box box = BoxOf(candidate, FontSizes[size]);
                if (!includeOffscreen && (box.Right < 0f || box.Left > screenWidth || box.Bottom < 0f || box.Top > screenHeight)) continue;
                float clearance = previouslyShown is not null && !System.Linq.Enumerable.Contains(previouslyShown, candidate.Id)
                    ? ReentryClearance : 0f;
                if (candidate.Group is { } group && RepeatsNearby(group, box, clearance)) continue;

                // Reserve the preferred patch even when another title crowds it: no sideways fallback.
                if (candidate.Group is { } reservedGroup)
                {
                    repeatBoxes.Add(box);
                    groups.Add(reservedGroup);
                }
                PoliticalMapLabelCandidate chosen = candidate;
                IReadOnlyList<Box> ink = LineBoxesOf(chosen, FontSizes[size], box);
                if (Crowded(ink))
                {
                    bool found = false;
                    if (candidate.AlternativeAnchors is { } alternatives)
                        foreach (PoliticalMapLabelAnchor anchor in alternatives)
                        {
                            chosen = candidate with { X = anchor.X, Y = anchor.Y };
                            ink = LineBoxesOf(chosen, FontSizes[size], BoxOf(chosen, FontSizes[size]));
                            if (Crowded(ink)) continue;
                            found = true;
                            break;
                        }
                    if (!found) continue;
                }

                boxes.AddRange(ink);
                placed.Add(new PoliticalMapPlacedLabel(candidate.Id, chosen.X, chosen.Y, size));

                bool Crowded(IReadOnlyList<Box> lines)
                {
                    foreach (Box line in lines)
                        if (boxes.Exists(other => other.Overlaps(line) || clearance > 0 && other.DistanceTo(line) < clearance))
                            return true;
                    return false;
                }
            }

            bool RepeatsNearby(string group, Box box, float clearance)
            {
                for (int index = 0; index < repeatBoxes.Count; index++)
                {
                    if (string.Equals(groups[index], group, System.StringComparison.Ordinal)
                        && repeatBoxes[index].DistanceTo(box) < repeatDistance + clearance) return true;
                }

                return false;
            }

            return placed;
        }

        private static Box BoxOf(PoliticalMapLabelCandidate candidate, int fontSize)
        {
            float width = candidate.LineWidths is { Count: > 0 } widths
                ? System.Linq.Enumerable.Max(widths) * fontSize : TextWidthOf(fontSize, candidate.TextLength);
            float halfWidth = width / 2f + Gap;
            float halfHeight = (TextHeight + System.Math.Max(0, candidate.LineCount - 1) * candidate.LineAdvance) * fontSize / 2f + Gap;
            return new Box(candidate.X - halfWidth, candidate.Y - halfHeight, candidate.X + halfWidth, candidate.Y + halfHeight);
        }

        private static IReadOnlyList<Box> LineBoxesOf(PoliticalMapLabelCandidate candidate, int fontSize, Box fallback)
        {
            if (candidate.LineWidths is not { Count: > 0 } widths) return new[] { fallback };
            var lines = new List<Box>(widths.Count);
            float firstY = candidate.Y - (widths.Count - 1) * candidate.LineAdvance * fontSize / 2f;
            for (int i = 0; i < widths.Count; i++)
            {
                float halfWidth = widths[i] * fontSize / 2f + Gap;
                float halfHeight = TextHeight * fontSize / 2f + Gap;
                float y = firstY + i * candidate.LineAdvance * fontSize;
                lines.Add(new Box(candidate.X - halfWidth, y - halfHeight, candidate.X + halfWidth, y + halfHeight));
            }
            return lines;
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
