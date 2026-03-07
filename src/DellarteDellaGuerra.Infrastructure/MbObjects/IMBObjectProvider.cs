using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure;

public interface IMBObjectProvider<T> where T : MBObjectBase
{
    T GetMbObject();
}