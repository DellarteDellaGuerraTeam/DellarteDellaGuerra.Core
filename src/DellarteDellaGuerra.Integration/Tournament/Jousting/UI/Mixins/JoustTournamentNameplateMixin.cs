using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Tournament.Jousting.Api.Campaign;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.Tournament.Jousting.UI.Mixins;

/// <summary>
/// Marks the settlement nameplate's Tournament event item as a jousting tournament.
///
/// Vanilla adds that item from two places — SettlementNameplateEventsVM.PopulateEventList on
/// registration, and OnTournamentStarted when a tournament begins mid-session — and both go
/// through EventsList.Add. Subscribing to the list instead of hooking either method keeps this
/// free of any dependency on private vanilla members.
///
/// The item's Type is repurposed to the vanilla Production code path (case 6 in UpdateVisual
/// calls SetState(AdditionalParameters)), which selects the "JoustTournament" brush style
/// registered by <see cref="JoustTournamentNameplateBrushRegistrar"/> — showing the horse icon.
/// EventType stays Tournament so vanilla UnloadEvents/remove logic works unchanged.
/// </summary>
[ViewModelMixin]
public sealed class JoustTournamentNameplateMixin : BaseViewModelMixin<SettlementNameplateVM>
{
    /// <summary>
    /// Type integer that routes through the Production code path in UpdateVisual
    /// (case 6: SetState(AdditionalParameters)).
    /// </summary>
    private const int ProductionType = 6;

    private readonly MBBindingList<SettlementNameplateEventItemVM>? _eventsList;
    private readonly ListChangedEventHandler _eventsListChangedHandler;

    public JoustTournamentNameplateMixin(SettlementNameplateVM vm) : base(vm)
    {
        _eventsListChangedHandler = OnEventsListChanged;

        // SettlementEvents and its EventsList are both created in the SettlementNameplateVM
        // constructor, and the list instance is never replaced, so one subscription covers
        // the whole lifetime of the view model.
        _eventsList = vm.SettlementEvents?.EventsList;
        if (_eventsList is not null)
        {
            _eventsList.ListChanged += _eventsListChangedHandler;
        }
    }

    public override void OnFinalize()
    {
        if (_eventsList is not null)
        {
            _eventsList.ListChanged -= _eventsListChangedHandler;
        }

        base.OnFinalize();
    }

    private void OnEventsListChanged(object sender, ListChangedEventArgs e)
    {
        if (e.ListChangedType != ListChangedType.ItemAdded) return;
        MarkJoustingItem();
    }

    private void MarkJoustingItem()
    {
        var eventsList = _eventsList;
        var settlement = ViewModel?.Settlement;
        if (eventsList is null || settlement?.Town is null) return;

        if (Campaign.Current?.TournamentManager?.GetTournamentGame(settlement.Town) is not JoustTournament) return;

        foreach (var item in eventsList)
        {
            if (item.EventType != SettlementNameplateEventItemVM.SettlementEventType.Tournament) continue;

            item.Type = ProductionType;
            item.AdditionalParameters = JoustTournamentNameplateBrushRegistrar.JoustTournamentState;
            break;
        }
    }
}
