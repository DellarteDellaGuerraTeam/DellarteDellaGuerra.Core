using System;
using System.Collections.Generic;
using DellarteDellaGuerra.SetSpawnEquipment.Ports;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.Missions.Events
{
    public class MissionEventExecutor : MissionLogic, IMissionEventCallbackRegister
    {
        private readonly List<(int agentOriginUniqueSeed, Action<Agent, Banner> callback)> _onAgentBuildCallbacks =
            new();

        private readonly Stack<Action> _onMissionEndedCallbacks = new();
        private readonly Stack<Action> _onAfterEarlyCallbacks = new();

        public void RegisterOnAgentBuildCallback(int agentOriginUniqueSeed, Action<Agent, Banner> callback)
        {
            if (callback is null) return;
            _onAgentBuildCallbacks.Add((agentOriginUniqueSeed, callback));
        }

        public void RegisterMissionEndedCallback(Action callback)
        {
            if (callback is null) return;
            _onMissionEndedCallbacks.Push(callback);
        }
        
        public void RegisterAfterEarlyCallback(Action callback)
        {
            if (callback is null) return;
            _onAfterEarlyCallbacks.Push(callback);
        }

        public override void OnBehaviorInitialize()
        {
            _onAgentBuildCallbacks.Clear();
            _onMissionEndedCallbacks.Clear();
            _onAfterEarlyCallbacks.Clear();
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            _onAgentBuildCallbacks.ForEach(callback =>
            {
                if (agent.Origin?.UniqueSeed == callback.agentOriginUniqueSeed)
                    callback.callback(agent, banner);
            });
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();

            while (!_onMissionEndedCallbacks.IsEmpty())
            {
                var callback = _onMissionEndedCallbacks.Pop();
                callback();
            }
        }
        
        public override void AfterStart()
        {
            base.AfterStart();

            while (!_onAfterEarlyCallbacks.IsEmpty())
            {
                var callback = _onAfterEarlyCallbacks.Pop();
                callback();
            }
        }
    }
}