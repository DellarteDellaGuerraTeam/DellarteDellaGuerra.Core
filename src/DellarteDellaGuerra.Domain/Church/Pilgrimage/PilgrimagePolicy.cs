namespace DellarteDellaGuerra.Domain.Church.Pilgrimage
{
    public static class PilgrimagePolicy
    {
        public const float SpawnChancePerSettlementPerDay = 0.05f;

        /// <param name="activeParties">The number of pilgrim parties currently alive.</param>
        /// <param name="maxParties">The maximum number of pilgrim parties alive at once.</param>
        /// <param name="roll">A random roll in [0, 1).</param>
        public static bool ShouldSpawn(int activeParties, int maxParties, float roll)
        {
            if (activeParties >= maxParties) return false;
            return roll < SpawnChancePerSettlementPerDay;
        }
    }
}
