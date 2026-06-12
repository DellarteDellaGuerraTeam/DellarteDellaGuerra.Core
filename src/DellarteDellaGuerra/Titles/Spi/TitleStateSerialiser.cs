using System.Collections.Generic;
using System.Globalization;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Titles.Spi
{
    /**
     * <summary>
     *  Converts between the pure domain records (Title/Claim/FeudalTension) and flat,
     *  pipe-delimited string lists that are natively serialisable through Bannerlord's
     *  <c>IDataStore.SyncData</c> mechanism. This keeps the domain records free of any
     *  <c>SaveableField</c> attributes.
     * </summary>
     * <remarks>
     *  Format (fields joined with '|'):
     *  <list type="bullet">
     *   <item>Title: Id|Name|Rank|SeatSettlementId|HolderClanId|OccupantClanId|ContestedSinceDay
     *    ('' sentinel for a vacant/absent field). Pre-occupant saves carry 5 fields and load
     *    as uncontested.</item>
     *   <item>Claim: Id|ClaimantClanId|TitleId|Strength|Origin</item>
     *   <item>Tension: ClaimantClanId|TitleId|Amount (invariant culture float)</item>
     *  </list>
     *  The delimiter '|' must not appear in ids or title names. Ids are Bannerlord
     *  StringIds and configuration-defined title ids, neither of which contain pipes.
     *  Malformed entries are skipped on deserialisation rather than corrupting the load.
     * </remarks>
     */
    public static class TitleStateSerialiser
    {
        private const char Delimiter = '|';

        public static List<string> SerialiseTitles(IReadOnlyList<Title> titles)
        {
            var serialised = new List<string>(titles.Count);
            foreach (var title in titles)
            {
                serialised.Add(string.Join(
                    Delimiter.ToString(),
                    title.Id,
                    title.Name,
                    title.Rank.ToString(),
                    title.SeatSettlementId,
                    title.HolderClanId ?? string.Empty,
                    title.OccupantClanId ?? string.Empty,
                    title.ContestedSinceDay?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty));
            }

            return serialised;
        }

        public static List<Title> DeserialiseTitles(List<string> serialisedTitles)
        {
            var titles = new List<Title>(serialisedTitles.Count);
            foreach (string line in serialisedTitles)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 5 && fields.Length != 7) continue;
                if (!TryParseEnum(fields[2], out TitleRank rank)) continue;

                float? contestedSinceDay = null;
                if (fields.Length == 7 && fields[6].Length > 0)
                {
                    if (!float.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float day))
                    {
                        continue;
                    }

                    contestedSinceDay = day;
                }

                titles.Add(new Title(
                    fields[0],
                    fields[1],
                    rank,
                    fields[3],
                    fields[4].Length == 0 ? null : fields[4],
                    fields.Length == 7 && fields[5].Length > 0 ? fields[5] : null,
                    contestedSinceDay));
            }

            return titles;
        }

        public static List<string> SerialiseClaims(IReadOnlyList<Claim> claims)
        {
            var serialised = new List<string>(claims.Count);
            foreach (var claim in claims)
            {
                serialised.Add(string.Join(
                    Delimiter.ToString(),
                    claim.Id,
                    claim.ClaimantClanId,
                    claim.TitleId,
                    claim.Strength.ToString(),
                    claim.Origin.ToString()));
            }

            return serialised;
        }

        public static List<Claim> DeserialiseClaims(List<string> serialisedClaims)
        {
            var claims = new List<Claim>(serialisedClaims.Count);
            foreach (string line in serialisedClaims)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 5) continue;
                if (!TryParseEnum(fields[3], out ClaimStrength strength)) continue;
                if (!TryParseEnum(fields[4], out ClaimOrigin origin)) continue;

                claims.Add(new Claim(fields[0], fields[1], fields[2], strength, origin));
            }

            return claims;
        }

        public static List<string> SerialiseTensions(IReadOnlyList<FeudalTension> tensions)
        {
            var serialised = new List<string>(tensions.Count);
            foreach (var tension in tensions)
            {
                serialised.Add(string.Join(
                    Delimiter.ToString(),
                    tension.ClaimantClanId,
                    tension.TitleId,
                    tension.Amount.ToString("R", CultureInfo.InvariantCulture)));
            }

            return serialised;
        }

        public static List<FeudalTension> DeserialiseTensions(List<string> serialisedTensions)
        {
            var tensions = new List<FeudalTension>(serialisedTensions.Count);
            foreach (string line in serialisedTensions)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 3) continue;
                if (!float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                {
                    continue;
                }

                tensions.Add(new FeudalTension(fields[0], fields[1], amount));
            }

            return tensions;
        }

        private static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            return System.Enum.TryParse(value, out result) && System.Enum.IsDefined(typeof(TEnum), result);
        }
    }
}
