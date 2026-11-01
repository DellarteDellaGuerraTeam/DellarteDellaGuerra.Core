namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>Names an owner's land without presenting a clan fallback as a registered title.</summary>
    public static class PoliticalMapOwnerLabelName
    {
        public static string? Resolve(PoliticalMapLabelMode mode,
            string? titleName, string? clanName, string? estateName)
        {
            if (mode == PoliticalMapLabelMode.Clans) return clanName;
            return !string.IsNullOrWhiteSpace(titleName) ? titleName : estateName;
        }
    }
}
