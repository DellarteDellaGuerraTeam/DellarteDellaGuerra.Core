using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Visor.Mission
{
    /// <summary>
    /// Plays the visor open/close animations on the player's agent when the toggle key is pressed, and opens the
    /// visor of lords the player talks to, swapping the helmet to its counterpart from <see cref="VisorHelmetResolver"/>.
    /// The animations come from the DellarteDellaGuerra content module
    /// (ModuleData/Animations/dadg_action_types.xml and dadg_action_sets.xml).
    /// </summary>
    public class VisorToggleMissionLogic : MissionLogic
    {
        // TODO: make it configurable
        private const InputKey ToggleKey = InputKey.V;
        private const int UpperBodyChannel = 1;
        internal const float SwapProgress = 0.35f;

        private static readonly ActionIndexCache VisorOpen =ActionIndexCache.Create("act_visor_open");
        private static readonly ActionIndexCache VisorOpenLeftStance = ActionIndexCache.Create("act_visor_open_leftstance");
        private static readonly ActionIndexCache VisorClose = ActionIndexCache.Create("act_visor_close");
        private static readonly ActionIndexCache VisorCloseLeftStance = ActionIndexCache.Create("act_visor_close_leftstance");
        private static readonly ActionIndexCache VisorOpenHorseback = ActionIndexCache.Create("act_visor_open_horseback");
        private static readonly ActionIndexCache VisorCloseHorseback = ActionIndexCache.Create("act_visor_close_horseback");

        private readonly ILogger _logger;
        private readonly VisorHelmetResolver _resolver;

        private readonly Dictionary<Agent, PendingSwap> _pendingSwaps = new();
        private bool _openVisorsForConversation;

        public VisorToggleMissionLogic(ILoggerFactory loggerFactory, VisorHelmetResolver resolver)
        {
            _logger = loggerFactory.CreateLogger<VisorToggleMissionLogic>();
            _resolver = resolver;
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);
            _openVisorsForConversation = Mission.Mode == MissionMode.Conversation;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_pendingSwaps.Count > 0)
                foreach (var pendingAgent in _pendingSwaps.Keys.ToList())
                    TickPendingSwap(pendingAgent, _pendingSwaps[pendingAgent]);

            // Mission conversations start after rendering has started, but the loading screen can still be up.
            if (_openVisorsForConversation && !LoadingWindow.IsLoadingWindowActive)
            {
                _openVisorsForConversation = false;
                OpenConversationPartnerVisors();
            }

            if (!Input.IsKeyPressed(ToggleKey)) return;

            var agent = Mission.MainAgent;
            if (agent is null || _pendingSwaps.ContainsKey(agent)) return;

            TryStartVisorAction(agent, null);
        }

        /// <summary>
        /// Plays the visor animation on <paramref name="agent"/> and swaps the helmet so the visor ends up
        /// open or closed. Returns false when nothing was started: no visor variant, already in that state,
        /// a visor action in progress, or the animation cannot play.
        /// </summary>
        public bool TrySetVisor(Agent agent, bool open)
        {
            return !_pendingSwaps.ContainsKey(agent) && TryStartVisorAction(agent, open);
        }

        private void OpenConversationPartnerVisors()
        {
            var conversationAgents = Campaign.Current?.ConversationManager?.ConversationAgents;
            if (conversationAgents is null) return;

            foreach (var conversationAgent in conversationAgents)
            {
                // A seated lord's pose would be broken by the upper-body action.
                if (conversationAgent is Agent { IsMainAgent: false, IsHero: true } agent && !agent.IsSitting())
                    TrySetVisor(agent, true);
            }
        }

        private bool TryStartVisorAction(Agent agent, bool? open)
        {
            if (!agent.IsActive() || !agent.IsHuman) return false;

            if (IsPlayingVisorAction(agent)) return false;

            if (!_resolver.TryResolve(agent.SpawnEquipment[EquipmentIndex.Head].Item, open, out var nextItem,
                    out var isOpen)) return false;

            var action = GetVisorAction(isOpen, agent.HasMount, agent.GetIsLeftStance());
            if (action == ActionIndexCache.act_none)
            {
                _logger.Error(
                    "Could not play the visor animation because the action is not registered. " +
                    "The DellarteDellaGuerra module needs the visor entries in ModuleData/Animations.");
                return false;
            }

            if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, in action)) return false;

            if (!agent.SetActionChannel(UpperBodyChannel, in action)) return false;

            _pendingSwaps[agent] = new PendingSwap(nextItem, action);
            return true;
        }

        // The equipment refresh cancels the upper-body action, so the swap waits for SwapProgress and the action
        // is then restarted from where it was cut.
        private void TickPendingSwap(Agent agent, PendingSwap pending)
        {
            if (!agent.IsActive())
            {
                _pendingSwaps.Remove(agent);
                return;
            }

            if (IsPlayingVisorAction(agent))
            {
                pending.ActionStarted = true;
                var progress = agent.GetCurrentActionProgress(UpperBodyChannel);
                if (progress < SwapProgress) return;

                var action = pending.Action;
                SwapPendingHelmet(agent, pending.Helmet);
                agent.SetActionChannel(UpperBodyChannel, in action, ignorePriority: true, blendInPeriod: 0f,
                    startProgress: progress);
                return;
            }

            // The action may not be reported on the tick it was set; wait until it has been seen playing.
            if (!pending.ActionStarted) return;

            // Interrupted before SwapProgress: the visor never moved, so keep the current helmet.
            _pendingSwaps.Remove(agent);
        }

        private void SwapPendingHelmet(Agent agent, ItemObject helmet)
        {
            // Clone rather than mutate: SpawnEquipment can be shared with the character's own equipment.
            var equipment = new Equipment(agent.SpawnEquipment);
            var head = equipment[EquipmentIndex.Head];
            equipment[EquipmentIndex.Head] = new EquipmentElement(helmet, head.ItemModifier);
            _pendingSwaps.Remove(agent);
            SwapSpawnEquipmentKeepingWeapons(agent, equipment);
        }

        /// <summary>
        /// UpdateSpawnEquipmentAndRefreshVisuals rebuilds every weapon slot from the spawn equipment at full
        /// ammo/hit points and re-wields the initial weapons. Put the live weapons (ammo, shield damage, reload
        /// state, pickups, lost items) and the wielded slots back afterwards so the toggle cannot be used as a refill.
        /// </summary>
        private static void SwapSpawnEquipmentKeepingWeapons(Agent agent, Equipment newSpawnEquipment)
        {
            var weapons = new MissionWeapon[(int)EquipmentIndex.NumAllWeaponSlots];
            for (var slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
                weapons[(int)slot] = agent.Equipment[slot];
            var mainHand = agent.GetPrimaryWieldedItemIndex();
            var offHand = agent.GetOffhandWieldedItemIndex();

            agent.UpdateSpawnEquipmentAndRefreshVisuals(newSpawnEquipment);

            for (var slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
                agent.EquipWeaponWithNewEntity(slot, ref weapons[(int)slot]);

            RestoreWieldedSlot(agent, Agent.HandIndex.OffHand, offHand);
            RestoreWieldedSlot(agent, Agent.HandIndex.MainHand, mainHand);
        }

        private static void RestoreWieldedSlot(Agent agent, Agent.HandIndex hand, EquipmentIndex slot)
        {
            if (slot == EquipmentIndex.None)
                agent.TryToSheathWeaponInHand(hand, Agent.WeaponWieldActionType.Instant);
            else
                agent.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.Instant, false);
        }

        private static ActionIndexCache GetVisorAction(bool isOpening, bool isMounted, bool isLeftStance)
        {
            if (isMounted) return isOpening ? VisorOpenHorseback : VisorCloseHorseback;
            if (isOpening) return isLeftStance ? VisorOpenLeftStance : VisorOpen;
            return isLeftStance ? VisorCloseLeftStance : VisorClose;
        }

        private static bool IsPlayingVisorAction(Agent agent)
        {
            var currentAction = agent.GetCurrentAction(UpperBodyChannel);
            return currentAction == VisorOpen
                   || currentAction == VisorOpenLeftStance
                   || currentAction == VisorClose
                   || currentAction == VisorCloseLeftStance
                   || currentAction == VisorOpenHorseback
                   || currentAction == VisorCloseHorseback;
        }

        private class PendingSwap
        {
            public PendingSwap(ItemObject helmet, ActionIndexCache action)
            {
                Helmet = helmet;
                Action = action;
            }

            public ItemObject Helmet { get; }
            public ActionIndexCache Action { get; }
            public bool ActionStarted { get; set; }
        }
    }
}
