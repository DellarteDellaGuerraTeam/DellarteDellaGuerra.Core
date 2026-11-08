namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// What a worn piece is made of, from least to most protective. An arm hit takes the strongest
    /// of sleeve and glove, so the order matters.
    /// </summary>
    public enum ArmourMaterial
    {
        Cloth,
        Padded,
        Mail,
        Brigandine,
        Plate
    }
}
