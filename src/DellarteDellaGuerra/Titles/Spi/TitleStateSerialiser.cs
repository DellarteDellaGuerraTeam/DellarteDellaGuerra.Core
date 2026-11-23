using System.Collections.Generic;
using System.Globalization;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Titles.Spi
{
    /**
     * <summary>
     *  Converts between the pure domain records (Title/Claim) and flat,
     *  pipe-delimited string lists that are natively serialisable through Bannerlord's
     *  <c>IDataStore.SyncData</c> mechanism. This keeps the domain records free of any
     *  <c>SaveableField</c> attributes.
     * </summary>
     * <remarks>
     *  Format (fields joined with '|'):
     *  <list type="bullet">
     *   <item>Title: Id|Name|Rank|SeatSettlementId|HolderHeroId|OccupantClanId|ContestedSinceDay
     *    followed by the ledger, oldest first, as one HeroId|SinceDay|Acquisition triple per
     *    holder ('' sentinel for a vacant/absent field). HolderHeroId repeats the last holder.
     *    Pre-occupant saves carry 5 fields and load as
     *    uncontested; pre-ledger saves carry 5 or 7 and open a ledger with their holder on day 0.</item>
     *   <item>Claim: Id|ClaimantClanId|TitleId|Strength|Origin|ClaimantHeroId
     *    ('' sentinel for a clan-level claim). Pre-hero saves carry 5 fields and load
     *    with no claimant hero.</item>
     *   <item>Link: FromId|ToId ('' sentinel for no target), for the title reattachments
     *    (TitleId|SuzerainTitleId).</item>
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
                var fields = new List<string>
                {
                    title.Id,
                    title.Name,
                    title.Rank.ToString(),
                    title.SeatSettlementId,
                    title.HolderHeroId ?? string.Empty,
                    title.OccupantClanId ?? string.Empty,
                    title.ContestedSinceDay?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty
                };
                foreach (var holder in title.Holders)
                {
                    fields.Add(holder.HeroId ?? string.Empty);
                    fields.Add(holder.SinceDay.ToString("R", CultureInfo.InvariantCulture));
                    fields.Add(holder.Acquisition.ToString());
                }

                serialised.Add(string.Join(Delimiter.ToString(), fields));
            }

            return serialised;
        }

        public static List<Title> DeserialiseTitles(List<string> serialisedTitles)
        {
            var titles = new List<Title>(serialisedTitles.Count);
            foreach (string line in serialisedTitles)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 5 && (fields.Length < 7 || (fields.Length - 7) % 3 != 0)) continue;
                if (!TryParseEnum(fields[2], out TitleRank rank)) continue;

                float? contestedSinceDay = null;
                if (fields.Length >= 7 && fields[6].Length > 0)
                {
                    if (!TryParseDay(fields[6], out float day)) continue;

                    contestedSinceDay = day;
                }

                string? occupantClanId = fields.Length >= 7 && fields[5].Length > 0 ? fields[5] : null;

                // A save written before the ledger: the holder it names opens a fresh one.
                if (fields.Length <= 7)
                {
                    titles.Add(new Title(
                        fields[0],
                        fields[1],
                        rank,
                        fields[3],
                        fields[4].Length == 0 ? null : fields[4],
                        occupantClanId,
                        contestedSinceDay));
                    continue;
                }

                var holders = new List<TitleHolder>();
                for (int index = 7; index < fields.Length; index += 3)
                {
                    if (!TryParseDay(fields[index + 1], out float sinceDay)) break;
                    if (!TryParseEnum(fields[index + 2], out TitleAcquisition acquisition)) break;

                    holders.Add(new TitleHolder(
                        fields[index].Length == 0 ? null : fields[index], sinceDay, acquisition));
                }

                if (holders.Count != (fields.Length - 7) / 3) continue;

                titles.Add(Title.Restore(
                    fields[0], fields[1], rank, fields[3], holders, occupantClanId, contestedSinceDay));
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
                    claim.Origin.ToString(),
                    claim.ClaimantHeroId ?? string.Empty));
            }

            return serialised;
        }

        public static List<Claim> DeserialiseClaims(List<string> serialisedClaims)
        {
            var claims = new List<Claim>(serialisedClaims.Count);
            foreach (string line in serialisedClaims)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 5 && fields.Length != 6) continue;
                if (!TryParseEnum(fields[3], out ClaimStrength strength)) continue;
                if (!TryParseEnum(fields[4], out ClaimOrigin origin)) continue;

                claims.Add(new Claim(
                    fields[0],
                    fields[1],
                    fields[2],
                    strength,
                    origin,
                    fields.Length == 6 && fields[5].Length > 0 ? fields[5] : null));
            }

            return claims;
        }

        public static List<string> SerialiseLinks(IEnumerable<KeyValuePair<string, string?>> links)
        {
            var serialised = new List<string>();
            foreach (var link in links)
            {
                serialised.Add(link.Key + Delimiter + (link.Value ?? string.Empty));
            }

            return serialised;
        }

        public static Dictionary<string, string?> DeserialiseLinks(List<string> serialisedLinks)
        {
            var links = new Dictionary<string, string?>(serialisedLinks.Count);
            foreach (string line in serialisedLinks)
            {
                string[] fields = line.Split(Delimiter);
                if (fields.Length != 2 || fields[0].Length == 0) continue;

                links[fields[0]] = fields[1].Length == 0 ? null : fields[1];
            }

            return links;
        }

        private static bool TryParseDay(string value, out float day)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out day);
        }

        private static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            return System.Enum.TryParse(value, out result) && System.Enum.IsDefined(typeof(TEnum), result);
        }
    }
}
