using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /**
     * <summary>
     *  Who answered which call to arms. Only clans that end up somewhere the feudal hierarchy
     *  would not have put them are named: a vassal that stays loyal is already on its liege's
     *  side and needs no mention.
     * </summary>
     */
    public record SupportDecision(
        IReadOnlyCollection<string> AttackerSupporters,
        IReadOnlyCollection<string> DefenderSupporters);
}
