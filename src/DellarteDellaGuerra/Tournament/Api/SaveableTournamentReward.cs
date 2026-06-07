using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Tournament.Api
{
    /// <summary>
    /// Persistence-layer mirror of <see cref="Domain.Tournament.Reward.Model.TournamentReward"/>.
    /// Lives in the API layer so the Domain record stays free of TaleWorlds save-system attributes.
    /// Add a new <c>[SaveableProperty]</c> here whenever the domain reward grows a new field.
    /// </summary>
    internal class SaveableTournamentReward
    {
        [SaveableProperty(1)] public string? ItemId { get; set; }
    }
}
