using DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic.Support;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic
{
    /**
     * <summary>
     * In Bannerlord, all troops of the same type share the same character object.
     * This causes an issue if we want to work with different equipment definitions for the same troop type.
     * So, to bypass this issue, we temporarily override the agent's character with a new character object with the equipment we want.
     * Once the agent is built, the equipment from the character object has been initialised in the Agent object and is no longer needed.
     * So, we override the agent's character with the global character object so that other mission logic depending on it work as intended.
     * </summary>
     */
    public sealed class CharacterSpawnCleanUpLogic : MissionAgentSpawnLogic
    {
        public CharacterSpawnCleanUpLogic(
            IMissionTroopSupplier[] missionTroopSuppliers,
            BattleSideEnum playerSide,
            Mission.BattleSizeType battleSizeType) : base(missionTroopSuppliers, playerSide, battleSizeType)
        {
        }

        /**
         * <summary>
         * This method is called when an agent is built.
         * <br/>
         * Bannerlord uses for each troop type the same character object.
         * If troop1 and troop2 are two soldiers of the same type, they will share the same character object.
         * So here, we make sure to override the agent's character with the global character object if needed.
         * </summary>
         * <param name="agent">The agent to build.</param>
         * <param name="banner">The banner of the agent.</param>
         * <remarks>
         * This method is called when an agent is built and should not be called else where.
         * </remarks>
         */
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (agent?.Character?.StringId is null) return;

            var globalCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>(agent.Character.StringId);
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