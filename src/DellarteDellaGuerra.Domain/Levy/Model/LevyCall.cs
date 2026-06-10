namespace DellarteDellaGuerra.Domain.Levy.Model
{
    // Tracks a single feudal levy obligation: the issuing lord summons the vassal clan to
    // muster under their army. IssuedAtDay is the campaign elapsed-days value at the time
    // the levy was called, used to determine when it has expired without a response.
    public record LevyCall(
        string Id,
        string IssuingClanId,
        string VassalClanId,
        float IssuedAtDay,
        LevyStatus Status);
}
