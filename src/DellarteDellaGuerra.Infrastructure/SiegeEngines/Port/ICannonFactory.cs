using System;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonFactory
{
    Type CannonScriptType { get; }
}
