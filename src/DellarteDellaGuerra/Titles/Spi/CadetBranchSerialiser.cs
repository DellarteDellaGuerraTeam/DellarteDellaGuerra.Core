using System.Collections.Generic;

namespace DellarteDellaGuerra.Titles.Spi
{
    /**
     * <summary>
     *  Converts the map of cadet branches to the houses they split from into a flat,
     *  pipe-delimited string list that is natively serialisable through Bannerlord's
     *  <c>IDataStore.SyncData</c>.
     *
     * <remarks>
     *  Format: <c>CadetClanId|ParentClanId</c>. Both are Bannerlord StringIds, neither of which
     *  contains a pipe. Malformed entries are skipped on deserialisation rather than corrupting
     *  the load: the cost is a cadet branch that outlives a lost war, not a broken save.
     * </remarks>
     */
    public static class CadetBranchSerialiser
    {
        private const char Delimiter = '|';

        public static List<string> Serialise(IReadOnlyDictionary<string, string> parentByCadetClanId)
        {
            var serialised = new List<string>(parentByCadetClanId.Count);
            foreach (var entry in parentByCadetClanId)
            {
                serialised.Add(string.Join(Delimiter.ToString(), entry.Key, entry.Value));
            }

            return serialised;
        }

        public static Dictionary<string, string> Deserialise(List<string> serialisedBranches)
        {
            var parentByCadetClanId = new Dictionary<string, string>(serialisedBranches.Count);
            foreach (string line in serialisedBranches)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 2) continue;

                parentByCadetClanId[fields[0]] = fields[1];
            }

            return parentByCadetClanId;
        }
    }
}
