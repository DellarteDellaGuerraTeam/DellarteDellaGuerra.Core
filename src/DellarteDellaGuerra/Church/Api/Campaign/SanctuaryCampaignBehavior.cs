using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Church.Port;
using DellarteDellaGuerra.Domain.Church.Sanctuary;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class SanctuaryCampaignBehavior : CampaignBehaviorBase
    {
        private const string SanctuaryMenuId = "dadg_church_sanctuary";

        private readonly IChurchSettingsProvider _churchSettingsProvider;

        private CampaignTime _playerSanctuaryStart = CampaignTime.Never;
        private Dictionary<Hero, Settlement> _fugitiveSanctuaries = new();
        private Dictionary<Hero, CampaignTime> _fugitiveSanctuaryStarts = new();
        private bool _raidNoticeShown;

        public SanctuaryCampaignBehavior(IChurchSettingsProvider churchSettingsProvider)
        {
            _churchSettingsProvider = churchSettingsProvider;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTickHero);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_dadgChurchPlayerSanctuaryStart", ref _playerSanctuaryStart);
            dataStore.SyncData("_dadgChurchFugitiveSanctuaries", ref _fugitiveSanctuaries);
            dataStore.SyncData("_dadgChurchFugitiveSanctuaryStarts", ref _fugitiveSanctuaryStarts);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "village",
                "dadg_church_claim_sanctuary",
                "{=vK7pQn2X}Claim sanctuary",
                CanClaimSanctuary,
                ClaimSanctuary);
            starter.AddGameMenuOption(
                "village",
                "dadg_church_drag_fugitive",
                "{=iF3zTb6M}Drag {FUGITIVE_NAME} from the cloister",
                CanDragFugitive,
                DragFugitive);
            starter.AddWaitGameMenu(
                SanctuaryMenuId,
                "{=uW6mDv3K}You have claimed sanctuary within the walls of {MONASTERY_NAME}. " +
                "None may lay hands on you here, by law of God and man. " +
                "({DAYS_LEFT} days of grace remain)",
                SanctuaryWaitInit,
                _ => true,
                null,
                SanctuaryWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption,
                GameMenu.MenuOverlayType.SettlementWithBoth);
            starter.AddGameMenuOption(
                SanctuaryMenuId,
                "dadg_church_sanctuary_leave",
                "{=bN2kSj7F}Leave the sanctuary",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => EndPlayerSanctuary(),
                isLeave: true);
        }

        private static bool CanClaimSanctuary(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;

            var settlement = Settlement.CurrentSettlement;
            if (settlement == null || !ChurchSettlements.IsChurchSettlement(settlement)) return false;

            var enabled = settlement.Village.VillageState == Village.VillageStates.Normal;
            var tooltip = new TextObject("{=jR4wXe8B}The cloister is in no state to shelter you.");
            return MenuHelper.SetOptionProperties(args, enabled, !enabled, tooltip);
        }

        private void ClaimSanctuary(MenuCallbackArgs args)
        {
            _playerSanctuaryStart = CampaignTime.Now;
            GameMenu.SwitchToMenu(SanctuaryMenuId);
        }

        private void SanctuaryWaitInit(MenuCallbackArgs args)
        {
            _raidNoticeShown = false;
            UpdateSanctuaryText(_playerSanctuaryStart.ElapsedDaysUntilNow);
            if (PlayerEncounter.Current != null) PlayerEncounter.Current.IsPlayerWaiting = true;
            MobileParty.MainParty.SetMoveModeHold();
        }

        private void SanctuaryWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
            var elapsedDays = _playerSanctuaryStart.ElapsedDaysUntilNow;
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(
                elapsedDays / SanctuaryPolicy.PlayerSanctuaryDays);
            UpdateSanctuaryText(elapsedDays);

            var settlement = Settlement.CurrentSettlement;
            if (!_raidNoticeShown && settlement.IsUnderRaid)
            {
                _raidNoticeShown = true;
                var notice = new TextObject(
                    "{=eH5vGm1T}Raiders torch {VILLAGE_NAME} around you, but the abbey walls hold.");
                notice.SetTextVariable("VILLAGE_NAME", settlement.Name);
                InformationManager.DisplayMessage(new InformationMessage(notice.ToString()));
            }

            if (SanctuaryPolicy.Evaluate(elapsedDays, SanctuaryPolicy.PlayerSanctuaryDays) !=
                SanctuaryOutcome.Expired) return;
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=qC9fRt5L}Your forty days of sanctuary are spent.").ToString()));
            EndPlayerSanctuary();
        }

        private void EndPlayerSanctuary()
        {
            _playerSanctuaryStart = CampaignTime.Never;
            if (PlayerEncounter.Current != null) PlayerEncounter.Current.IsPlayerWaiting = false;
            GameMenu.SwitchToMenu("village");
        }

        private static void UpdateSanctuaryText(float elapsedDays)
        {
            var daysLeft = SanctuaryPolicy.PlayerSanctuaryDays - (int)elapsedDays;
            if (daysLeft < 0) daysLeft = 0;
            MBTextManager.SetTextVariable("MONASTERY_NAME", Settlement.CurrentSettlement.Name);
            MBTextManager.SetTextVariable("DAYS_LEFT", daysLeft);
        }

        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
            if (!mobileParty.IsLordParty) return;

            var hero = mobileParty.LeaderHero;
            if (hero == null || !hero.IsAlive || hero.IsPrisoner || hero.Clan == Clan.PlayerClan) return;

            var monastery = FindNearestChurchSettlement(mobileParty.GetPosition2D);
            if (monastery == null) return;

            _fugitiveSanctuaries[hero] = monastery;
            _fugitiveSanctuaryStarts[hero] = CampaignTime.Now;
            EnterSettlementAction.ApplyForCharacterOnly(hero, monastery);

            if (!hero.HasMet) return;
            var message = new TextObject("{=oD8yLw4Q}{LORD_NAME} has taken sanctuary at {MONASTERY_NAME}.");
            message.SetTextVariable("LORD_NAME", hero.Name);
            message.SetTextVariable("MONASTERY_NAME", monastery.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
        }

        private void OnDailyTickHero(Hero hero)
        {
            if (!_fugitiveSanctuaries.TryGetValue(hero, out var monastery)) return;

            var elapsedDays = _fugitiveSanctuaryStarts[hero].ElapsedDaysUntilNow;
            if (!hero.IsAlive || hero.IsPrisoner || hero.PartyBelongedTo != null ||
                SanctuaryPolicy.Evaluate(elapsedDays, SanctuaryPolicy.FugitiveSanctuaryDays) ==
                SanctuaryOutcome.Expired)
            {
                Untag(hero);
                return;
            }

            if (hero.CurrentSettlement != monastery)
                EnterSettlementAction.ApplyForCharacterOnly(hero, monastery);
        }

        private bool CanDragFugitive(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

            var settlement = Settlement.CurrentSettlement;
            if (settlement == null || !ChurchSettlements.IsChurchSettlement(settlement)) return false;

            var fugitives = GetFugitivesPresent(settlement);
            if (fugitives.Count == 0) return false;

            var target = fugitives.FirstOrDefault(IsAtWarWithPlayer) ?? fugitives[0];
            MBTextManager.SetTextVariable("FUGITIVE_NAME", target.Name);

            var enabled = IsAtWarWithPlayer(target);
            var tooltip = new TextObject("{=aG7cVp9S}You are not at war with {FUGITIVE_NAME}.");
            tooltip.SetTextVariable("FUGITIVE_NAME", target.Name);
            return MenuHelper.SetOptionProperties(args, enabled, !enabled, tooltip);
        }

        private void DragFugitive(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var target = GetFugitivesPresent(settlement).FirstOrDefault(IsAtWarWithPlayer);
            if (target == null) return;

            Untag(target);
            TakePrisonerAction.Apply(PartyBase.MainParty, target);
            ChurchCampaignBehavior.ApplySacrilege(Hero.MainHero, settlement, _churchSettingsProvider.GetSettings());
            GameMenu.SwitchToMenu("village");
        }

        private List<Hero> GetFugitivesPresent(Settlement settlement) =>
            _fugitiveSanctuaries
                .Where(pair => pair.Value == settlement && pair.Key.CurrentSettlement == settlement)
                .Select(pair => pair.Key)
                .ToList();

        private static bool IsAtWarWithPlayer(Hero hero) =>
            hero.MapFaction != null && hero.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);

        private static Settlement FindNearestChurchSettlement(Vec2 position)
        {
            Settlement nearest = null;
            var nearestDistanceSquared = float.MaxValue;
            foreach (var settlement in Settlement.All)
            {
                if (!ChurchSettlements.IsChurchSettlement(settlement)) continue;

                var distanceSquared = position.DistanceSquared(settlement.GetPosition2D);
                if (distanceSquared >= nearestDistanceSquared) continue;
                nearest = settlement;
                nearestDistanceSquared = distanceSquared;
            }

            return nearest;
        }

        private void Untag(Hero hero)
        {
            _fugitiveSanctuaries.Remove(hero);
            _fugitiveSanctuaryStarts.Remove(hero);
        }
    }
}
