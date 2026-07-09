using System;
using System.ComponentModel;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Integration.Titles.UI;
using Helpers;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.PrivateWars.UI.Mixins
{
    [ViewModelMixin]
    public sealed class SettlementNameplatePrivateWarMixin : BaseViewModelMixin<SettlementNameplateVM>
    {
        private const int NoPrivateWar = 0;
        private const int PrivateWarEnemy = 1;
        private const int PrivateWarAlly = 2;

        private readonly PropertyChangedEventHandler _propertyChangedHandler;
        private readonly Action _hostilityChangedHandler;

        public SettlementNameplatePrivateWarMixin(SettlementNameplateVM vm) : base(vm)
        {
            _propertyChangedHandler = OnBasePropertyChanged;
            _hostilityChangedHandler = OnPrivateWarHostilityChanged;

            vm.PropertyChanged += _propertyChangedHandler;
            if (FeudalUiServices.IsInitialised && FeudalUiServices.PrivateWarHostility is not null)
            {
                FeudalUiServices.PrivateWarHostility.Changed += _hostilityChangedHandler;
            }
        }

        [DataSourceProperty]
        public Color SettlementCapsuleColor
        {
            get
            {
                SettlementNameplateVM? vm = ViewModel;
                var colors = FeudalUiServices.PrivateWarNameplateColor;
                if (!FeudalUiServices.IsInitialised || vm is null || colors is null)
                {
                    return ArgbToColor(PrivateWarNameplateColorUseCase.VanillaNeutralSettlementArgb);
                }

                int privateWarState = ResolvePrivateWarState();
                uint argb = colors.GetSettlementCapsuleArgbColor(
                    ResolveVanillaRelation(vm),
                    privateWarState == PrivateWarEnemy,
                    privateWarState == PrivateWarAlly);

                return ArgbToColor(argb);
            }
        }

        [DataSourceProperty]
        public int SettlementCapsuleRelationType
        {
            get
            {
                SettlementNameplateVM? vm = ViewModel;
                if (vm is null)
                {
                    return (int)SettlementNameplateRelation.Neutral;
                }

                int privateWarState = ResolvePrivateWarState();
                if (privateWarState == PrivateWarEnemy)
                {
                    return (int)SettlementNameplateRelation.Enemy;
                }

                if (privateWarState == PrivateWarAlly)
                {
                    return (int)SettlementNameplateRelation.Ally;
                }

                return vm.Relation;
            }
        }

        public override void OnFinalize()
        {
            if (ViewModel is not null)
            {
                ViewModel.PropertyChanged -= _propertyChangedHandler;
            }

            if (FeudalUiServices.IsInitialised && FeudalUiServices.PrivateWarHostility is not null)
            {
                FeudalUiServices.PrivateWarHostility.Changed -= _hostilityChangedHandler;
            }

            base.OnFinalize();
        }

        private void OnBasePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(SettlementNameplateVM.Relation))
            {
                NotifyPrivateWarCapsuleProperties();
            }
        }

        private void OnPrivateWarHostilityChanged()
            => NotifyPrivateWarCapsuleProperties();

        private void NotifyPrivateWarCapsuleProperties()
        {
            OnPropertyChanged(nameof(SettlementCapsuleRelationType));
            OnPropertyChanged(nameof(SettlementCapsuleColor));
        }

        private int ResolvePrivateWarState()
        {
            SettlementNameplateVM? vm = ViewModel;
            if (!FeudalUiServices.IsInitialised || vm is null)
            {
                return NoPrivateWar;
            }

            Clan? mainClan = Hero.MainHero?.Clan;
            Clan? ownerClan = ResolveOwnerClan(vm);
            if (mainClan is null || ownerClan is null)
            {
                return NoPrivateWar;
            }

            var hostility = FeudalUiServices.PrivateWarHostility;
            if (hostility is null)
            {
                return NoPrivateWar;
            }

            if (hostility.AreEnemies(mainClan.StringId, ownerClan.StringId))
            {
                return PrivateWarEnemy;
            }

            if (hostility.AreAllies(mainClan.StringId, ownerClan.StringId))
            {
                return PrivateWarAlly;
            }

            return NoPrivateWar;
        }

        private static Clan? ResolveOwnerClan(SettlementNameplateVM vm)
            => vm.Settlement?.OwnerClan ?? vm.Settlement?.MapFaction as Clan;

        private static SettlementNameplateRelation ResolveVanillaRelation(SettlementNameplateVM vm)
        {
            IFaction? settlementFaction = vm.Settlement?.MapFaction;
            IFaction? mainFaction = Hero.MainHero?.MapFaction;
            if (vm.Settlement?.OwnerClan is null || settlementFaction is null || mainFaction is null)
            {
                return SettlementNameplateRelation.Neutral;
            }

            if (FactionManager.IsAtWarAgainstFaction(settlementFaction, mainFaction))
            {
                return SettlementNameplateRelation.Enemy;
            }

            if (DiplomacyHelper.IsSameFactionAndNotEliminated(settlementFaction, mainFaction))
            {
                return SettlementNameplateRelation.Ally;
            }

            return SettlementNameplateRelation.Neutral;
        }

        private static Color ArgbToColor(uint argb)
        {
            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;
            return Color.ConvertStringToColor(
                "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2"));
        }
    }
}
