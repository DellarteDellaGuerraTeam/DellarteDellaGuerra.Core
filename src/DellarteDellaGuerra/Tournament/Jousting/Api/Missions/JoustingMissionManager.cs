using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment;
using DellarteDellaGuerra.Tournament.Jousting.Api.Campaign;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions.MissionLogic;
using DellarteDellaGuerra.Tournament.Jousting.Equipment.Spi;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.View.Missions.Sound.Components;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Missions
{
    [MissionManager]
    public class JoustingMissionManager
    {
        [MissionMethod]
        public static Mission OpenJoustingFightMission(string scene, JoustTournament tournamentGame,
            Settlement settlement, CultureObject culture, bool isPlayerParticipating)
        {
            return MissionState.OpenNew("JoustFight",
                SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town),
                delegate
                 {
                     var slotMapper = new EquipmentSlotMapper(MBObjectManager.Instance);
                     var equipmentMapper = new EquipmentMapper(slotMapper);
                     var templatesRepository = new JoustingEquipmentTemplatesRepository(MBObjectManager.Instance);
                     var getJoustEquipmentUtil = new GetJoustEquipmentUtil(templatesRepository);

                     var joustFightMissionController = new JoustFightMissionController(
                         culture,
                         getJoustEquipmentUtil,
                         equipmentMapper);

                     return new MissionBehavior[]
                     {
                         new CampaignMissionComponent(),
                         new EquipmentControllerLeaveLogic(),
                        joustFightMissionController,
                        new JoustTournamentBehaviour(tournamentGame, settlement, joustFightMissionController,
                            isPlayerParticipating),
                        new AgentVictoryLogic(),
                        new MissionAgentPanicHandler(),
                        new AgentHumanAILogic(),
                        new ArenaAgentStateDeciderLogic(),
                        new MissionHardBorderPlacer(),
                        new MissionBoundaryPlacer(),
                        new MissionOptionsComponent(),
                        new HighlightsController(),
                        new SandboxHighlightsController()
                    };
                });
        }


        [ViewCreatorModule]
        public class JoustFightViewCreatorModule
        {
            [ViewMethod("JoustFight")]
            public static MissionView[] OpenJoustFightMission(Mission mission)
            {
                return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    SandBoxViewCreator.CreateMissionTournamentView(),
                    new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionSpectatorControlView(mission),
                    new MusicTournamentMissionView(),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView(),
                    new MissionCameraFadeView()
                }.ToArray();
            }
        }
    }
}
