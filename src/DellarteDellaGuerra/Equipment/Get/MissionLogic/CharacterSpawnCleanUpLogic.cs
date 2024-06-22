﻿using DellarteDellaGuerra.Equipment.Get.MissionLogic.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Equipment.Get.MissionLogic
{
    public class CharacterSpawnCleanUpLogic : MissionAgentSpawnLogic
    {
        public CharacterSpawnCleanUpLogic(
            IMissionTroopSupplier[] missionTroopSuppliers,
            BattleSideEnum playerSide,
            Mission.BattleSizeType battleSizeType) : base(missionTroopSuppliers, playerSide, battleSizeType)
        {
        }

        /**
         * <summary>
         *     This method is called when an agent is built.
         *     <br />
         *     Natively, the troops with the same id reference the same character object.
         *     So here, we make sure to override the agent's character with the global character object if needed.
         * </summary>
         * <param name="agent">The agent to build.</param>
         * <param name="banner">The banner of the agent.</param>
         * <remarks>
         *     This method is called when an agent is built and should not be called else where.
         * </remarks>
         */
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (agent?.Character?.StringId is null) return;

            var globalCharacterObject =
                MBObjectManager.Instance.GetObject<BasicCharacterObject>(agent.Character.StringId);
            if (globalCharacterObject is null || agent.Character == globalCharacterObject) return;

            agent.Character = globalCharacterObject;

            if (agent.Origin is null) return;

            agent.Origin = new AgentOriginTroopOverrider(agent.Origin)
            {
                Troop = globalCharacterObject
            };
        }
    }
}