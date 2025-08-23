namespace DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers
{
    public class AgentMapper
    {
        public Agent Map(TaleWorlds.MountAndBlade.Agent agent)
        {
            return new Agent(agent.Index.ToString(), agent.IsHuman);
        }
    }
}