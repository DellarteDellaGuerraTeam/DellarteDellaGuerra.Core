using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whether one lord stands close enough to another to ride to his quarrel from outside his
     *  realm: close kin by blood or by marriage, or a firm friend.
     * </summary>
     * <remarks>
     *  Close kin is a parent, a child or a sibling, and by marriage a spouse, a spouse's father
     *  or a spouse's sibling. Siblings are found through the father, the one parent the
     *  genealogy records, the same way the succession walk finds them. A firm friend is what
     *  the game itself calls a friend: a relation above 50.
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
            relation > FriendRelation || IsCloseKin(heroId, otherHeroId);

        private bool IsCloseKin(string heroId, string otherHeroId)
        {
            var hero = _genealogy.GetHero(heroId);
            var other = _genealogy.GetHero(otherHeroId);
            if (hero is null || other is null || hero.Id == other.Id) return false;

            return IsBloodKin(hero, other)
                   || hero.SpouseId == other.Id
                   || other.SpouseId == hero.Id
                   || IsInLaw(hero, other)
                   || IsInLaw(other, hero);
        }

        // The other hero is the father or a sibling of the hero's spouse.
        private bool IsInLaw(HeroNode hero, HeroNode other)
        {
            if (hero.SpouseId is null) return false;

            var spouse = _genealogy.GetHero(hero.SpouseId);

            return spouse is not null
                   && spouse.Id != other.Id
                   && (spouse.FatherId == other.Id || IsSibling(spouse, other));
        }

        private static bool IsBloodKin(HeroNode hero, HeroNode other) =>
            hero.ChildIds.Contains(other.Id)
            || other.ChildIds.Contains(hero.Id)
            || IsSibling(hero, other);

        private static bool IsSibling(HeroNode hero, HeroNode other) =>
            hero.FatherId is not null && hero.FatherId == other.FatherId;
    }
}
