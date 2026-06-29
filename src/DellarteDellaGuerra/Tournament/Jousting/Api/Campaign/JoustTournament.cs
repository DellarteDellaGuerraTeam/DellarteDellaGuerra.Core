using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Campaign
{
    public class JoustTournament : DadgFightingTournament
    {
        private const string SceneName = "dadg_joust_v2";

        public JoustTournament(Town town, IGetTournamentRewardUseCase getTournamentRewardUseCase)
            : base(town, getTournamentRewardUseCase)
        {
        }

        public override int MaxTeamSize => 1;
        public override int MaxTeamNumberPerMatch => 2;

        public override bool CanBeAParticipant(CharacterObject character, bool considerSkills)
        {
            return character is not null && HasRequiredJoustSkills(character);
        }

        public override MBList<CharacterObject> GetParticipantCharacters(Settlement settlement,
            bool includePlayer = true)
        {
            var participants = new MBList<CharacterObject>();

            participants.AddRange(base.GetParticipantCharacters(settlement, includePlayer)
                .Where(HasRequiredJoustSkills));
            participants.AddRange(GetGarrisonSoldiers(settlement)
                .Where(c => !participants.Contains(c))
                .Take(MaximumParticipantCount - participants.Count));

            var filler = GetBestCultureHorseman(settlement)
                         ?? settlement.Culture?.EliteBasicTroop
                         ?? settlement.Culture?.BasicTroop;

            if (filler != null)
            {
                participants.AddRange(Enumerable.Repeat(filler, MaximumParticipantCount - participants.Count));
            }
            else if (participants.Count > 0)
            {
                var count = participants.Count;
                participants.AddRange(Enumerable.Range(0, MaximumParticipantCount - count)
                    .Select(i => participants[i % count]));
            }

            return participants;
        }

        public override void OpenMission(Settlement settlement, bool isPlayerParticipating)
        {
            JoustingMissionManager.OpenJoustingFightMission(SceneName, this, settlement, settlement.Culture,
                isPlayerParticipating && CanBeAParticipant(CharacterObject.PlayerCharacter, true));
        }

        private IEnumerable<CharacterObject> GetGarrisonSoldiers(Settlement settlement)
        {
            return settlement.Town?.GarrisonParty?.MemberRoster
                       .GetTroopRoster()
                       .Select(e => e.Character)
                       .Where(HasRequiredJoustSkills)
                   ?? Enumerable.Empty<CharacterObject>();
        }

        private CharacterObject GetBestCultureHorseman(Settlement settlement)
        {
            return MBObjectManager.Instance
                .GetObjectTypeList<CharacterObject>()
                .Where(character => !character.IsHero && character.Culture == settlement.Culture && IsHorseman(character) && HasRequiredJoustSkills(character))
                .OrderByDescending(c => c.Tier)
                .FirstOrDefault();
        }

        private static bool IsHorseman(CharacterObject character)
        {
            return character.Equipment != null && character.Equipment[EquipmentIndex.Horse].Item != null;
        }

        private bool HasRequiredJoustSkills(CharacterObject character)
        {
            var provider = BannerlordCampaign.Current
                ?.GetCampaignBehavior<JoustTournamentCampaignBehavior>()
                ?.JoustRequirementsProvider;
            if (provider == null) return false;
            return provider.GetRequirements().IsMet(skillName =>
            {
                var skill = MBObjectManager.Instance?.GetObject<SkillObject>(skillName);
                return skill != null ? character.GetSkillValue(skill) : 0;
            });
        }
    }
}
