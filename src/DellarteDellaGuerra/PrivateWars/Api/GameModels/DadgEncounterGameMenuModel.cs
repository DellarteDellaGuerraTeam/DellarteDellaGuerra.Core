using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.PrivateWars.Api.GameModels
{
    // Turn a same-kingdom private-war field meeting into a real battle instead of a dismissable chat.
    //
    // When the player's party collides with another party in the field, EncounterManager routes through
    // RestartPlayerEncounter -> PlayerEncounter.Init -> EncounterGameMenuModel.GetEncounterMenu. For a
    // plain party-vs-party field encounter the default model returns "encounter_meeting" with
    // startBattle = false, which opens PlayerEncounter.DoMeeting -> a friendly conversation. Vanilla
    // relies on that conversation being war-gated to escalate to a fight; in a same-kingdom private war
    // IsAtWarWith is false (design §2), so the conversation stays friendly and the player can always
    // dismiss it. The AI attacker immediately re-collides, producing an endless meeting loop with no
    // battle.
    //
    // This override detects that exact case — the default model chose "encounter_meeting" and the
    // encountered party is a registered private-war enemy of the player's clan — and forces the battle
    // branch ("encounter" with startBattle = true), mirroring how the default model already starts a
    // battle for a besieging garrison. Every other encounter falls through to vanilla unchanged; when no
    // private war is active AreEnemies is false and this is a no-op. The bespoke pre-battle "press your
    // claim" conversation is the deferred dialog-UI layer (design §4.4) and is intentionally not added
    // here.
    public class DadgEncounterGameMenuModel : DefaultEncounterGameMenuModel
    {
        public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty,
            out bool startBattle, out bool joinBattle)
        {
            
            var menu = base.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);

            if (menu == "encounter_meeting" && IsPrivateWarFieldEnemy(attackerParty, defenderParty))
            {
                startBattle = true;
                joinBattle = false;
                return "encounter";
            }

            return menu;
        }

        private static bool IsPrivateWarFieldEnemy(PartyBase attackerParty, PartyBase defenderParty)
        {
            var encountered = MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty);
            return PrivateWarSiegeDefenderPolicy.AreEnemies(Hero.MainHero?.Clan, encountered?.MobileParty?.ActualClan);
        }
    }
}
