using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.MbObjects;

public interface IMBObjectProvider<T> where T : MBObjectBase
{
    T GetMbObject();
}