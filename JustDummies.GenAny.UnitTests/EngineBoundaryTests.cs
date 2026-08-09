using System;
using System.Linq;
using System.Reflection;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Guards the two boundaries the engine's whole shape rests on.
/// </summary>
/// <remarks>
///     It references no JustDummies assembly, because every library symbol it reasons about is resolved by
///     metadata name against the developer's compilation. That is what makes version skew between the tool and the
///     library structurally impossible (ADR-0063), and it is enforced here in the assembly as well as on the packed
///     tool, where the release train asserts the produced package declares no such dependency (§13.6).
///     <para>
///         It also references no workspace, MSBuild or console assembly: those belong to the shell. An engine that
///         acquired one would stop being loadable inside a Roslyn host, which is the constraint ADR-0065 exists to
///         keep (§10.2).
///     </para>
/// </remarks>
public sealed class EngineBoundaryTests {

    [Fact(DisplayName = "The scaffolding engine references no JustDummies assembly.")]
    public void EngineReferencesNoJustDummiesAssembly() {
        foreach (AssemblyName reference in EngineReferences()) {
            Check.WithCustomMessage($"Unexpected assembly reference: {reference.Name}")
                 .That(reference.Name!.StartsWith("JustDummies", StringComparison.Ordinal)).IsFalse();
        }
    }

    [Fact(DisplayName = "The scaffolding engine references no workspace, MSBuild or console assembly.")]
    public void EngineReferencesNothingTheShellOwns() {
        string[] shellOnly = ["Microsoft.CodeAnalysis.Workspaces", "Microsoft.Build", "Spectre.Console"];

        foreach (AssemblyName reference in EngineReferences()) {
            bool belongsToTheShell = shellOnly.Any(name => reference.Name!.StartsWith(name, StringComparison.Ordinal));

            Check.WithCustomMessage($"Unexpected assembly reference: {reference.Name}")
                 .That(belongsToTheShell).IsFalse();
        }
    }

    private static AssemblyName[] EngineReferences() {
        AssemblyName[] references = typeof(TypeNaming).Assembly.GetReferencedAssemblies();

        // Only USED references are recorded in metadata, so an engine that referenced nothing would satisfy
        // both checks above without meaning anything. Fail loudly instead.
        Check.That(references).Not.IsEmpty();

        return references;
    }

}
