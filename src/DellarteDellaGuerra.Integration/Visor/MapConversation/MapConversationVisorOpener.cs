using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Integration.Visor.Mission;
using HarmonyLib;
using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace DellarteDellaGuerra.Integration.Visor.MapConversation
{
    /// <summary>
    /// Opens the visor of the lord the player talks to on the campaign map. The lord is an <see cref="AgentVisuals"/>
    /// inside <see cref="MapConversationTableau"/>, so the visor action is played on its skeleton and the helmet is
    /// swapped by refreshing the visuals, at the same beat as <see cref="VisorToggleMissionLogic"/>.
    /// </summary>
    public class MapConversationVisorOpener
    {
        // Lets the conversation screen settle once the lord is loaded, so the player sees the whole gesture.
        private const float StartDelaySeconds = 0.5f;
        // The visor clips only animate the upper body: on channel 0 the lord loses his pose and lies flat.
        private const int UpperBodyChannel = 1;
        // The tableau does not fire the sounds keyed in its clips (even with animation sounds activated), and the
        // engine accepts the visor_open module sound there but plays nothing, 2D or 3D. So the clip's sound files
        // are played from file. The voice lines' event plays one sound at a time, so the external-file event is
        // tried first.
        private const string ExternalSoundEvent = "event:/Extra/external";
        private const string VoiceOverEvent = "event:/Extra/voiceover";
        private const string VisorOpenSoundFiles = "DellarteDellaGuerra/ModuleSounds/Visor/visoropen_0{0}.ogg";
        private const int VisorOpenSoundFileCount = 2;
        // Used when the clip declares no blend out; the shortest fade that hides the snap back to the idle.
        private const float MinBlendOutSeconds = 0.25f;
        private const float SwapTickSeconds = 0.001f;

        // Resolved on use: IsSupported runs the static initializers during module load, before the engine has read
        // the action types.
        private const string VisorOpenAction = "act_visor_open";

        private static readonly AccessTools.FieldRef<MapConversationTableau, List<AgentVisuals>>? AgentVisualsField =
            Field<List<AgentVisuals>>("_agentVisuals");
        private static readonly AccessTools.FieldRef<MapConversationTableau, string>? EquipmentCacheField =
            Field<string>("_opponentLeaderEquipmentCache");
        private static readonly AccessTools.FieldRef<MapConversationTableau, MapConversationTableauData>? DataField =
            Field<MapConversationTableauData>("_data");
        private static readonly AccessTools.FieldRef<MapConversationTableau, bool>? InitializedField =
            Field<bool>("_initialized");
        private static readonly AccessTools.FieldRef<MapConversationTableau, bool>? IsEnabledField =
            Field<bool>("_isEnabled");
        private static readonly AccessTools.FieldRef<MapConversationTableau, GameEntity>? CameraEntityField =
            Field<GameEntity>("_cameraEntity");
        private static readonly AccessTools.FieldRef<MapConversationTableau, TaleWorlds.Engine.Scene>? TableauSceneField =
            Field<TaleWorlds.Engine.Scene>("_tableauScene");

        private readonly ILogger _logger;
        private readonly VisorHelmetResolver _resolver;
        private readonly ConditionalWeakTable<MapConversationTableau, State> _states = new();
        private readonly Random _random = new();

        public MapConversationVisorOpener(ILoggerFactory loggerFactory, VisorHelmetResolver resolver)
        {
            _logger = loggerFactory.CreateLogger<MapConversationVisorOpener>();
            _resolver = resolver;
        }

        /// <summary>
        /// False when a private member of <see cref="MapConversationTableau"/> was not found, i.e. the game changed.
        /// Without the equipment cache the swap would make vanilla respawn the lord every tick.
        /// </summary>
        public static bool IsSupported => AgentVisualsField is not null && EquipmentCacheField is not null
                                   && DataField is not null && InitializedField is not null
                                   && IsEnabledField is not null && CameraEntityField is not null;

        /// <summary>Runs after <see cref="MapConversationTableau.OnTick"/>.</summary>
        public void Tick(MapConversationTableau tableau, float dt)
        {
            if (!IsEnabledField!(tableau) || !InitializedField!(tableau)) return;

            var agentVisualsList = AgentVisualsField!(tableau);
            if (agentVisualsList is null || agentVisualsList.Count == 0) return;
            var leader = agentVisualsList[0];

            // Vanilla respawns the leader on SetData or an equipment change: a new instance is a new conversation.
            var state = _states.GetValue(tableau, _ => new State());
            if (state.Leader != leader) state.Reset(leader);

            try
            {
                switch (state.Phase)
                {
                    case Phase.Waiting:
                        TickWaiting(tableau, leader, state, dt);
                        break;
                    case Phase.Playing:
                        TickPlaying(tableau, leader, state);
                        break;
                }
            }
            catch (Exception e)
            {
                state.Phase = Phase.Done;
                _logger.Error("Could not open the visor of the map conversation partner.", e);
            }
        }

        private void TickWaiting(MapConversationTableau tableau, AgentVisuals leader, State state, float dt)
        {
            // The tableau renders over the map; wait until the loading screen is gone and the lord is drawable.
            if (LoadingWindow.IsLoadingWindowActive || !leader.GetVisuals().CheckResources(true)) return;

            state.Elapsed += dt;
            if (state.Elapsed < StartDelaySeconds) return;

            state.Phase = Phase.Done;

            var character = DataField!(tableau)?.ConversationPartnerData.Character;
            if (character is null || !character.IsHero) return;

            var head = leader.GetEquipment()?[EquipmentIndex.Head] ?? EquipmentElement.Invalid;
            if (!_resolver.TryResolve(head.Item, true, out var openHelmet, out _)) return;

            var skeleton = leader.GetVisuals().GetSkeleton();
            if (skeleton is null) return;
            var actionSet = leader.GetCopyAgentVisualsData().ActionSetData;
            var action = ActionIndexCache.Create(VisorOpenAction);
            if (action == ActionIndexCache.act_none || !MBActionSet.CheckActionAnimationClipExists(actionSet, in action))
            {
                _logger.Error(
                    "Could not open the visor because the visor animation is missing. " +
                    "The DellarteDellaGuerra module needs act_visor_open in ModuleData/Animations.");
                return;
            }

            state.Helmet = new EquipmentElement(openHelmet, head.ItemModifier);
            state.Action = action;
            state.Duration = MBActionSet.GetActionAnimationDuration(actionSet, in action);
            var blendOutStart = MBActionSet.GetActionBlendOutStartProgress(actionSet, in action);
            state.BlendOutStart = blendOutStart > 0f && blendOutStart < 1f
                ? blendOutStart
                : 1f - MinBlendOutSeconds / Math.Max(state.Duration, MinBlendOutSeconds);
            MBSkeletonExtensions.SetAgentActionChannel(skeleton, UpperBodyChannel, in action, 0f, -0.2f, false, 0f);
            state.Phase = Phase.Playing;
        }

        // The gesture plays on the upper-body channel over vanilla's channel 0 idle, as on mission agents, so
        // conversation lines keep driving the idle. If the gesture is cut short the helmet is swapped anyway so
        // the visor never stays shut.
        private void TickPlaying(MapConversationTableau tableau, AgentVisuals leader, State state)
        {
            var skeleton = leader.GetVisuals().GetSkeleton();
            var isPlayingVisor = skeleton is not null
                                 && skeleton.GetActionAtChannel(UpperBodyChannel) == state.Action;

            if (!isPlayingVisor)
            {
                // The action may not be reported on the tick it was set; wait until it has been seen playing.
                if (!state.ActionStarted) return;
                if (!state.Swapped) OpenVisor(tableau, leader, state, -1f);
                state.Phase = Phase.Done;
                return;
            }

            state.ActionStarted = true;
            var progress = skeleton!.GetAnimationParameterAtChannel(UpperBodyChannel);

            if (!state.Swapped)
            {
                if (progress < VisorToggleMissionLogic.SwapProgress) return;
                OpenVisor(tableau, leader, state, progress);
                state.Swapped = true;
                return;
            }

            // Mission agents fade the gesture out; the tableau snaps the torso straight back onto the idle.
            if (progress < state.BlendOutStart) return;
            var blendOut = Math.Max(state.Duration * (1f - progress), MinBlendOutSeconds);
            var none = ActionIndexCache.act_none;
            MBSkeletonExtensions.SetAgentActionChannel(leader.GetVisuals().GetSkeleton(), UpperBodyChannel, in none,
                0f, blendOut, false, 0f);
            state.Phase = Phase.Done;
        }

        private void OpenVisor(MapConversationTableau tableau, AgentVisuals leader, State state, float gestureProgress)
        {
            SwapHelmet(tableau, leader, state.Helmet, state.Action, gestureProgress);

            PlayVisorSound(tableau);
        }

        private void PlayVisorSound(MapConversationTableau tableau)
        {
            var scene = TableauSceneField?.Invoke(tableau);
            if (scene is null) return;

            var file = System.IO.Path.Combine(BasePath.Name, "Modules",
                string.Format(VisorOpenSoundFiles, _random.Next(VisorOpenSoundFileCount) + 1));
            if (!System.IO.File.Exists(file))
            {
                _logger.Error($"Could not play the visor sound because {file} does not exist.");
                return;
            }
            if (SoundEvent.CreateEventFromExternalFile(ExternalSoundEvent, file, scene, true, false).Play()) return;
            SoundEvent.CreateEventFromExternalFile(VoiceOverEvent, file, scene, true, false).Play();
        }

        // Refresh rebuilds the skeleton and only puts back the channel 0 action, so the gesture is restarted
        // from <paramref name="gestureProgress"/> (negative: not restarted).
        private static void SwapHelmet(MapConversationTableau tableau, AgentVisuals leader, EquipmentElement helmet,
            ActionIndexCache action, float gestureProgress)
        {
            var oldSkeleton = leader.GetVisuals().GetSkeleton();
            var faceAnimation = oldSkeleton is null ? null : MBSkeletonExtensions.GetSkeletonFaceAnimationName(oldSkeleton);

            var equipment = leader.GetEquipment().Clone(false);
            equipment[EquipmentIndex.Head] = helmet;
            var data = leader.GetCopyAgentVisualsData();
            data.Equipment(equipment);
            leader.Refresh(false, data);

            // OnTick respawns the leader from the hero's equipment when it no longer matches this cache.
            EquipmentCacheField!(tableau) = equipment.CalculateEquipmentCode();

            // Re-apply what SpawnOpponentLeader set up after AgentVisuals.Create.
            var visuals = leader.GetVisuals();
            var skeleton = visuals.GetSkeleton();
            if (skeleton is not null)
            {
                // Restart the gesture before the forced update: meshes added by Refresh are posed by that update,
                // and posing them on the idle alone leaves the gauntlets behind until the gesture ends.
                if (gestureProgress >= 0f)
                    MBSkeletonExtensions.SetAgentActionChannel(skeleton, UpperBodyChannel, in action,
                        gestureProgress, 0f, false, 0f);
                if (!string.IsNullOrEmpty(faceAnimation))
                    skeleton.SetFacialAnimation(Agent.FacialAnimChannel.Mid, faceAnimation, false, true);
                // A zero-length tick leaves the restarted gesture at zero weight: the lord pops onto the idle for
                // that frame.
                skeleton.TickAnimationsAndForceUpdate(SwapTickSeconds, visuals.GetGlobalFrame(), true);
            }

            var cameraEntity = CameraEntityField!(tableau);
            if (cameraEntity is not null)
                leader.SetLookDirection(cameraEntity.GetGlobalFrame().origin - leader.GetGlobalStableEyePoint(true));
            leader.SetAgentLodZeroOrMaxExternal(true);
            visuals.LazyUpdateAgentRendererData();
        }

        private static AccessTools.FieldRef<MapConversationTableau, T>? Field<T>(string name)
        {
            var field = AccessTools.DeclaredField(typeof(MapConversationTableau), name);
            return field is not null && field.FieldType == typeof(T)
                ? AccessTools.FieldRefAccess<MapConversationTableau, T>(field)
                : null;
        }

        private enum Phase
        {
            Waiting,
            Playing,
            Done
        }

        private class State
        {
            public AgentVisuals? Leader { get; private set; }
            public Phase Phase { get; set; }
            public float Elapsed { get; set; }
            public bool ActionStarted { get; set; }
            public bool Swapped { get; set; }
            public EquipmentElement Helmet { get; set; }
            public ActionIndexCache Action { get; set; } = ActionIndexCache.act_none;
            public float Duration { get; set; }
            public float BlendOutStart { get; set; }

            public void Reset(AgentVisuals leader)
            {
                Leader = leader;
                Phase = Phase.Waiting;
                Elapsed = 0f;
                ActionStarted = false;
                Swapped = false;
                Helmet = EquipmentElement.Invalid;
                Action = ActionIndexCache.act_none;
                Duration = 0f;
                BlendOutStart = 1f;
            }
        }
    }
}
