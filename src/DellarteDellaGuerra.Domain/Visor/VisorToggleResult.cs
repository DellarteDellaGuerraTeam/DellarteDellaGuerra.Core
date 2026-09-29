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
    }
}
