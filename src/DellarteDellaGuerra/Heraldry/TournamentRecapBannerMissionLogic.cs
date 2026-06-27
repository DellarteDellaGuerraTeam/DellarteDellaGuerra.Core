using System.Reflection;
using SandBox.Tournaments.MissionLogics;
using SandBox.ViewModelCollection.Tournament;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Heraldry
{
    /// <summary>
    /// After a tournament ends, sets
    /// <see cref="TaleWorlds.Core.ViewModelCollection.CharacterViewModel.BannerCodeText"/>
    /// on the winner's recap character so that heraldic items (surcoats with
    /// <c>using_tableau="true"</c>) display the clan banner texture in the final
    /// tournament recap screen.
    ///
    /// Root cause: vanilla <c>TournamentVM.OnTournamentEnd</c> sets faction
    /// <c>ArmorColor1/2</c> on the winner's <see cref="CharacterViewModel"/> but never
    /// sets <c>BannerCodeText</c>, leaving the tableau renderer without a banner code
    /// and unable to apply heraldry to banner-replacement meshes on the winner's items.
    ///
    /// Approach: subscribe to <see cref="TournamentBehavior.TournamentEnd"/> and
    /// defer the fix by one mission tick so that <c>TournamentVM.OnTournamentEnd</c>
    /// has already run (setting <c>TournamentWinner</c>) before we read and update it.
    /// The <c>TournamentVM</c> instance is located at runtime via reflection on the
    /// mission's behavior list, avoiding a hard compile-time dependency on
    /// <c>SandBox.GauntletUI.MissionGauntletTournamentView</c>.
    /// </summary>
    public class TournamentRecapBannerMissionLogic : MissionLogic
    {
        private bool _pendingBannerUpdate;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            TournamentBehavior? tb = Mission.GetMissionBehavior<TournamentBehavior>();
            if (tb != null)
                tb.TournamentEnd += OnTournamentEnd;
        }

        protected override void OnEndMission()
        {
            TournamentBehavior? tb = Mission.GetMissionBehavior<TournamentBehavior>();
            if (tb != null)
                tb.TournamentEnd -= OnTournamentEnd;
        }

        private void OnTournamentEnd()
        {
            // Don't act immediately — TournamentVM.OnTournamentEnd (which sets
            // TournamentWinner) runs in the same event; defer to the next mission tick
            // to guarantee the VM's handler has already populated TournamentWinner.
            _pendingBannerUpdate = true;
        }

        public override void OnMissionTick(float dt)
        {
            if (!_pendingBannerUpdate)
                return;

            _pendingBannerUpdate = false;
            ApplyBannerToRecapWinner();
        }

        private void ApplyBannerToRecapWinner()
        {
            TournamentVM? vm = FindTournamentVM();
            if (vm == null) return;

            TournamentParticipantVM? winner = vm.TournamentWinner;
            if (winner?.Participant?.Character?.IsHero != true) return;

            Banner? clanBanner = winner.Participant.Character.HeroObject?.ClanBanner;
            if (clanBanner == null) return;

            // This triggers the live MVVM binding in the auto-generated Gauntlet UI
            // (Tournament__...TournamentVM.cs) which propagates BannerCodeText to the
            // CharacterTableau widget, causing it to render the banner on any
            // banner_replacement_mesh in the winner's equipped heraldic items.
            winner.Character.BannerCodeText = BannerCode.CreateFrom(clanBanner).Code;
        }

        /// <summary>
        /// Locates the <see cref="TournamentVM"/> held by the active tournament Gauntlet
        /// view without a compile-time reference to <c>SandBox.GauntletUI</c>.
        /// Iterates all mission behaviors and returns the first whose private
        /// <c>_dataSource</c> field holds a <see cref="TournamentVM"/> instance.
        /// </summary>
        private TournamentVM? FindTournamentVM()
        {
            foreach (MissionBehavior behavior in Mission.MissionBehaviors)
            {
                FieldInfo? field = behavior.GetType().GetField(
                    "_dataSource",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (field?.GetValue(behavior) is TournamentVM vm)
                    return vm;
            }
            return null;
        }
    }
}
