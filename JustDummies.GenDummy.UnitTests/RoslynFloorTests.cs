using System;
using System.Linq;
using System.Reflection;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     Enforces the engine's Roslyn load contract. The engine is built to be loadable by a host compiler — the CLI
///     today, an IDE code refactoring tomorrow (ADR-0065) — so the Microsoft.CodeAnalysis version it is compiled
///     against is the minimum Roslyn able to load it; a higher one makes it fail to load, and fail silently. The
///     floor is defined once in Directory.Build.props and surfaced here through assembly metadata, so the csproj
///     pin and this test can never diverge.
/// </summary>
/// <remarks>
///     Deliberately a copy of the analyzer's guard rather than a shared helper: the two assemblies are pinned for
///     the same reason but ship through different packages, and a shared harness would let one of them lose its
///     guard by a change made for the other.
/// </remarks>
public sealed class RoslynFloorTests {

    [Fact(DisplayName = "The scaffolding engine stays on the supported Roslyn floor.")]
    public void EngineStaysOnTheSupportedRoslynFloor() {
        Assembly engine = typeof(TypeNaming).Assembly;
        Version  floor  = ReadFloor(engine);

        AssemblyName[] roslynReferences = engine
           .GetReferencedAssemblies()
           .Where(reference => reference.Name is not null
                            && reference.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
           .ToArray();

        // If the family ever disappears from the metadata this test proves nothing: fail loudly rather than
        // pass vacuously.
        Check.That(roslynReferences).Not.IsEmpty();

        string[] offenders = roslynReferences
           .Where(reference => OnMajorMinorBuild(reference.Version) > floor)
           .Select(reference => $"{reference.Name} {reference.Version}")
           .ToArray();

        Check.That(offenders).IsEmpty();
    }

    private static Version ReadFloor(Assembly engine) {
        AssemblyMetadataAttribute floor = engine
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .Single(metadata => metadata.Key == "RoslynFloorVersion");

        return OnMajorMinorBuild(Version.Parse(floor.Value!));
    }

    // Roslyn assemblies carry a four-part version (x.y.z.0) while the floor is written as x.y.z; comparing on
    // major.minor.build only keeps a raw 4.8.0.0 from reading as newer than the 4.8.0 floor.
    private static Version OnMajorMinorBuild(Version? version) =>
        new(version?.Major ?? 0, version?.Minor ?? 0, version is { Build: >= 0 } ? version.Build : 0);

}
