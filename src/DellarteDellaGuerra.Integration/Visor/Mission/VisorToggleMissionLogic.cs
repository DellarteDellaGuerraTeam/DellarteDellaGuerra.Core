using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Visor;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Visor.Mission
{
    /// <summary>
    /// Plays the visor open/close animations on the player's agent when the toggle key is pressed,
    /// and swaps the equipped helmet to its open/closed counterpart from <see cref="VisorVariantCatalog"/>.
    /// The animations come from the DellarteDellaGuerra content module
    /// (ModuleData/Animations/dadg_action_types.xml and dadg_action_sets.xml).
    /// </summary>
    public class VisorToggleMissionLogic : MissionLogic
    {
        // TODO: make it configurable
        private const InputKey ToggleKey = InputKey.V;
        private const int UpperBodyChannel = 1;
        private const float SwapProgress = 0.35f;

        private static readonly ActionIndexCache VisorOpen = ActionIndexCache.Create("act_visor_open");
        private static readonly ActionIndexCache VisorOpenLeftStance = ActionIndexCache.Create("act_visor_open_leftstance");
        private static readonly ActionIndexCache VisorClose = ActionIndexCache.Create("act_visor_close");
        private static readonly ActionIndexCache VisorCloseLeftStance = ActionIndexCache.Create("act_visor_close_leftstance");
        private static readonly ActionIndexCache VisorOpenHorseback = ActionIndexCache.Create("act_visor_open_horseback");
        private static readonly ActionIndexCache VisorCloseHorseback = ActionIndexCache.Create("act_visor_close_horseback");

        private readonly ILogger _logger;
        private readonly VisorVariantCatalog _catalog;

        private Agent? _pendingAgent;
        private ItemObject? _pendingHelmet;
        private ActionIndexCache _pendingAction;
        private bool _pendingActionStarted;

        public VisorToggleMissionLogic(ILoggerFactory loggerFactory, VisorVariantCatalog catalog)
        {
            _logger = loggerFactory.CreateLogger<VisorToggleMissionLogic>();
            _catalog = catalog;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_pendingAgent is not null)
            {
                TickPendingSwap();
                return;
            }

            if (!Input.IsKeyPressed(ToggleKey)) return;

            var agent = Mission.MainAgent;
            if (agent is null || !agent.IsActive() || !agent.IsHuman) return;

            if (IsPlayingVisorAction(agent)) return;

            var head = agent.SpawnEquipment[EquipmentIndex.Head];
            var currentItemId = head.Item?.StringId;
            if (currentItemId is null || !_catalog.TryFindPair(currentItemId, out var pair)) return;

            var result = VisorToggleResult.From(pair, currentItemId);
            var nextItem = MBObjectManager.Instance.GetObject<ItemObject>(result.NextItemId);
            if (nextItem is null)
            {
                _logger.Error(
                    $"Could not toggle the visor of '{currentItemId}' because item '{result.NextItemId}' does not exist. " +
                    "Check dadg_visor_variants.xml against the item XML.");
                return;
            }

            var action = GetVisorAction(result.IsOpen, agent.HasMount, agent.GetIsLeftStance());
            if (action == ActionIndexCache.act_none)
            {
                _logger.Error(
                    "Could not play the visor animation because the action is not registered. " +
                    "The DellarteDellaGuerra module needs the visor entries in ModuleData/Animations.");
                return;
            }

            if (!agent.SetActionChannel(UpperBodyChannel, in action)) return;

            _pendingAgent = agent;
            _pendingHelmet = nextItem;
            _pendingAction = action;
            _pendingActionStarted = false;
        }

        // The equipment refresh cancels the upper-body action, so the swap waits for SwapProgress and the action
        // is then restarted from where it was cut.
        private void TickPendingSwap()
        {
            var agent = _pendingAgent!;
            if (!agent.IsActive())
            {
                ClearPendingSwap();
                return;
            }

            if (IsPlayingVisorAction(agent))
            {
                _pendingActionStarted = true;
                var progress = agent.GetCurrentActionProgress(UpperBodyChannel);
                if (progress < SwapProgress) return;

                var action = _pendingAction;
                SwapPendingHelmet(agent);
                agent.SetActionChannel(UpperBodyChannel, in action, ignorePriority: true, blendInPeriod: 0f,
                    startProgress: progress);
                return;
            }

            // The action may not be reported on the tick it was set; wait until it has been seen playing.
            if (!_pendingActionStarted) return;

            // Interrupted before SwapProgress: the visor never moved, so keep the current helmet.
            ClearPendingSwap();
        }

        private void SwapPendingHelmet(Agent agent)
        {
            // Clone rather than mutate: SpawnEquipment can be shared with the character's own equipment.
            var equipment = new Equipment(agent.SpawnEquipment);
            var head = equipment[EquipmentIndex.Head];
            equipment[EquipmentIndex.Head] = new EquipmentElement(_pendingHelmet, head.ItemModifier);
            ClearPendingSwap();
            SwapSpawnEquipmentKeepingWeapons(agent, equipment);
        }

        private void ClearPendingSwap()
        {
            _pendingAgent = null;
            _pendingHelmet = null;
            _pendingActionStarted = false;
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
    }
}
