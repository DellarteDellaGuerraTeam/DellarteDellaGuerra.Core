using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.SetSpawnEquipment.Ports
{
    public interface IMissionEventCallbackRegister
    {
        void RegisterOnAgentBuildCallback(int agentOriginUniqueSeed, Action<Agent, Banner> callback);
        void RegisterMissionEndedCallback(Action callback);
        void RegisterAfterEarlyCallback(Action callback);
    }
}