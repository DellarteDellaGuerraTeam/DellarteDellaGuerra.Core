using System.Reflection;
using DellarteDellaGuerra.Domain.Church.Hierarchy;

namespace DellarteDellaGuerra.Church.Contract.Tests;

public class BannerlordDependencyBoundaryTests
{
    [Fact]
    public void ContractTestHostAndDomain_DoNotReferenceBannerlordAssemblies()
    {
        var referencedAssemblyNames = new[]
            {
                Assembly.GetExecutingAssembly(),
                typeof(BuildChurchMapUseCase).Assembly
            }
            .SelectMany(assembly => assembly.GetReferencedAssemblies())
            .Select(reference => reference.Name ?? string.Empty)
            .Distinct()
            .ToList();

        Assert.DoesNotContain(referencedAssemblyNames, IsBannerlordAssembly);
    }

    private static bool IsBannerlordAssembly(string assemblyName) =>
        assemblyName.StartsWith("TaleWorlds.", StringComparison.Ordinal) ||
        assemblyName.Contains("Bannerlord", StringComparison.OrdinalIgnoreCase);
}
