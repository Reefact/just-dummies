#region Usings declarations

using System.Text.RegularExpressions;
using System.Xml.Linq;

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The xUnit-floor contract: every page telling the reader which <c>xunit.v3</c> the adapter needs tells them the
///     version <c>Directory.Packages.props</c> actually pins, and the pages that owe that statement make it.
/// </summary>
/// <remarks>
///     <para>
///         <c>JustDummies.Xunit</c> compiles against <c>xunit.v3.extensibility.core</c>, and the version Central Package
///         Management pins for it becomes the package's dependency floor. xUnit's packages only work at one and the same
///         version, so a consumer whose <c>xunit.v3</c> metapackage sits below that floor restores a mixed graph —
///         warning <c>NU1608</c>, then a crashed test process — and the user documentation never said so. A newcomer's
///         from-scratch trial met exactly that, with the NuGet warning as the only hint.
///     </para>
///     <para>
///         A version retyped into prose is a copy of a fact whose original lives in the build, and a copy drifts the
///         day Dependabot moves the pin. The number has a SOURCE, so this suite reads it off
///         <c>Directory.Packages.props</c> rather than off a twin: the French page agreeing with the English one proves
///         nothing when both were written against last year's floor.
///     </para>
/// </remarks>
public sealed partial class DocumentedXunitFloorTests {

    /// <summary>The package whose central version is the adapter's dependency floor.</summary>
    private const string FloorPackage = "xunit.v3.extensibility.core";

    /// <summary>
    ///     The pages that OWE the statement: where the package is installed, where it is first introduced, where the
    ///     packages are listed, and the README a consumer reads on nuget.org before any of those.
    /// </summary>
    private static readonly string[] PagesThatMustStateTheFloor = [
        "doc/handwritten/for-users/packages/justdummies-xunit.en.md",
        "doc/handwritten/for-users/packages/justdummies-xunit.fr.md",
        "doc/handwritten/for-users/packages/README.md",
        "doc/handwritten/for-users/packages/README.fr.md",
        "doc/handwritten/for-users/guides/getting-started.en.md",
        "doc/handwritten/for-users/guides/getting-started.fr.md",
        "JustDummies.Xunit/README.nuget.md"
    ];

    /// <summary>
    ///     Pages excluded from the drift check, each a RECORD of what was true when it was written: an ADR, a release
    ///     note and the migration log stop being records once their numbers are rewritten to match today's pin.
    /// </summary>
    private static readonly IReadOnlyCollection<string> ExcludedPages = [
        "doc/handwritten/for-maintainers/adr/",
        "doc/handwritten/for-maintainers/migration/",
        "RELEASE_NOTES-"
    ];

    [Fact(DisplayName = "Every stated xunit.v3 minimum is the version Directory.Packages.props pins for the adapter's dependency.")]
    public void EveryStatedXunitFloorIsThePinnedOne() {
        string       pinned      = PinnedFloor();
        List<string> divergences = [];

        foreach (string path in PagesInScope()) {
            string[] lines = File.ReadAllLines(path);

            for (int index = 0; index < lines.Length; index++) {
                foreach (Match match in StatedFloorExpression.Matches(lines[index])) {
                    string stated = match.Groups["version"].Value;
                    if (stated.Equals(pinned, StringComparison.Ordinal)) { continue; }

                    divergences.Add($"{Relative(path)}:{index + 1}: states {stated} where Directory.Packages.props pins {pinned} — {lines[index].Trim()}");
                }
            }
        }

        Check.WithCustomMessage(
                  $"{divergences.Count} statement(s) of an xunit.v3 floor the build contradicts:{Environment.NewLine}"
                + string.Join(Environment.NewLine, divergences))
             .That(divergences).IsEmpty();
    }

    [Fact(DisplayName = "The pages that introduce or install the adapter state the xunit.v3 floor.")]
    public void ThePagesThatOweTheFloorStateIt() {
        string       pinned  = PinnedFloor();
        List<string> missing = [];

        foreach (string relativePath in PagesThatMustStateTheFloor) {
            string text = File.ReadAllText(Path.Combine(DocumentationCorpus.RepositoryRoot, relativePath));

            bool stated = StatedFloorExpression.Matches(text)
                                               .Any(match => match.Groups["version"].Value.Equals(pinned, StringComparison.Ordinal));

            if (!stated) { missing.Add(relativePath); }
        }

        Check.WithCustomMessage(
                  $"These pages never state that the adapter needs xunit.v3 {pinned}:{Environment.NewLine}"
                + string.Join(Environment.NewLine, missing))
             .That(missing).IsEmpty();
    }

    /// <summary>The version Central Package Management pins for the adapter's one xUnit dependency.</summary>
    private static string PinnedFloor() {
        string    path  = Path.Combine(DocumentationCorpus.RepositoryRoot, "Directory.Packages.props");
        XDocument props = XDocument.Load(path);

        XElement? entry = props.Descendants("PackageVersion")
                               .SingleOrDefault(element => string.Equals((string?)element.Attribute("Include"), FloorPackage, StringComparison.OrdinalIgnoreCase));

        Check.WithCustomMessage($"Directory.Packages.props pins no single version for {FloorPackage}; the adapter's floor has lost its source.")
             .That(entry).IsNotNull();

        string? version = (string?)entry!.Attribute("Version");

        Check.WithCustomMessage($"The {FloorPackage} entry of Directory.Packages.props carries no Version attribute.")
             .That(version).IsNotNull();

        return version!;
    }

    /// <summary>The corpus minus the records, plus the packaged README the corpus does not hold.</summary>
    private static IEnumerable<string> PagesInScope() {
        IEnumerable<string> corpus = DocumentationCorpus.Pages
                                                        .Where(page => !ExcludedPages.Any(excluded => page.RelativePath.Contains(excluded, StringComparison.Ordinal)))
                                                        .Select(page => page.AbsolutePath);

        return corpus.Append(Path.Combine(DocumentationCorpus.RepositoryRoot, "JustDummies.Xunit", "README.nuget.md"));
    }

    private static string Relative(string absolutePath) {
        return Path.GetRelativePath(DocumentationCorpus.RepositoryRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
    }

    // A floor is stated as the package name followed by a version — "`xunit.v3` **4.0.2** or later",
    // "`xunit.v3` 4.0.2 ou ultérieure" — with nothing but inline-code and emphasis marks between the two. A version
    // that sits further away is somebody else's number: the release notes' own, or an xUnit release being discussed.
    [GeneratedRegex(@"xunit\.v3`?\**\s+\**(?<version>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex StatedFloorExpression { get; }

}
