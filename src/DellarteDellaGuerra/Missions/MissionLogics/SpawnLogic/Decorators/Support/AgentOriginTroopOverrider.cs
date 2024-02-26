using TaleWorlds.Core;

namespace DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic.Decorators.Support;

/**
 * <summary>
 * This IAgentOriginBase decorator provides a way to override the troop origin.
 * </summary>
 */
internal class AgentOriginTroopOverrider : IAgentOriginBase
{
    private readonly IAgentOriginBase _agentOriginBase;

    public AgentOriginTroopOverrider(IAgentOriginBase agentOriginBase)
    {
        _agentOriginBase = agentOriginBase;
        Troop = agentOriginBase.Troop;
    }

    public void SetWounded() => _agentOriginBase.SetWounded();

    public void SetKilled() => _agentOriginBase.SetKilled();

    public void SetRouted() => _agentOriginBase.SetRouted();

    public void OnAgentRemoved(float agentHealth) => _agentOriginBase.OnAgentRemoved(agentHealth);

    public void OnScoreHit(
        BasicCharacterObject victim,
        BasicCharacterObject formationCaptain,
        int damage,
        bool isFatal,
        bool isTeamKill,
        WeaponComponentData attackerWeapon
    )
    {
        _agentOriginBase.OnScoreHit(victim, formationCaptain, damage, isFatal, isTeamKill, attackerWeapon);
    }

    public void SetBanner(Banner banner) => _agentOriginBase.SetBanner(banner);

    public bool IsUnderPlayersCommand => _agentOriginBase.IsUnderPlayersCommand;

    public uint FactionColor => _agentOriginBase.FactionColor;

    public uint FactionColor2 => _agentOriginBase.FactionColor2;

    public IBattleCombatant BattleCombatant => _agentOriginBase.BattleCombatant;

    public int UniqueSeed => _agentOriginBase.UniqueSeed;

    public int Seed => _agentOriginBase.Seed;

    public Banner Banner => _agentOriginBase.Banner;

    public BasicCharacterObject Troop { get; init; }
}
