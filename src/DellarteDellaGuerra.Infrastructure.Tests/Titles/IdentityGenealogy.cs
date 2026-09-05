using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Minimal identity-mapping genealogy test double: every id is its own clan id and its own
/// clan's leader id. Lets tests that predate the hero-id refactor keep using clan-id-shaped
/// literals as title holders without registering any hero data.
/// </summary>
internal sealed class IdentityGenealogy : IGenealogy
{
    public HeroNode? GetHero(string heroId) => new HeroNode(heroId, false, true, heroId, Array.Empty<string>());

    public string? GetClanLeaderId(string clanId) => clanId;

    public string? GetClanOf(string heroId) => heroId;

    public IReadOnlyList<string> GetDeceasedClanMemberIds(string clanId) => Array.Empty<string>();
}
