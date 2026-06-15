namespace DellarteDellaGuerra.Domain.PrivateWars.Port
{
    /// <summary>
    /// Supplies the DADG feudal suzerain link used to resolve war sides by walking up the chain
    /// to the nearest belligerent principal (call-to-arms, design §18.A). Returns null for a clan
    /// with no suzerain (top of its tree).
    /// </summary>
    public interface IFeudalHierarchy
    {
        string? GetSuzerain(string clanId);
    }
}
