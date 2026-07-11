using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Integration.Titles.UI;
using Helpers;
using NLog;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Logger = NLog.Logger;

namespace DellarteDellaGuerra.Integration.PrivateWars.UI.Mixins
{
    [ViewModelMixin(nameof(PartyNameplateVM.InitializeWith))]
    public sealed class PartyNameplatePrivateWarMixin : BaseViewModelMixin<PartyNameplateVM>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly PropertyChangedWithValueEventHandler _propertyChangedHandler;
        private readonly Action _hostilityChangedHandler;

        public PartyNameplatePrivateWarMixin(PartyNameplateVM vm) : base(vm)
        {
            _propertyChangedHandler = OnBasePropertyChanged;
            _hostilityChangedHandler = OnPrivateWarHostilityChanged;
        }

        [DataSourceProperty]
        public string PrivateWarFontColor
        {
            get
            {
                PartyNameplateVM? vm = ViewModel;
                string neutralColor = vm?.FactionColor ?? "#FFFFFFFF";
                if (!FeudalUiServices.IsInitialised || vm is null)
                {
                    return neutralColor;
                }

                MobileParty? party = vm.Party;
                IFaction? mainFaction = Hero.MainHero?.MapFaction;
                if (party?.HomeSettlement is not null
                    && (mainFaction is null || party.HomeSettlement.MapFaction != mainFaction))
                {
                    return neutralColor;
                }

                Clan? mainClan = Hero.MainHero?.Clan;
                Clan? ownerClan = ResolveOwnerClan(vm);
                if (mainClan is null || ownerClan is null)
                {
                    return neutralColor;
                }

                var hostility = FeudalUiServices.PrivateWarHostility;
                var colors = FeudalUiServices.PrivateWarNameplateColor;
                if (hostility is null || colors is null)
                {
                    return neutralColor;
                }

                if (hostility.AreEnemies(mainClan.StringId, ownerClan.StringId))
                {
                    return ArgbToFactionColorString(colors.GetPrivateWarEnemyArgbColor());
                }

                if (hostility.AreAllies(mainClan.StringId, ownerClan.StringId))
                {
                    return ArgbToFactionColorString(colors.GetPrivateWarAllyArgbColor());
                }

                return neutralColor;
            }
        }

        public override void OnRefresh()
        {
            if (ViewModel is not null)
            {
                ViewModel.PropertyChangedWithValue += _propertyChangedHandler;
            }

            if (FeudalUiServices.IsInitialised && FeudalUiServices.PrivateWarHostility is not null)
            {
                FeudalUiServices.PrivateWarHostility.Changed += _hostilityChangedHandler;
            }

            OnPropertyChanged(nameof(PrivateWarFontColor));
            LogColorState("InitializeWith");
            base.OnRefresh();
        }

        public override void OnFinalize()
        {
            if (ViewModel is not null)
            {
                ViewModel.PropertyChangedWithValue -= _propertyChangedHandler;
            }

            if (FeudalUiServices.IsInitialised && FeudalUiServices.PrivateWarHostility is not null)
            {
                FeudalUiServices.PrivateWarHostility.Changed -= _hostilityChangedHandler;
            }

            base.OnFinalize();
        }

        private void OnBasePropertyChanged(object? sender, PropertyChangedWithValueEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(NameplateVM.FactionColor))
            {
                OnPropertyChanged(nameof(PrivateWarFontColor));
                LogColorState("FactionColorChanged");
            }
        }

        private void OnPrivateWarHostilityChanged()
        {
            OnPropertyChanged(nameof(PrivateWarFontColor));
            LogColorState("PrivateWarHostilityChanged");
        }

        private void LogColorState(string trigger)
        {
            PartyNameplateVM? vm = ViewModel;
            MobileParty? party = vm?.Party;
            if (vm is null || party is null || Campaign.Current is null)
            {
                Logger.Info($"[PartyNameplateColor] trigger={trigger} party=<unavailable> baseVmColor={vm?.FactionColor ?? "<null>"}");
                return;
            }

            Clan? mainClan = Hero.MainHero?.Clan;
            Clan? ownerClan = ResolveOwnerClan(vm);
            IFaction? mainFaction = Hero.MainHero?.MapFaction;
            IFaction? partyFaction = party.MapFaction;
            bool atWar = FactionManager.IsAtWarAgainstFaction(partyFaction, mainFaction);
            bool constantWar = FactionManager.IsAtConstantWarAgainstFaction(partyFaction, mainFaction);
            bool sameFaction = partyFaction is not null
                               && mainFaction is not null
                               && DiplomacyHelper.IsSameFactionAndNotEliminated(partyFaction, mainFaction);

            var hostility = FeudalUiServices.PrivateWarHostility;
            bool privateWarSettlementScope = party.HomeSettlement is null
                                             || (mainFaction is not null
                                                 && party.HomeSettlement.MapFaction == mainFaction);
            bool privateEnemy = mainClan is not null
                                && ownerClan is not null
                                && privateWarSettlementScope
                                && hostility?.AreEnemies(mainClan.StringId, ownerClan.StringId) == true;
            bool privateAlly = mainClan is not null
                               && ownerClan is not null
                               && privateWarSettlementScope
                               && hostility?.AreAllies(mainClan.StringId, ownerClan.StringId) == true;

            bool isArmy = party.Army?.LeaderParty == party;
            string logicalColor;
            string logicalReason;
            if (privateEnemy && FeudalUiServices.PrivateWarNameplateColor is not null)
            {
                logicalColor = ArgbToFactionColorString(
                    FeudalUiServices.PrivateWarNameplateColor.GetPrivateWarEnemyArgbColor());
                logicalReason = "private-war-enemy";
            }
            else if (privateAlly && FeudalUiServices.PrivateWarNameplateColor is not null)
            {
                logicalColor = ArgbToFactionColorString(
                    FeudalUiServices.PrivateWarNameplateColor.GetPrivateWarAllyArgbColor());
                logicalReason = "private-war-ally";
            }
            else if (party.IsMainParty)
            {
                logicalColor = isArmy ? PartyNameplateVM.MainPartyArmyIndicator : PartyNameplateVM.MainPartyIndicator;
                logicalReason = "main-party";
            }
            else if (atWar)
            {
                logicalColor = isArmy ? PartyNameplateVM.NegativeArmyIndicator : PartyNameplateVM.NegativeIndicator;
                logicalReason = "faction-war";
            }
            else if (sameFaction)
            {
                logicalColor = isArmy ? PartyNameplateVM.PositiveArmyIndicator : PartyNameplateVM.PositiveIndicator;
                logicalReason = "same-faction";
            }
            else
            {
                logicalColor = isArmy ? PartyNameplateVM.NeutralArmyIndicator : PartyNameplateVM.NeutralIndicator;
                logicalReason = "neutral";
            }

            string actualBoundColor = PrivateWarFontColor;
            string message =
                $"[PartyNameplateColor] trigger={trigger} party=\"{party.Name}\" partyId={party.StringId} " +
                $"component={party.PartyComponent?.GetType().Name ?? "<none>"} actualClan={ownerClan?.StringId ?? "<none>"} " +
                $"mapFaction={partyFaction?.StringId ?? "<none>"} mainFaction={mainFaction?.StringId ?? "<none>"} " +
                $"isBandit={party.IsBandit} isDeserter={ownerClan?.StringId == "deserters"} " +
                $"baseVmColor={vm.FactionColor ?? "<null>"} actualBoundColor={actualBoundColor} " +
                $"logicalColor={logicalColor} logicalReason={logicalReason} " +
                $"atWar={atWar} constantWar={constantWar} sameFaction={sameFaction} " +
                $"privateEnemy={privateEnemy} privateAlly={privateAlly}";

            if (string.Equals(actualBoundColor, logicalColor, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info(message);
            }
            else
            {
                Logger.Warn(message);
            }
        }

        private static Clan? ResolveOwnerClan(PartyNameplateVM vm)
        {
            MobileParty? party = vm.Party;
            return party?.ActualClan ?? party?.LeaderHero?.Clan ?? party?.HomeSettlement?.OwnerClan;
        }

        private static string ArgbToFactionColorString(uint argb)
        {
            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;
            return "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2");
        }
    }
}
