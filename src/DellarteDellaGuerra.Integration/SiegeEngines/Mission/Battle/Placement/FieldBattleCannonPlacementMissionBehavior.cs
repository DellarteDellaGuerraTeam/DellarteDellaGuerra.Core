using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Battle.Placement;

public class FieldBattleCannonPlacementMissionBehavior : MissionView
{
    private const string CannonPrefabPrefix = "dadg_";
    private const string GhostPrefabSuffix = "_ghost";
    private const float PreviewAlphaValid = 0.6f;
    private const float PreviewAlphaInvalid = 0.25f;
    private const float RotationStepInRadians = 0.1f;
    private const int RemoveEntityReason = 82;

    private const InputKey TogglePlacementModeKey = InputKey.B;
    private const InputKey CycleCannonTypeKey = InputKey.N;
    private const InputKey RotateLeftKey = InputKey.Q;
    private const InputKey RotateRightKey = InputKey.E;
    private const InputKey PlaceCannonKey = InputKey.LeftMouseButton;
    private const InputKey CancelPlacementModeKey = InputKey.RightMouseButton;
    private const InputKey RemoveLastPlacedCannonKey = InputKey.K;
    private const InputKey RepositionLastPlacedCannonKey = InputKey.R;

    private readonly IFieldBattleCannonPlacementSettingsProvider _settingsProvider;
    private readonly List<string> _availableCannonIds;
    private readonly ILogger _logger;

    private readonly List<PlacedCannon> _placedCannons = new();

    private OrderTroopPlacer? _orderTroopPlacer;
    private bool _wasTroopPlacerSuspended;
    private bool _isPlacementModeActive;
    private int _selectedCannonIndex;
    private float _previewYawInRadians;

    private GameEntity? _previewEntity;
    private string? _previewPrefabName;

    public FieldBattleCannonPlacementMissionBehavior(
        IFieldBattleCannonPlacementSettingsProvider settingsProvider,
        ICannonRegistry cannonRegistry,
        ILoggerFactory loggerFactory)
    {
        _settingsProvider = settingsProvider;
        _logger = loggerFactory.CreateLogger<FieldBattleCannonPlacementMissionBehavior>();
        _availableCannonIds = cannonRegistry.GetAllCannons()
            .Select(c => c.Id)
            .Distinct()
            .ToList();
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);

        if (!IsEligibleFieldBattleDeployment())
        {
            DeactivatePlacementMode(showMessage: false);
            return;
        }

        if (!_settingsProvider.IsEnabled)
        {
            DeactivatePlacementMode(showMessage: false);
            return;
        }

        if (Input.IsKeyPressed(TogglePlacementModeKey))
            TogglePlacementMode();

        if (!_isPlacementModeActive)
            return;

        PruneInvalidPlacedCannons();
        HandleCannonTypeCycling();
        HandlePreviewRotation();
        HandleCancelPlacementMode();

        if (!_isPlacementModeActive)
            return;

        bool hasGroundHit = TryGetMouseGroundPosition(out WorldPosition targetWorldPosition);
        bool isInsideDeploymentBoundaries = hasGroundHit && IsInsidePlayerDeploymentBoundaries(targetWorldPosition);

        UpdatePreviewEntity(hasGroundHit, targetWorldPosition, isInsideDeploymentBoundaries);
        HandleRemovePlacedCannon();
        HandleRepositionPlacedCannon(hasGroundHit, targetWorldPosition, isInsideDeploymentBoundaries);
        HandlePlaceCannon(hasGroundHit, targetWorldPosition, isInsideDeploymentBoundaries);
    }

    public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
    {
        base.OnMissionModeChange(oldMissionMode, atStart);

        if (oldMissionMode == MissionMode.Deployment && Mission.Mode != MissionMode.Deployment)
            DeactivatePlacementMode(showMessage: false);
    }

    public override void OnRemoveBehavior()
    {
        DeactivatePlacementMode(showMessage: false);
        RemovePreviewEntity();
        base.OnRemoveBehavior();
    }

    private void TogglePlacementMode()
    {
        if (_isPlacementModeActive)
        {
            DeactivatePlacementMode();
            return;
        }

        if (_availableCannonIds.Count == 0)
        {
            ShowQuickInfo("dadg_cannon_placement_no_type",
                "No configured cannon type is available for placement.");
            return;
        }

        ActivatePlacementMode();
    }

    private void ActivatePlacementMode()
    {
        _isPlacementModeActive = true;
        _previewYawInRadians = 0f;
        _selectedCannonIndex = _selectedCannonIndex < 0
            ? 0
            : (_selectedCannonIndex >= _availableCannonIds.Count ? _availableCannonIds.Count - 1 : _selectedCannonIndex);

        SuspendTroopPlacer();

        ShowQuickInfo("dadg_cannon_placement_enabled",
            "Cannon placement enabled. B: exit | N: next cannon | Q/E: rotate | Left click: place | R: move last | K: remove last.");
    }

    private void DeactivatePlacementMode(bool showMessage = true)
    {
        if (!_isPlacementModeActive)
            return;

        _isPlacementModeActive = false;
        RestoreTroopPlacer();
        RemovePreviewEntity();

        if (showMessage)
            ShowQuickInfo("dadg_cannon_placement_disabled", "Cannon placement disabled.");
    }

    private void SuspendTroopPlacer()
    {
        _orderTroopPlacer ??= Mission.GetMissionBehavior<OrderTroopPlacer>();
        if (_orderTroopPlacer == null)
            return;

        _wasTroopPlacerSuspended = _orderTroopPlacer.SuspendTroopPlacer;
        _orderTroopPlacer.SuspendTroopPlacer = true;
    }

    private void RestoreTroopPlacer()
    {
        if (_orderTroopPlacer == null)
            return;

        _orderTroopPlacer.SuspendTroopPlacer = _wasTroopPlacerSuspended;
    }

    private void HandleCannonTypeCycling()
    {
        if (!Input.IsKeyPressed(CycleCannonTypeKey))
            return;

        _selectedCannonIndex = (_selectedCannonIndex + 1) % _availableCannonIds.Count;
        RemovePreviewEntity();
        ShowQuickInfo("dadg_cannon_placement_cycle",
            $"Selected cannon: {BuildCannonPrefabName(GetSelectedCannonId())}");
    }

    private void HandlePreviewRotation()
    {
        if (Input.IsKeyPressed(RotateLeftKey))
            _previewYawInRadians += RotationStepInRadians;
        if (Input.IsKeyPressed(RotateRightKey))
            _previewYawInRadians -= RotationStepInRadians;
    }

    private void HandleCancelPlacementMode()
    {
        if (Input.IsKeyPressed(CancelPlacementModeKey))
            DeactivatePlacementMode();
    }

    private void HandleRemovePlacedCannon()
    {
        if (!Input.IsKeyPressed(RemoveLastPlacedCannonKey))
            return;

        if (_placedCannons.Count == 0)
        {
            ShowQuickInfo("dadg_cannon_placement_remove_none", "No placed cannon to remove.");
            return;
        }

        int lastIndex = _placedCannons.Count - 1;
        var placed = _placedCannons[lastIndex];
        placed.RootEntity?.Remove(RemoveEntityReason);
        _placedCannons.RemoveAt(lastIndex);

        ShowQuickInfo("dadg_cannon_placement_remove_ok",
            $"Removed last placed cannon. {_placedCannons.Count}/{_settingsProvider.PlacementLimit} placed.");
    }

    private void HandleRepositionPlacedCannon(bool hasGroundHit, in WorldPosition targetWorldPosition,
        bool isInsideDeploymentBoundaries)
    {
        if (!Input.IsKeyPressed(RepositionLastPlacedCannonKey))
            return;

        if (_placedCannons.Count == 0)
        {
            ShowQuickInfo("dadg_cannon_placement_reposition_none", "No placed cannon to reposition.");
            return;
        }

        if (!hasGroundHit || !isInsideDeploymentBoundaries)
        {
            ShowQuickInfo("dadg_cannon_placement_reposition_invalid",
                "Reposition target must be inside deployment boundaries.");
            return;
        }

        int lastIndex = _placedCannons.Count - 1;
        var placed = _placedCannons[lastIndex];
        if (placed.RootEntity == null)
            return;

        MatrixFrame frame = BuildPlacementFrame(targetWorldPosition);
        placed.RootEntity.SetGlobalFrame(in frame);
        ShowQuickInfo("dadg_cannon_placement_reposition_ok", "Moved last placed cannon.");
    }

    private void HandlePlaceCannon(bool hasGroundHit, in WorldPosition targetWorldPosition, bool isInsideDeploymentBoundaries)
    {
        if (!Input.IsKeyPressed(PlaceCannonKey))
            return;

        if (!hasGroundHit)
        {
            ShowQuickInfo("dadg_cannon_placement_hit_missing", "Cannot place cannon here.");
            return;
        }

        if (!isInsideDeploymentBoundaries)
        {
            ShowQuickInfo("dadg_cannon_placement_boundary",
                "Placement must stay inside your deployment boundaries.");
            return;
        }

        if (IsPlacementLimitReached())
        {
            ShowQuickInfo("dadg_cannon_placement_limit",
                $"Placement limit reached ({_settingsProvider.PlacementLimit}). Remove or move an existing cannon.");
            return;
        }

        if (!TrySpawnSelectedCannon(targetWorldPosition))
            return;

        ShowQuickInfo("dadg_cannon_placement_ok",
            $"Placed cannon {_placedCannons.Count}/{_settingsProvider.PlacementLimit}.");
    }

    private bool TrySpawnSelectedCannon(in WorldPosition targetWorldPosition)
    {
        string cannonId = GetSelectedCannonId();
        string cannonPrefab = BuildCannonPrefabName(cannonId);

        if (!GameEntity.PrefabExists(cannonPrefab))
        {
            _logger.Warn($"Cannot place field battle cannon. Missing prefab '{cannonPrefab}'.");
            ShowQuickInfo("dadg_cannon_placement_missing_prefab",
                $"Missing prefab: {cannonPrefab}");
            return false;
        }

        MatrixFrame frame = BuildPlacementFrame(targetWorldPosition);
        MissionObject? missionObject = Mission.CreateMissionObjectFromPrefab(cannonPrefab, frame);
        if (missionObject == null)
        {
            _logger.Warn($"Mission.CreateMissionObjectFromPrefab returned null for '{cannonPrefab}'.");
            return false;
        }

        GameEntity root = missionObject.GameEntity.Root ?? missionObject.GameEntity;
        var cannon = FindScriptOfTypeRecursive<DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn.GenericCannon>(root);
        if (cannon == null)
        {
            _logger.Warn($"Spawned prefab '{cannonPrefab}' does not include a GenericCannon script.");
            root.Remove(RemoveEntityReason);
            return false;
        }

        if (Mission.PlayerTeam == null)
        {
            _logger.Warn("Mission.PlayerTeam is null while placing a field battle cannon.");
            root.Remove(RemoveEntityReason);
            return false;
        }

        cannon.SetSide(Mission.PlayerTeam.Side);
        MarkSpawnablesAsSpawned(root);

        _placedCannons.Add(new PlacedCannon(cannon, root));
        return true;
    }

    private void UpdatePreviewEntity(bool hasGroundHit, in WorldPosition targetWorldPosition, bool isInsideDeploymentBoundaries)
    {
        string selectedCannonId = GetSelectedCannonId();
        EnsurePreviewEntity(selectedCannonId);
        if (_previewEntity == null)
            return;

        if (!hasGroundHit)
        {
            _previewEntity.SetVisibilityExcludeParents(false);
            return;
        }

        MatrixFrame frame = BuildPlacementFrame(targetWorldPosition);
        _previewEntity.SetGlobalFrame(in frame);
        _previewEntity.SetVisibilityExcludeParents(true);
        _previewEntity.SetAlpha(isInsideDeploymentBoundaries ? PreviewAlphaValid : PreviewAlphaInvalid);
        _previewEntity.SetContourColor(isInsideDeploymentBoundaries ? 2852192000u : 2868838400u);
        _previewEntity.SetPreviousFrameInvalid();
    }

    private void EnsurePreviewEntity(string selectedCannonId)
    {
        string preferredPreviewPrefab = ResolvePreviewPrefab(selectedCannonId);

        if (_previewEntity != null && _previewPrefabName == preferredPreviewPrefab)
            return;

        RemovePreviewEntity();

        if (!GameEntity.PrefabExists(preferredPreviewPrefab))
        {
            _logger.Warn($"Cannot create cannon placement preview. Missing prefab '{preferredPreviewPrefab}'.");
            return;
        }

        _previewEntity = GameEntity.Instantiate(Mission.Scene, preferredPreviewPrefab, false);
        if (_previewEntity == null)
            return;

        _previewPrefabName = preferredPreviewPrefab;
        _previewEntity.SetVisibilityExcludeParents(false);
        _previewEntity.SetBodyFlagsRecursive(BodyFlags.Disabled);
    }

    private void RemovePreviewEntity()
    {
        _previewEntity?.Remove(RemoveEntityReason);
        _previewEntity = null;
        _previewPrefabName = null;
    }

    private bool TryGetMouseGroundPosition(out WorldPosition targetWorldPosition)
    {
        targetWorldPosition = WorldPosition.Invalid;

        if (MissionScreen == null)
            return false;

        MissionScreen.ScreenPointToWorldRay(Input.GetMousePositionRanged(), out Vec3 rayBegin, out Vec3 rayEnd);
        if (rayBegin == Vec3.Invalid || rayEnd == Vec3.Invalid)
            return false;

        if (!Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float collisionDistance,
                out GameEntity _, 0.3f,
                BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora))
            return false;

        Vec3 rayDirection = rayEnd - rayBegin;
        rayDirection.Normalize();
        targetWorldPosition = new WorldPosition(Mission.Scene, UIntPtr.Zero, rayBegin + rayDirection * collisionDistance,
            hasValidZ: false);
        return true;
    }

    private bool IsEligibleFieldBattleDeployment()
    {
        if (Mission.Mode != MissionMode.Deployment)
            return false;
        if (Mission.IsSiegeBattle || Mission.IsSallyOutBattle)
            return false;
        if (Mission.GetMissionBehavior<DeploymentMissionController>() == null)
            return false;
        if (Mission.GetMissionBehavior<SiegeDeploymentMissionController>() != null)
            return false;
        if (Mission.PlayerTeam == null || Mission.PlayerTeam.Side == BattleSideEnum.None)
            return false;
        return true;
    }

    private bool IsInsidePlayerDeploymentBoundaries(in WorldPosition targetWorldPosition)
    {
        TaleWorlds.MountAndBlade.Team playerTeam = Mission.PlayerTeam;
        if (playerTeam == null)
            return false;

        BattleSideEnum playerSide = playerTeam.Side;
        if (playerSide == BattleSideEnum.None)
            return false;

        IMissionDeploymentPlan deploymentPlan = Mission.DeploymentPlan;
        if (!deploymentPlan.HasDeploymentBoundaries(playerSide))
            return false;

        Vec2 targetPosition = targetWorldPosition.AsVec2;
        return deploymentPlan.IsPositionInsideDeploymentBoundaries(playerSide, in targetPosition);
    }

    private bool IsPlacementLimitReached()
    {
        return _placedCannons.Count >= _settingsProvider.PlacementLimit;
    }

    private void PruneInvalidPlacedCannons()
    {
        for (int index = _placedCannons.Count - 1; index >= 0; index--)
        {
            var placed = _placedCannons[index];
            if (placed.Cannon is null || placed.Cannon.IsDestroyed || placed.Cannon.IsDeactivated)
                _placedCannons.RemoveAt(index);
        }
    }

    private MatrixFrame BuildPlacementFrame(in WorldPosition targetWorldPosition)
    {
        MatrixFrame frame = MatrixFrame.Identity;
        frame.origin = targetWorldPosition.GetGroundVec3();
        frame.rotation.RotateAboutUp(_previewYawInRadians);
        return frame;
    }

    private string ResolvePreviewPrefab(string cannonId)
    {
        string cannonPrefab = BuildCannonPrefabName(cannonId);
        string ghostPrefab = $"{cannonPrefab}{GhostPrefabSuffix}";
        return GameEntity.PrefabExists(ghostPrefab) ? ghostPrefab : cannonPrefab;
    }

    private string BuildCannonPrefabName(string cannonId)
    {
        return $"{CannonPrefabPrefix}{cannonId}";
    }

    private string GetSelectedCannonId()
    {
        return _availableCannonIds[_selectedCannonIndex];
    }

    private static IEnumerable<GameEntity> EnumerateEntityTree(GameEntity rootEntity)
    {
        yield return rootEntity;
        var children = new List<GameEntity>();
        rootEntity.GetChildrenRecursive(ref children);
        foreach (var child in children)
            yield return child;
    }

    private static void MarkSpawnablesAsSpawned(GameEntity rootEntity)
    {
        foreach (var entity in EnumerateEntityTree(rootEntity))
        foreach (var script in entity.GetScriptComponents())
            if (script is ISpawnable spawnable)
                spawnable.SetSpawnedFromSpawner();
    }

    private static T? FindScriptOfTypeRecursive<T>(GameEntity rootEntity) where T : ScriptComponentBehavior
    {
        T? script = rootEntity.GetFirstScriptOfType<T>();
        if (script != null)
            return script;

        foreach (var child in rootEntity.GetChildren())
        {
            T? childScript = FindScriptOfTypeRecursive<T>(child);
            if (childScript != null)
                return childScript;
        }

        return null;
    }

    private static void ShowQuickInfo(string textId, string text)
    {
        MBInformationManager.AddQuickInformation(new TextObject($"{{={textId}}}{text}"));
    }

    private sealed class PlacedCannon
    {
        public PlacedCannon(DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn.GenericCannon cannon, GameEntity rootEntity)
        {
            Cannon = cannon;
            RootEntity = rootEntity;
        }

        public DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn.GenericCannon Cannon { get; }
        public GameEntity RootEntity { get; }
    }
}
