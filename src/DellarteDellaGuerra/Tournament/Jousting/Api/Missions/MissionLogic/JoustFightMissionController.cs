using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions;
using DellarteDellaGuerra.Tournament.Jousting.Equipment.Spi;
using SandBox;
using SandBox.Tournaments;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Missions.MissionLogic
{
    /// <summary>
    ///     See
    ///     https://github.com/TheOldRealms/TOR_Core/blob/development/CSharpSourceCode/Missions/JoustFightMissionController.cs
    ///     for the original implementation
    /// </summary>
    public class JoustFightMissionController : TaleWorlds.MountAndBlade.MissionLogic, ITournamentGameBehavior
    {
        public enum JoustFightState
        {
            MountedCombat,
            Transition,
            FootCombat
        }

        private const string JoustBarrierTag = "jousting_barrier_passable";

        private readonly CultureObject _culture;
        private readonly List<Agent> _currentTournamentAgents;
        private readonly List<Agent> _currentTournamentMountAgents;

        private readonly IGetJoustEquipmentUtil _getJoustEquipmentUtil;
        private readonly IEquipmentMapper _equipmentMapper;
        private readonly JoustingLanceUtil _joustingLanceUtil;
        private readonly IList<Action> _matchEndListeners;

        private List<TournamentParticipant> _aliveParticipants;
        private List<TournamentTeam> _aliveTeams;
        private bool _cheerStarted;
        private BasicMissionTimer _cheerTimer;
        private BasicMissionTimer _dismountNotificationTimer;
        private BasicMissionTimer _endTimer;
        private bool _forceEndMatch;
        private bool _isLastRound;
        private bool _isSimulated;
        private TournamentMatch _match;
        private GameEntity _team0FootSpawn;
        private GameEntity _team0MountedSpawn;
        private GameEntity _team1FootSpawn;
        private GameEntity _team1MountedSpawn;

        public JoustFightMissionController(
            CultureObject culture,
            IGetJoustEquipmentUtil getJoustEquipmentUtil,
            IEquipmentMapper equipmentMapper,
            ILoggerFactory loggerFactory)
        {
            _match = null;
            _culture = culture;
            _getJoustEquipmentUtil = getJoustEquipmentUtil;
            _equipmentMapper = equipmentMapper;
            _joustingLanceUtil = new JoustingLanceUtil(loggerFactory);
            _cheerStarted = false;
            _currentTournamentAgents = new List<Agent>();
            _currentTournamentMountAgents = new List<Agent>();
            _matchEndListeners = new List<Action>();
        }

        public JoustFightState CurrentState { get; private set; } = JoustFightState.MountedCombat;

        public void StartMatch(TournamentMatch match, bool isLastRound)
        {
            _cheerStarted = false;
            CurrentState = JoustFightState.MountedCombat;
            _match = match;
            _isLastRound = isLastRound;
            PrepareForMatch();
            Mission.SetMissionMode(MissionMode.Battle, true);
            Mission.Scene.SetAbilityOfFacesWithId(2, false);
            EnableJoustingBarriers();

            if (_match.Teams.Count() != 2)
                throw new ArgumentException("The number of teams in a jousting tournament match is other than 2.");

            List<Team> teams = new();
            int spawnPointId = 0;

            foreach (TournamentTeam tournamentTeam in _match.Teams)
            {
                BattleSideEnum side = tournamentTeam.IsPlayerTeam ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
                Team team = Mission.Teams.Add(side, tournamentTeam.TeamColor, uint.MaxValue, tournamentTeam.TeamBanner);
                GameEntity spawnPoint = GetSpawnPointForTeam(spawnPointId, true);
                foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
                    SpawnTournamentParticipant(spawnPoint, tournamentParticipant, team);
                spawnPointId++;
                teams.Add(team);
            }

            for (int i = 0; i < teams.Count; i++)
            for (int j = i + 1; j < teams.Count; j++)
                teams[i].SetIsEnemyOf(teams[j], true);
            _aliveParticipants = _match.Participants.ToList();
            _aliveTeams = _match.Teams.ToList();

            // If the player left clicks before couch lancing, the lance bugs out and stays straight, which makes it very easy to beat the opponent. So we temporarily disable the mouse interaction
            if (_match.IsPlayerParticipating()) DisablePlayerMouseInteraction();
        }

        public void SkipMatch(TournamentMatch match)
        {
            _match = match;
            PrepareForMatch();
            Simulate();
        }

        public bool IsMatchEnded()
        {
            if (_isSimulated || _match == null) return true;
            if ((_endTimer != null && _endTimer.ElapsedTime > 6f) || _forceEndMatch)
            {
                _forceEndMatch = false;
                _endTimer = null;
                return true;
            }

            if (_cheerTimer != null && !_cheerStarted && _cheerTimer.ElapsedTime > 1f)
            {
                OnMatchResultsReady();
                _cheerTimer = null;
                _cheerStarted = true;
                AgentVictoryLogic missionBehavior = Mission.GetMissionBehavior<AgentVictoryLogic>();
                foreach (Agent agent in _currentTournamentAgents)
                    if (agent.IsAIControlled)
                        missionBehavior.SetTimersOfVictoryReactionsOnTournamentVictoryForAgent(agent, 1f, 3f);
                return false;
            }

            if (_endTimer == null && !CheckIfIsThereAnyEnemies())
            {
                _endTimer = new BasicMissionTimer();
                if (!_cheerStarted) _cheerTimer = new BasicMissionTimer();
            }

            return false;
        }

        public void OnMatchEnded()
        {
            SandBoxHelpers.MissionHelper.FadeOutAgents(from x in _currentTournamentAgents
                where x.IsActive()
                select x, true, false);
            SandBoxHelpers.MissionHelper.FadeOutAgents(from x in _currentTournamentMountAgents
                where x.IsActive()
                select x, true, false);
            Mission.ClearCorpses(false);
            Mission.Teams.Clear();
            Mission.RemoveSpawnedItemsAndMissiles();
            _match = null;
            _endTimer = null;
            _cheerTimer = null;
            _dismountNotificationTimer = null;
            _isSimulated = false;
            _currentTournamentAgents.Clear();
            _currentTournamentMountAgents.Clear();

            EnablePlayerMouseInteraction();

            foreach (var listener in _matchEndListeners) listener.Invoke();
            _matchEndListeners.Clear();
        }

        public bool CanAgentRout(Agent agent)
        {
            return false;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.CanAgentRout_AdditionalCondition += CanAgentRout;
        }

        public override void AfterStart()
        {
            _team0MountedSpawn = Mission.Scene.FindEntityWithTag("team0_mounted_spawn");
            _team1MountedSpawn = Mission.Scene.FindEntityWithTag("team1_mounted_spawn");
            _team0FootSpawn = Mission.Scene.FindEntityWithTag("team0_foot_spawn");
            _team1FootSpawn = Mission.Scene.FindEntityWithTag("team1_foot_spawn");
        }

        public override void OnMissionTick(float dt)
        {
            if (CurrentState == JoustFightState.Transition)
            {
                if (IsThereAnyPlayerAgent() && GetPlayerAgent().HasMount)
                {
                    if (_dismountNotificationTimer == null)
                    {
                        _dismountNotificationTimer = new BasicMissionTimer();
                    }
                    else if (_dismountNotificationTimer.ElapsedTime > 5)
                    {
                        MBInformationManager.AddQuickInformation(
                            new TextObject("You must dismount to continue with combat on foot."));
                        _dismountNotificationTimer.Reset();
                    }
                }

                int num = 0;
                foreach (var agent in _currentTournamentAgents)
                {
                    var action = agent.GetCurrentAction(0);
                    if (action.GetName().Contains("act_dismount_"))
                    {
                        var progress = agent.GetCurrentActionProgress(0);
                        if (progress >= 0.95f)
                            SandBoxHelpers.MissionHelper.FadeOutAgents(from x in _currentTournamentMountAgents
                                where x.IsActive()
                                select x, true, false);
                    }

                    var target = GetSpawnPointForTeam(agent.Team.TeamIndex, false);
                    if (agent.Position.DistanceSquared(target.GlobalPosition) < 5 && !agent.HasMount) num++;
                }

                if (num == 2)
                {
                    foreach (var agent in _currentTournamentAgents)
                        if (!agent.IsPlayerControlled)
                        {
                            agent.DisableScriptedMovement();
                            agent.ToggleInvulnerable();
                        }

                    CurrentState = JoustFightState.FootCombat;

                    foreach (var item in Mission.MountsWithoutRiders.ToList()) item.Key.FadeOut(false, false);
                }
            }

            if (CurrentState == JoustFightState.MountedCombat)
                foreach (var agent in _currentTournamentAgents)
                    if (agent != Agent.Main)
                    {
                        agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.MoveForwardOnly);
                        agent.SetScriptedFlags(Agent.AIScriptedFrameFlags.NeverSlowDown);

                        // The couch-only lance has no active attack usage, so whenever the AI is
                        // allowed to change weapons it sheathes the lance to bare fists. Below couch
                        // speed we strip CanWieldWeapon so it cannot sheathe: the lance stays in hand
                        // and there is nothing to re-wield, which removes the low-speed flicker.
                        // Once fast enough to couch we restore wielding and re-wield the lance if it
                        // was sheathed; the couch then engages and holds (it will not sheathe mid
                        // passive attack).
                        if (!agent.IsPassiveUsageConditionsAreMet)
                        {
                            agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanWieldWeapon);
                        }
                        else
                        {
                            agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanWieldWeapon);

                            if (agent.WieldedWeapon.IsEmpty)
                            {
                                for (var equipmentIndex = EquipmentIndex.Weapon0;
                                     equipmentIndex < EquipmentIndex.NumPrimaryWeaponSlots;
                                     equipmentIndex++)
                                {
                                    if (agent.Equipment[equipmentIndex].CurrentUsageItem?.WeaponClass ==
                                        WeaponClass.TwoHandedPolearm)
                                    {
                                        agent.TryToWieldWeaponInSlot(equipmentIndex, Agent.WeaponWieldActionType.Instant, true);
                                        break;
                                    }
                                }
                            }
                        }
                    }
        }

        public void PrepareForMatch()
        {
            List<TaleWorlds.Core.Equipment> participantWeaponEquipmentList = GetParticipantWeaponEquipmentList();
            foreach (TournamentTeam tournamentTeam in _match.Teams)
            {
                int num = 0;
                foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
                {
                    tournamentParticipant.MatchEquipment = participantWeaponEquipmentList[num].Clone();
                    AddRandomClothes(_culture, tournamentParticipant);
                    num++;
                }
            }
        }

        public override void OnAgentDismount(Agent agent)
        {
            base.OnAgentDismount(agent);
            if (Agent.Main is null || agent != Agent.Main) return;

            EnablePlayerMouseInteraction();
        }

        private GameEntity GetSpawnPointForTeam(int teamIndex, bool isMounted)
        {
            if (teamIndex == 0)
            {
                if (isMounted) return _team0MountedSpawn;
                return _team0FootSpawn;
            }

            if (teamIndex == 1)
            {
                if (isMounted) return _team1MountedSpawn;
                return _team1FootSpawn;
            }

            return null;
        }

        public void RestartMatch()
        {
            if (!IsMatchEnded() && _endTimer == null)
                foreach (var agent in _currentTournamentAgents)
                    if (agent.Team.TeamIndex == 0)
                    {
                        agent.TeleportToPosition(_team0MountedSpawn.GlobalPosition);
                        agent.LookDirection = _team0MountedSpawn.GetFrame().rotation.f;
                    }
                    else if (agent.Team.TeamIndex == 1)
                    {
                        agent.TeleportToPosition(_team1MountedSpawn.GlobalPosition);
                        agent.LookDirection = _team1MountedSpawn.GetFrame().rotation.f;
                    }
        }

        protected override void OnEndMission()
        {
            Mission.CanAgentRout_AdditionalCondition -= CanAgentRout;
        }

        private void SpawnTournamentParticipant(GameEntity spawnPoint, TournamentParticipant participant, Team team)
        {
            MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            SpawnParticipantAgent(participant, team, globalFrame);
        }

        private List<TaleWorlds.Core.Equipment> GetParticipantWeaponEquipmentList()
        {
            List<TaleWorlds.Core.Equipment> list = new();
            CultureObject culture = PlayerEncounter.EncounterSettlement.Culture;
            CharacterObject characterObject = culture.TournamentTeamTemplatesForOneParticipant.FirstOrDefault();
            foreach (TaleWorlds.Core.Equipment sourceEquipment in characterObject.BattleEquipments)
            {
                TaleWorlds.Core.Equipment equipment = new TaleWorlds.Core.Equipment();
                equipment.FillFrom(sourceEquipment);
                list.Add(equipment);
            }

            return list;
        }

        public void OnMatchResultsReady()
        {
            if (!_match.IsPlayerParticipating())
            {
                MBInformationManager.AddQuickInformation(new TextObject("{=UBd0dEPp}Match is over"));
                return;
            }

            if (_match.IsPlayerWinner())
            {
                if (_isLastRound)
                {
                    MBInformationManager.AddQuickInformation(
                        new TextObject("{=Jn0k20c3}Round is over, you survived the final round of the tournament."));
                    return;
                }

                MBInformationManager.AddQuickInformation(
                    new TextObject(
                        "{=uytwdSVH}Round is over, you are qualified for the next stage of the tournament."));
                return;
            }

            MBInformationManager.AddQuickInformation(
                new TextObject("{=lcVauEKV}Round is over, you are disqualified from the tournament."));
        }

        private void SpawnParticipantAgent(TournamentParticipant participant, Team team, MatrixFrame frame)
        {
            CharacterObject character = participant.Character;
            AgentBuildData agentBuildData =
                new AgentBuildData(new SimpleAgentOrigin(character, -1, character.HeroObject?.ClanBanner ?? null,
                        participant.Descriptor)).Team(team)
                    .InitialPosition(frame.origin);
            Vec2 vec = frame.rotation.f.AsVec2;
            vec = vec.Normalized();

            var agentBuildData2 = agentBuildData.InitialDirection(vec)
                .Equipment(_equipmentMapper.ToNative(_getJoustEquipmentUtil.GetJoustEquipment(_equipmentMapper.ToDomain(participant.MatchEquipment))))
                .ClothingColor1(team.Color)
                .Banner(character.HeroObject?.Clan?.Banner ?? team.Banner)
                .Controller(character.IsPlayerCharacter
                    ? AgentControllerType.Player
                    : AgentControllerType.AI);
            Agent agent = Mission.SpawnAgent(agentBuildData2);

            _joustingLanceUtil.RestrictToCouchUsage(agent);
            DisableNonTwoHandedPolearmsOnHorseback(agent);

            if (character.IsPlayerCharacter)
            {
                agent.Health = character.HeroObject.HitPoints;
                Mission.PlayerTeam = team;
            }
            else
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
                agent.Team.MasterOrderController?.SetOrder(OrderType.Charge);
            }

            agent.WieldInitialWeapons();
            _currentTournamentAgents.Add(agent);
            if (agent.HasMount) _currentTournamentMountAgents.Add(agent.MountAgent);
        }

        private void DisableNonTwoHandedPolearmsOnHorseback(Agent agent)
        {
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                 equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                 equipmentIndex++)
            {
                var equipmentUsageItem = agent.Equipment[equipmentIndex].CurrentUsageItem;
                if (equipmentUsageItem is not null && equipmentUsageItem.WeaponClass != WeaponClass.TwoHandedPolearm &&
                    equipmentUsageItem.ItemUsage is not null)
                {
                    MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
                    DisableWeaponOnHorse(missionWeapon);
                    agent.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon);
                }
            }
        }

        private void DisableWeaponOnHorse(MissionWeapon weapon)
        {
            if (weapon.CurrentUsageItem?.ItemUsage is null) return;

            var noMountItemUsageVariant =
                weapon.CurrentUsageItem.ItemUsage + "_nomount";

            if (MBItem.GetItemUsageIndex(noMountItemUsageVariant) < 0)
                // TODO: Log both error use cases
                return;

            var usageItem = weapon.CurrentUsageItem;
            var originalItemUsage = usageItem.ItemUsage;
            if (!_joustingLanceUtil.TrySetItemUsage(usageItem, noMountItemUsageVariant)) return;

            _matchEndListeners.Add(() =>
            {
                _joustingLanceUtil.TrySetItemUsage(usageItem, originalItemUsage);
            });
        }

        private void AddRandomClothes(CultureObject culture, TournamentParticipant participant)
        {
            TaleWorlds.Core.Equipment participantArmor =
                TaleWorlds.CampaignSystem.Campaign.Current.Models.TournamentModel.GetParticipantArmor(participant
                    .Character);
            for (int i = 5; i < 10; i++)
            {
                EquipmentElement equipmentFromSlot = participantArmor.GetEquipmentFromSlot((EquipmentIndex)i);
                if (equipmentFromSlot.Item != null)
                    participant.MatchEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
            }
        }

        private bool CheckIfTeamIsDead(TournamentTeam affectedParticipantTeam)
        {
            bool result = true;
            using (List<TournamentParticipant>.Enumerator enumerator = _aliveParticipants.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    if (enumerator.Current.Team == affectedParticipantTeam)
                    {
                        result = false;
                        break;
                    }
            }

            return result;
        }

        private void AddScoreToRemainingTeams()
        {
            foreach (TournamentTeam tournamentTeam in _aliveTeams)
            foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
                tournamentParticipant.AddScore(1);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState,
            KillingBlow killingBlow)
        {
            if (!IsMatchEnded() && affectorAgent != null && affectedAgent != affectorAgent && affectedAgent.IsHuman &&
                affectorAgent.IsHuman)
            {
                TournamentParticipant participant = _match.GetParticipant(affectedAgent.Origin.UniqueSeed);
                _aliveParticipants.Remove(participant);
                _currentTournamentAgents.Remove(affectedAgent);
                if (CheckIfTeamIsDead(participant.Team))
                {
                    _aliveTeams.Remove(participant.Team);
                    AddScoreToRemainingTeams();
                }
            }
            else if (!IsMatchEnded() && affectorAgent != null && affectedAgent != affectorAgent &&
                     affectedAgent.IsMount && affectorAgent.IsHuman && CurrentState == JoustFightState.MountedCombat)
            {
                CurrentState = JoustFightState.Transition;
                foreach (var agent in _currentTournamentAgents)
                {
                    if (agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.Weapon0 &&
                        !agent.WieldedWeapon.IsEmpty &&
                        agent.WieldedWeapon.Item != null &&
                        agent.WieldedWeapon.Item.StringId.Contains("lance"))
                        agent.DropItem(EquipmentIndex.Weapon0);

                    if (!agent.IsPlayerControlled)
                    {
                        // Restore weapon wielding stripped in SpawnParticipantAgent so the agent can
                        // wield a melee weapon once combat continues on foot.
                        agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanWieldWeapon);

                        var spawnPoint = GetSpawnPointForTeam(agent.Team.TeamIndex, false);
                        WorldPosition pos = new WorldPosition(Mission.Scene, spawnPoint.GlobalPosition);
                        agent.SetScriptedPosition(ref pos, true,
                            Agent.AIScriptedFrameFlags.GoWithoutMount | Agent.AIScriptedFrameFlags.NoAttack);
                        agent.ToggleInvulnerable();
                    }

                    DisableJoustingBarriers();
                    Mission.Scene.SetAbilityOfFacesWithId(2, true);
                }
            }
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon,
            bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp,
            float hitDistance, float shotDifficulty)
        {
            if (affectorAgent == null) return;
            if (affectorAgent.IsMount && affectorAgent.RiderAgent != null) affectorAgent = affectorAgent.RiderAgent;
            if (affectorAgent.Character == null || affectedAgent.Character == null) return;
            float num = blow.InflictedDamage;
            if (num > affectedAgent.HealthLimit) num = affectedAgent.HealthLimit;
            float num2 = num / affectedAgent.HealthLimit;
            EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty,
                attackerWeapon, blow.AttackType, 0.5f * num2, num);
        }

        private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus,
            float lastShotDifficulty, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType,
            float hitpointRatio, float damageAmount)
        {
            CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
            CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;
            if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
            {
                bool isHorseCharge = affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision;
                SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus,
                    lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Tournament,
                    affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, false, damageAmount,
                    affectedAgent.Health < 1f, false, isHorseCharge, false);
            }
        }

        public bool CheckIfIsThereAnyEnemies()
        {
            Team team = null;
            foreach (Agent agent in _currentTournamentAgents)
                if (agent.IsHuman && agent.IsActive() && agent.Team != null)
                {
                    if (team == null)
                        team = agent.Team;
                    else if (team != agent.Team) return true;
                }

            return false;
        }

        private void Simulate()
        {
            _isSimulated = false;
            if (_currentTournamentAgents.Count == 0)
            {
                _aliveParticipants = _match.Participants.ToList();
                _aliveTeams = _match.Teams.ToList();
            }

            TournamentParticipant tournamentParticipant =
                _aliveParticipants.FirstOrDefault(x => x.Character == CharacterObject.PlayerCharacter);
            if (tournamentParticipant != null)
            {
                TournamentTeam team = tournamentParticipant.Team;
                foreach (TournamentParticipant tournamentParticipant2 in team.Participants)
                {
                    tournamentParticipant2.ResetScore();
                    _aliveParticipants.Remove(tournamentParticipant2);
                }

                _aliveTeams.Remove(team);
                AddScoreToRemainingTeams();
            }

            Dictionary<TournamentParticipant, Tuple<float, float>> dictionary = new();
            foreach (TournamentParticipant tournamentParticipant3 in _aliveParticipants)
            {
                float item;
                float item2;
                tournamentParticipant3.Character.GetSimulationAttackPower(out item, out item2,
                    tournamentParticipant3.MatchEquipment);
                dictionary.Add(tournamentParticipant3, new Tuple<float, float>(item, item2));
            }

            int num = 0;
            while (_aliveParticipants.Count > 1 && _aliveTeams.Count > 1)
            {
                num++;
                num %= _aliveParticipants.Count;
                TournamentParticipant tournamentParticipant4 = _aliveParticipants[num];
                int num2;
                TournamentParticipant tournamentParticipant5;
                do
                {
                    num2 = MBRandom.RandomInt(_aliveParticipants.Count);
                    tournamentParticipant5 = _aliveParticipants[num2];
                } while (tournamentParticipant4 == tournamentParticipant5 ||
                         tournamentParticipant4.Team == tournamentParticipant5.Team);

                if (dictionary[tournamentParticipant5].Item2 - dictionary[tournamentParticipant4].Item1 > 0f)
                {
                    dictionary[tournamentParticipant5] = new Tuple<float, float>(
                        dictionary[tournamentParticipant5].Item1,
                        dictionary[tournamentParticipant5].Item2 - dictionary[tournamentParticipant4].Item1);
                }
                else
                {
                    dictionary.Remove(tournamentParticipant5);
                    _aliveParticipants.Remove(tournamentParticipant5);
                    if (CheckIfTeamIsDead(tournamentParticipant5.Team))
                    {
                        _aliveTeams.Remove(tournamentParticipant5.Team);
                        AddScoreToRemainingTeams();
                    }

                    if (num2 < num) num--;
                }
            }

            _isSimulated = true;
        }

        private bool IsThereAnyPlayerAgent()
        {
            if (Mission.MainAgent != null && Mission.MainAgent.IsActive()) return true;
            return _currentTournamentAgents.Any(agent => agent.IsPlayerControlled);
        }

        private Agent GetPlayerAgent()
        {
            if (Mission.MainAgent != null && Mission.MainAgent.IsActive()) return Mission.MainAgent;
            return _currentTournamentAgents.FirstOrDefault(agent => agent.IsPlayerControlled);
        }

        private void SkipMatch()
        {
            Mission.Current.GetMissionBehavior<JoustTournamentBehaviour>().SkipMatch();
        }

        public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
        {
            InquiryData result = null;
            canPlayerLeave = true;
            if (_match != null)
            {
                if (_match.IsPlayerParticipating())
                {
                    MBTextManager.SetTextVariable("SETTLEMENT_NAME",
                        Hero.MainHero.CurrentSettlement.EncyclopediaLinkWithName);
                    if (IsThereAnyPlayerAgent())
                    {
                        if (Mission.IsPlayerCloseToAnEnemy())
                        {
                            canPlayerLeave = false;
                            MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat"));
                        }
                        else if (CheckIfIsThereAnyEnemies())
                        {
                            result = new InquiryData(GameTexts.FindText("str_tournament").ToString(),
                                GameTexts.FindText("str_tournament_forfeit_game").ToString(), true, true,
                                GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(),
                                SkipMatch, null);
                        }
                        else
                        {
                            _forceEndMatch = true;
                            canPlayerLeave = false;
                        }
                    }
                    else if (CheckIfIsThereAnyEnemies())
                    {
                        result = new InquiryData(GameTexts.FindText("str_tournament").ToString(),
                            GameTexts.FindText("str_tournament_skip").ToString(), true, true,
                            GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(),
                            SkipMatch, null);
                    }
                    else
                    {
                        _forceEndMatch = true;
                        canPlayerLeave = false;
                    }
                }
                else if (CheckIfIsThereAnyEnemies())
                {
                    result = new InquiryData(GameTexts.FindText("str_tournament").ToString(),
                        GameTexts.FindText("str_tournament_skip").ToString(), true, true,
                        GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), SkipMatch,
                        null);
                }
                else
                {
                    _forceEndMatch = true;
                    canPlayerLeave = false;
                }
            }

            return result;
        }

        private void DisableJoustingBarriers()
        {
            OverrideBarriersBodyFlag(BodyFlags.Disabled);
        }

        private void EnableJoustingBarriers()
        {
            OverrideBarriersBodyFlag(BodyFlags.Barrier);
        }

        private void OverrideBarriersBodyFlag(BodyFlags bodyFlag)
        {
            foreach (GameEntity item in Mission.Scene.FindEntitiesWithTag(JoustBarrierTag).ToList())
                item.BodyFlag = bodyFlag;
        }

        private void DisablePlayerMouseInteraction()
        {
            Mission.GetMissionBehavior<MissionMainAgentController>().MissionScreen.SceneLayer.InputRestrictions
                .SetInputRestrictions(false, InputUsageMask.Invalid);
        }

        private void EnablePlayerMouseInteraction()
        {
            Mission.GetMissionBehavior<MissionMainAgentController>().MissionScreen.SceneLayer.InputRestrictions
                .SetInputRestrictions(false);
        }
    }
}
