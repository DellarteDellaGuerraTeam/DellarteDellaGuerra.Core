using System.Collections.Generic;
using System.Globalization;
using DellarteDellaGuerra.Domain.Levy.Model;

namespace DellarteDellaGuerra.Levy.Spi
{
    // Converts LevyCall records to/from pipe-delimited strings for IDataStore.SyncData.
    // Format: Id|IssuingClanId|VassalClanId|IssuedAtDay|Status
    public static class LevyStateSerialiser
    {
        private const char Delimiter = '|';

        public static List<string> Serialise(IReadOnlyList<LevyCall> calls)
        {
            var result = new List<string>(calls.Count);
            foreach (var call in calls)
            {
                result.Add(string.Join(
                    Delimiter.ToString(),
                    call.Id,
                    call.IssuingClanId,
                    call.VassalClanId,
                    call.IssuedAtDay.ToString("R", CultureInfo.InvariantCulture),
                    call.Status));
            }
            return result;
        }

        public static List<LevyCall> Deserialise(List<string> lines)
        {
            var result = new List<LevyCall>(lines.Count);
            foreach (var line in lines)
            {
                var fields = line.Split(Delimiter);
                if (fields.Length != 5) continue;
                if (!float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float day))
                    continue;
                if (!System.Enum.TryParse(fields[4], out LevyStatus status))
                    continue;

                result.Add(new LevyCall(fields[0], fields[1], fields[2], day, status));
            }
            return result;
        }
    }
}
