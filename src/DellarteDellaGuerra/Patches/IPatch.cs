using System.Reflection;
using DellarteDellaGuerra.Patches;

namespace DellarteDellaGuerra.Infrastructure.Patches
{
    public interface IPatch
    {
        MethodInfo? TargetMethod { get; }
        MethodInfo? PatchMethod { get; }

        PatchType PatchType { get; }
    }
}
