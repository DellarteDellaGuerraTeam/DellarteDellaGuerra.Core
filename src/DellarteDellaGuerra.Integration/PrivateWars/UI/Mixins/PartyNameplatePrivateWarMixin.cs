using System;
using System.ComponentModel;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Integration.Titles.UI;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.PrivateWars.UI.Mixins
{
    [ViewModelMixin]
    public sealed class PartyNameplatePrivateWarMixin : BaseViewModelMixin<PartyNameplateVM>
    {
        private readonly PropertyChangedEventHandler _propertyChangedHandler;
        private readonly Action _hostilityChangedHandler;

        public PartyNameplatePrivateWarMixin(PartyNameplateVM vm) : base(vm)
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
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(NameplateVM.FactionColor))
            {
                OnPropertyChanged(nameof(PrivateWarFontColor));
            }
        }

        private void OnPrivateWarHostilityChanged()
            => OnPropertyChanged(nameof(PrivateWarFontColor));

        private static Clan? ResolveOwnerClan(PartyNameplateVM vm)
            => vm.Party?.ActualClan ?? vm.Party?.LeaderHero?.Clan;

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
