using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Infrastructure.Campaign;

public sealed class FemaleNonCombatantHeroCreationModel : HeroCreationModel
{
    private readonly HeroCreationModel _inner;
    private readonly IConfigurationProvider<DadgConfig> _configProvider;

    public FemaleNonCombatantHeroCreationModel(
        HeroCreationModel inner,
        IConfigurationProvider<DadgConfig> configProvider)
    {
        _inner = inner;
        _configProvider = configProvider;
    }

    public override (CampaignTime birthDay, CampaignTime deathDay) GetBirthAndDeathDay(
        CharacterObject character, bool createAlive, int age) =>
        _inner.GetBirthAndDeathDay(character, createAlive, age);

    public override Settlement GetBornSettlement(Hero character) =>
        _inner.GetBornSettlement(character);

    public override StaticBodyProperties GetStaticBodyProperties(
        Hero character, bool isOffspring, float variationAmount = 0.35f) =>
        _inner.GetStaticBodyProperties(character, isOffspring, variationAmount);

    public override FormationClass GetPreferredUpgradeFormation(Hero character) =>
        _inner.GetPreferredUpgradeFormation(character);

    public override Clan GetClan(Hero character) =>
        _inner.GetClan(character);

    public override CultureObject GetCulture(Hero hero, Settlement bornSettlement, Clan clan) =>
        _inner.GetCulture(hero, bornSettlement, clan);

    public override CharacterObject GetRandomTemplateByOccupation(
        Occupation occupation, Settlement? settlement = null) =>
        _inner.GetRandomTemplateByOccupation(occupation, settlement);

    public override List<(TraitObject trait, int level)> GetTraitsForHero(Hero hero) =>
        _inner.GetTraitsForHero(hero);

    public override Equipment GetCivilianEquipment(Hero hero) =>
        _inner.GetCivilianEquipment(hero);

    public override Equipment GetBattleEquipment(Hero hero) =>
        _inner.GetBattleEquipment(hero);

    public override CharacterObject GetCharacterTemplateForOffspring(
        Hero mother, Hero father, bool isOffspringFemale) =>
        _inner.GetCharacterTemplateForOffspring(mother, father, isOffspringFemale);

    public override (TextObject firstName, TextObject name) GenerateFirstAndFullName(Hero hero) =>
        _inner.GenerateFirstAndFullName(hero);

    public override List<(SkillObject, int)> GetDefaultSkillsForHero(Hero hero) =>
        _inner.GetDefaultSkillsForHero(hero);

    public override List<(SkillObject, int)> GetInheritedSkillsForHero(Hero hero) =>
        _inner.GetInheritedSkillsForHero(hero);

    public override bool IsHeroCombatant(Hero hero)
    {
        return hero.IsFemale
            ? _configProvider.Config?.EnableFemaleFighters == true
            : _inner.IsHeroCombatant(hero);
    }
}
