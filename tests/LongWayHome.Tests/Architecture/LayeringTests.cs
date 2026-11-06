using System.Reflection;
using LongWayHome.Application;
using LongWayHome.Domain;
using LongWayHome.Infrastructure;

namespace LongWayHome.Tests.Architecture;

/// <summary>
/// Enforces the dependency direction from the technical blueprint (§2):
/// Domain ← Application ← Infrastructure ← Game, and nothing below the Godot project references Godot.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(DomainAssembly).Assembly;
    private static readonly Assembly Application = typeof(ApplicationAssembly).Assembly;
    private static readonly Assembly Infrastructure = typeof(InfrastructureAssembly).Assembly;

    public static TheoryData<string> CoreAssemblies => new()
    {
        "LongWayHome.Domain",
        "LongWayHome.Application",
        "LongWayHome.Infrastructure",
    };

    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void Core_assembly_references_no_Godot_assembly(string assemblyName)
    {
        var assembly = Load(assemblyName);

        var godotReferences = ReferencedNames(assembly)
            .Where(name => name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(godotReferences);
    }

    [Fact]
    public void Domain_references_no_other_LongWayHome_assembly()
    {
        Assert.Empty(LongWayHomeReferences(Domain));
    }

    [Fact]
    public void Application_references_only_Domain()
    {
        // The compiler only emits a reference once a type from it is used, so "only" means "a subset of".
        Assert.Subset(new HashSet<string> { "LongWayHome.Domain" }, LongWayHomeReferences(Application).ToHashSet());
    }

    [Fact]
    public void Infrastructure_references_only_Application_and_Domain()
    {
        var references = LongWayHomeReferences(Infrastructure);

        Assert.Subset(new HashSet<string> { "LongWayHome.Application", "LongWayHome.Domain" }, references.ToHashSet());
    }

    private static Assembly Load(string assemblyName) => assemblyName switch
    {
        "LongWayHome.Domain" => Domain,
        "LongWayHome.Application" => Application,
        "LongWayHome.Infrastructure" => Infrastructure,
        _ => throw new ArgumentOutOfRangeException(nameof(assemblyName), assemblyName, null),
    };

    private static IEnumerable<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty);

    private static List<string> LongWayHomeReferences(Assembly assembly) =>
        ReferencedNames(assembly)
            .Where(name => name.StartsWith("LongWayHome.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();
}
