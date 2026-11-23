namespace DellarteDellaGuerra.Domain.Visor
{
    public record VisorToggleResult(string NextItemId, bool IsOpen)
    {
        public static VisorToggleResult From(VisorVariantPair pair, string currentItemId)
        {
            return pair.ClosedItemId == currentItemId
                ? new VisorToggleResult(pair.OpenItemId, true)
                : new VisorToggleResult(pair.ClosedItemId, false);
        }

        /// <summary>
        /// Resolves the helmet that puts the visor in the requested state, or null when it is already there.
        /// </summary>
        public static VisorToggleResult? ToState(VisorVariantPair pair, string currentItemId, bool open)
        {
            var targetItemId = open ? pair.OpenItemId : pair.ClosedItemId;
            return currentItemId == targetItemId ? null : new VisorToggleResult(targetItemId, open);
        }
    }
}
