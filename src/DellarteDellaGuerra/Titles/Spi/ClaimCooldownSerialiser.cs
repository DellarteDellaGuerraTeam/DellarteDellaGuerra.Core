using System.Collections.Generic;
using System.Globalization;

namespace DellarteDellaGuerra.Titles.Spi
{
    /**
     * <summary>
     *  Converts the claim evaluator's cooldown map to and from a flat, pipe-delimited string
     *  list that is natively serialisable through Bannerlord's <c>IDataStore.SyncData</c>.
     * </summary>
     * <remarks>
     *  Format: <c>TitleId:ClaimantClanId|NextEvaluationDay</c>. The delimiter '|' must not
     *  appear in the key; it is built from a configuration-defined title id and a Bannerlord
     *  StringId, neither of which contains a pipe. Malformed entries are skipped on
     *  deserialisation rather than corrupting the load.
     * </remarks>
     */
    public static class ClaimCooldownSerialiser
    {
        private const char Delimiter = '|';

        public static List<string> Serialise(IReadOnlyDictionary<string, float> nextEvaluationDayByPair)
        {
            var serialised = new List<string>(nextEvaluationDayByPair.Count);
            foreach (var entry in nextEvaluationDayByPair)
            {
                serialised.Add(string.Join(
                    Delimiter.ToString(),
                    entry.Key,
                    entry.Value.ToString("R", CultureInfo.InvariantCulture)));
            }

            return serialised;
        }

        public static Dictionary<string, float> Deserialise(List<string> serialisedCooldowns)
        {
            var nextEvaluationDayByPair = new Dictionary<string, float>(serialisedCooldowns.Count);
            foreach (string line in serialisedCooldowns)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 2) continue;
                if (!float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float day)) continue;

                nextEvaluationDayByPair[fields[0]] = day;
            }

            return nextEvaluationDayByPair;
        }
    }
}
