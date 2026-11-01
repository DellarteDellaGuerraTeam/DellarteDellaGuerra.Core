using System;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>Breaks oversized names at word boundaries, keeping at most three centred lines.</summary>
    public static class PoliticalMapLabelWrapping
    {
        public static string Wrap(string text, int targetLength)
        {
            targetLength = Math.Max(1, targetLength);
            if (text.Length <= targetLength * 1.5f) return text;
            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2) return text;

            var lengths = new int[words.Length + 1];
            for (int i = 0; i < words.Length; i++) lengths[i + 1] = lengths[i] + words[i].Length + 1;
            int Length(int from, int to) => lengths[to] - lengths[from] - 1;

            int first = 1, second = words.Length, widest = int.MaxValue;
            for (int split = 1; split < words.Length; split++)
            {
                int width = Math.Max(Length(0, split), Length(split, words.Length));
                if (width >= widest) continue;
                widest = width;
                first = split;
            }

            // Prefer two lines; only add a third when it actually reduces an oversized line.
            if (widest > targetLength)
                for (int a = 1; a < words.Length - 1; a++)
                for (int b = a + 1; b < words.Length; b++)
                {
                    int width = Math.Max(Length(0, a), Math.Max(Length(a, b), Length(b, words.Length)));
                    if (width >= widest) continue;
                    widest = width;
                    first = a;
                    second = b;
                }

            string result = string.Join(" ", words, 0, first) + "\n" + string.Join(" ", words, first, second - first);
            return second == words.Length ? result : result + "\n" + string.Join(" ", words, second, words.Length - second);
        }
    }
}
