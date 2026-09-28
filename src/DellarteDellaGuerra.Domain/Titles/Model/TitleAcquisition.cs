namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>How a holder came by a title. Initial is the holder the campaign starts with.</summary>
    public enum TitleAcquisition
    {
        Initial,
        Granted,
        Conquered,
        Inherited,
        Awarded
    }
}
