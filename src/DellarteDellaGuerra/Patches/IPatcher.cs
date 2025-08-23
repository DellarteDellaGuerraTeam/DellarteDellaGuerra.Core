using DellarteDellaGuerra.Infrastructure.Patches;

namespace DellarteDellaGuerra.Patches
{
    public interface IPatcher
    {
        void AddPatch(IPatch patch);
    }
}