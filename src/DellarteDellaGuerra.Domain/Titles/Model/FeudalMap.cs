using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// The whole feudal hierarchy as a forest of realms, where a realm is a top-level title
    /// without a de jure suzerain.
    /// </summary>
    public record FeudalMap(IReadOnlyList<FeudalMapEntry> Realms);
}
