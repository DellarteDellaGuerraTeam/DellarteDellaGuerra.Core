using System.Collections.Generic;
using Bannerlord.PrivateWars.Domain;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    // Map-backed ISuzerainProvider test double: replaces the old inline Suzerain(...) Func helpers.
    internal sealed class MapSuzerainProvider : ISuzerainProvider
    {
        private readonly Dictionary<string, string> _map = new();

        public MapSuzerainProvider(params (string clan, string suzerain)[] links)
        {
            foreach (var (clan, suzerain) in links) _map[clan] = suzerain;
        }

        public string? GetSuzerain(string clanId) => _map.TryGetValue(clanId, out var s) ? s : null;
    }
}
