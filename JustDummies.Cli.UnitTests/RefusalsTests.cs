using System;
using System.IO;
using System.Linq;

using JustDummies.GenAny;

using NFluent;

using Spectre.Console;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     What the console says when there is no file (§7).
/// </summary>
/// <remarks>
///     Checked as text, because text is what it is: the exit code says a run failed and nothing more, and
///     everything that turns that into something a developer can act on is in these sentences.
/// </remarks>
public sealed class RefusalsTests : IDisposable {

    private readonly string directory = Directory.CreateTempSubdirectory("dum-refuse-").FullName;

    /// <inheritdoc />
    public void Dispose() {
        Directory.Delete(directory, recursive: true);
    }

    /// <summary>
    ///     A status the console has nothing to say about is a status nobody finished adding.
    /// </summary>
    /// <remarks>
    ///     The refusal falls back to printing the status's own name, which reads like the inside of the engine
    ///     leaking out — so this asserts it never does. It is the one test that fails on a
    ///     <see cref="ScaffoldStatus" /> added without a sentence to go with it, and it is written over the
    ///     enum rather than over a list precisely so it cannot fall behind one.
    /// </remarks>
    [Fact(DisplayName = "Every way a scaffold can fail has a sentence of its own.")]
    public void EveryFailureHasASentence() {
        foreach (ScaffoldStatus status in Enum.GetValues<ScaffoldStatus>().Where(value => value != ScaffoldStatus.Scaffolded)) {
            string said = Rendered(ScaffoldOutcome.Refused(status));

            Check.WithCustomMessage($"{status} has no sentence of its own.").That(said).Not.Contains(status.ToString());
            Check.That(said).Contains("Order");
        }
    }

    // The near-misses are the whole point of the answer: a denial costs a search, a correction costs a
    // keystroke.
    [Fact(DisplayName = "A type that matched nothing is answered with the closest names.")]
    public void ATypeThatMatchedNothingIsAnsweredWithTheClosestNames() {
        string said = Rendered(ScaffoldOutcome.Refused(ScaffoldStatus.TypeNotFound, ["OrderLine", "OrderStatus"]));

        Check.That(said).Contains("OrderLine, OrderStatus");
    }

    // Nothing close is a different situation, and points at the likelier cause: the wrong project.
    [Fact(DisplayName = "A type nothing resembles points at --project rather than at the spelling.")]
    public void ATypeNothingResemblesPointsAtTheProject() {
        Check.That(Rendered(ScaffoldOutcome.Refused(ScaffoldStatus.TypeNotFound))).Contains("--project");
    }

    [Fact(DisplayName = "An ambiguous name lists the full names and asks for one of them.")]
    public void AnAmbiguousNameListsTheFullNames() {
        string said = Rendered(ScaffoldOutcome.Refused(ScaffoldStatus.TypeAmbiguous,
                                                       ["Shop.Archive.Order", "Shop.Sales.Order"]));

        Check.That(said).Contains("  Shop.Archive.Order\n  Shop.Sales.Order\n");
        Check.That(said).Contains("in full");
    }

    // Nothing can be resolved without the package (ADR-0059), so the answer is the one command that changes
    // it.
    [Fact(DisplayName = "A project without the library is answered with the package to add.")]
    public void AProjectWithoutTheLibraryIsAnsweredWithThePackage() {
        Check.That(Rendered(ScaffoldOutcome.Refused(ScaffoldStatus.LibraryNotReferenced)))
             .Contains("dotnet add package JustDummies");
    }

    [Fact(DisplayName = "An existing file is named, with --force and what it costs.")]
    public void AnExistingFileIsNamedWithWhatForceCosts() {
        string said = Say(console => Refusals.FileExists("/tests/AnyOrder.cs", console));

        Check.That(said).Contains("/tests/AnyOrder.cs");
        Check.That(said).Contains("--force");
        Check.That(said).Contains("lost");
    }

    [Fact(DisplayName = "Several projects are all named, under the sentence that refuses them.")]
    public void SeveralProjectsAreAllNamed() {
        File.WriteAllText(Path.Combine(directory, "Shop.Tests.csproj"), string.Empty);
        File.WriteAllText(Path.Combine(directory, "Shop.Domain.Tests.csproj"), string.Empty);

        string said = Say(console => Refusals.NoProject(ProjectLocator.Locate(directory, explicitPath: null), console));

        Check.That(said).Contains("--project");
        Check.That(said).Contains("Shop.Domain.Tests.csproj");
        Check.That(said).Contains("Shop.Tests.csproj");
    }

    private static string Rendered(ScaffoldOutcome outcome) {
        return Say(console => Refusals.Render("Order", outcome, console));
    }

    private static string Say(Action<IAnsiConsole> refusal) {
        StringWriter written = new();

        refusal(ToolConsole.On(written));

        return written.ToString();
    }

}
