using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whether one lord stands close enough to another to ride to his quarrel from outside his
     *  realm: close kin by blood, or a firm friend.
     * </summary>
     * <remarks>
     *  Close kin is a parent, a child or a sibling. Siblings are found through the father, the
     *  one parent the genealogy records, the same way the succession walk finds them.
     * </remarks>
     */
    public class PersonalBondPolicy : IPersonalBondPolicy
    {
        private const float FriendRelation = 50f;

        private readonly IGenealogy _genealogy;

        public PersonalBondPolicy(IGenealogy genealogy)
        {
            _genealogy = genealogy;
        }

        public bool IsBonded(string heroId, string otherHeroId, float relation) =>
            relation >= FriendRelation || IsCloseKin(heroId, otherHeroId);

        private bool IsCloseKin(string heroId, string otherHeroId)
        {
            var hero = _genealogy.GetHero(heroId);
            var other = _genealogy.GetHero(otherHeroId);
            if (hero is null || other is null || hero.Id == other.Id) return false;

            return hero.ChildIds.Contains(other.Id)
                   || other.ChildIds.Contains(hero.Id)
                   || (hero.FatherId is not null && hero.FatherId == other.FatherId);
        }
    }
}
