using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /// <summary>
    /// The inheritance a title carries for a house: the titles answering to it by allegiance that
    /// the house also holds, and in turn theirs. A title another house holds under it is part of
    /// its following, not its inheritance, and so is everything below that title.
    /// </summary>
    internal static class TitleInheritance
    {
        public static IEnumerable<string> HeldUnder(
            string titleId,
            string clanId,
            IFeudalStructure feudalStructure,
            ITitleRepository titleRepository,
            IGenealogy genealogy)
        {
            List<string> vassalTitleIds = feudalStructure.GetAllTitleIds()
                .Where(id => feudalStructure.GetAllegianceSuzerainTitleId(id) == titleId)
                .ToList();
            foreach (string vassalTitleId in vassalTitleIds)
            {
                if (genealogy.GetHolderClanOf(titleRepository.GetTitle(vassalTitleId)) != clanId) continue;

                yield return vassalTitleId;
                foreach (string heldTitleId in HeldUnder(vassalTitleId, clanId, feudalStructure, titleRepository, genealogy))
                {
                    yield return heldTitleId;
                }
            }
        }
    }
}
