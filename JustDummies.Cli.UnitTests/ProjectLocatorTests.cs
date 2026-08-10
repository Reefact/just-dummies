using System;
using System.IO;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     Which project's compilation is analyzed (§3.1).
/// </summary>
/// <remarks>
///     The one place the tool is allowed to guess, and it guesses only when there is nothing to guess between.
///     None or several is a refusal: picking the alphabetically first would scaffold against a compilation the
///     developer did not choose, and the file would look right while resolving another project's types.
/// </remarks>
public sealed class ProjectLocatorTests : IDisposable {

    private readonly string directory = Directory.CreateTempSubdirectory("dum-locate-").FullName;

    /// <inheritdoc />
    public void Dispose() {
        Directory.Delete(directory, recursive: true);
    }

    [Fact(DisplayName = "The single project in the directory is the one analyzed.")]
    public void TheSingleProjectHereIsTheOneAnalyzed() {
        string project = Project("Shop.Tests.csproj");

        ProjectChoice choice = ProjectLocator.Locate(directory, explicitPath: null);

        Check.That(choice.Found).IsTrue();
        Check.That(choice.Path).IsEqualTo(project);
    }

    [Fact(DisplayName = "No project here is refused, pointing at --project.")]
    public void NoProjectHereIsRefused() {
        ProjectChoice choice = ProjectLocator.Locate(directory, explicitPath: null);

        Check.That(choice.Found).IsFalse();
        Check.That(choice.Refusal).Contains("--project");
        Check.That(choice.Candidates).IsEmpty();
    }

    // Both are named, because the developer choosing between them needs to read them.
    [Fact(DisplayName = "Several projects here are refused, and all of them named.")]
    public void SeveralProjectsHereAreRefused() {
        string first  = Project("Shop.Tests.csproj");
        string second = Project("Shop.Domain.Tests.csproj");

        ProjectChoice choice = ProjectLocator.Locate(directory, explicitPath: null);

        Check.That(choice.Found).IsFalse();
        Check.That(choice.Refusal).Contains("--project");
        Check.That(choice.Candidates).Contains(first, second);
    }

    // A directory holding files that are not projects holds no project.
    [Fact(DisplayName = "Only .csproj files count as projects.")]
    public void OnlyProjectFilesCount() {
        File.WriteAllText(Path.Combine(directory, "Shop.sln"), string.Empty);
        File.WriteAllText(Path.Combine(directory, "Shop.fsproj"), string.Empty);

        Check.That(ProjectLocator.Locate(directory, explicitPath: null).Found).IsFalse();
    }

    [Fact(DisplayName = "--project names the project, whatever else is here.")]
    public void AnExplicitProjectSettlesIt() {
        Project("Shop.Tests.csproj");

        string named = Project("Chosen.csproj");

        Check.That(ProjectLocator.Locate(directory, named).Path).IsEqualTo(named);
    }

    // Named and absent is a refusal that says so, rather than a silent fall back to whatever is here: the
    // developer asked for one project, and answering with another would be the worst kind of helpful.
    [Fact(DisplayName = "--project naming nothing is refused, naming the path it looked at.")]
    public void AnExplicitProjectThatIsNotThereIsRefused() {
        Project("Shop.Tests.csproj");

        string missing = Path.Combine(directory, "Absent.csproj");

        ProjectChoice choice = ProjectLocator.Locate(directory, missing);

        Check.That(choice.Found).IsFalse();
        Check.That(choice.Refusal).Contains(missing);
    }

    // A directory that does not exist is the same answer as one holding nothing, and not an exception: the
    // caller has an --output and a --project to be wrong about, and both come back as §7's exit 1.
    [Fact(DisplayName = "A directory that is not there is refused like an empty one.")]
    public void ADirectoryThatIsNotThereIsRefused() {
        ProjectChoice choice = ProjectLocator.Locate(Path.Combine(directory, "absent"), explicitPath: null);

        Check.That(choice.Found).IsFalse();
        Check.That(choice.Refusal).IsNotEmpty();
    }

    private string Project(string fileName) {
        string path = Path.Combine(directory, fileName);

        File.WriteAllText(path, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        return path;
    }

}
