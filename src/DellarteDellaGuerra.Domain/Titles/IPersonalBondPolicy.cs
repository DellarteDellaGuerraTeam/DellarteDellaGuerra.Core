namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IPersonalBondPolicy
    {
        bool IsBonded(string heroId, string otherHeroId, float relation);
    }
}
