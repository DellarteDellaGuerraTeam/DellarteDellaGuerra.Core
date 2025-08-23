using TaleWorlds.Core;

namespace DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers
{
    public class DamageTypeMapper
    {
        public DamageType Map(DamageTypes native)
        {
            return native switch
            {
                DamageTypes.Cut => DamageType.Cut,
                DamageTypes.Pierce => DamageType.Pierce,
                DamageTypes.Blunt => DamageType.Blunt,
                _ => DamageType.Cut
            };
        }
    }
}