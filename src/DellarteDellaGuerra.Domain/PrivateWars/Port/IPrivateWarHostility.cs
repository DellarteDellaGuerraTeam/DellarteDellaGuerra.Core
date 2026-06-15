namespace DellarteDellaGuerra.Domain.PrivateWars.Port
{
    /// <summary>
    /// The patch-facing hostility signal: two clans are enemies when they sit on opposite sides
    /// of some active private war. This is the hot path consulted by every §4 model override and
    /// Harmony patch, so the registry implementation must answer it in O(1) (design §18.A).
    /// </summary>
    public interface IPrivateWarHostility
    {
        bool AreEnemies(string clanIdA, string clanIdB);
    }
}
