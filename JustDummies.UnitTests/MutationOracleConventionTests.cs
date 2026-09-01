#region Usings declarations

using System.Reflection;
using System.Text.Json;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Holds every Stryker configuration under <c>build/stryker/</c> to declaring the oracle that actually judges
///     its mutants — the test projects whose failure kills one.
/// </summary>
/// <remarks>
///     <para>
///         The trap this closes is silent. When a configuration sets <c>solution</c>, Stryker 4.16 discovers the
///         test projects itself — every project in the solution that references the mutated assembly — and its
///         <c>test-projects</c> list is never read. Nothing warns; the file goes on stating a narrow oracle while
///         a wide one runs.
///     </para>
///     <para>
///         Measured on 2026-08-31 against the pinned engine: <c>justdummies.json</c> named
///         <c>JustDummies.UnitTests</c> alone, and the run reported 2119 tests found — the whole repository, the
///         FsCheck property suite included. The same configuration without <c>solution</c> reported 790, which is
///         that one suite. The command-line <c>--test-project</c> and the <c>test-case-filter</c> option are no
///         remedy: the first leaves the count unchanged, and the second is accepted and ignored under the MTP
///         runner — a filter matching no test at all still produced the same score. A solution filter
///         (<c>.slnf</c>) is not one either: MSBuild builds it, Stryker aborts on it.
///     </para>
///     <para>
///         That is how ADR-0026 came to be recorded and not applied: the commit that removed the property suite
///         from <c>test-projects</c> landed the same day the configuration was created, on a file that already
///         carried <c>solution</c>, so it removed nothing and the decision was never in force for a single run.
///         Recorded, believed, and held by no test. This is that test.
///     </para>
/// </remarks>
public sealed class MutationOracleConventionTests {

    /// <summary>How many configurations must be found before the scan is trusted to have found its target.</summary>
    /// <remarks>
    ///     A floor, not a count: it guards against a moved directory turning every assertion below into a vacuous
    ///     pass, and leaves adding a fifth component free.
    /// </remarks>
    private const int ConfigurationFloor = 3;

    [Fact(DisplayName = "No Stryker configuration declares an oracle that a solution context would ignore.")]
    public void NoConfigurationNamesTestProjectsBesideASolution() {
        List<string> configurations = Configurations();

        Check.WithCustomMessage($"Only {configurations.Count} Stryker configuration(s) found under build/stryker; the scan lost its target.")
             .That(configurations.Count).IsGreaterOrEqualThan(ConfigurationFloor);

        List<string> offenders = [];

        foreach (string configuration in configurations) {
            JsonElement settings = Settings(configuration);
            if (!settings.TryGetProperty("solution", out JsonElement _)) { continue; }
            if (!settings.TryGetProperty("test-projects", out JsonElement declared)) { continue; }

            offenders.Add($"{Path.GetFileName(configuration)} sets \"solution\" and also declares {declared.GetArrayLength()} test project(s); "
                        + "Stryker discovers the oracle from the solution and never reads that list, so the file states an oracle that does not run.");
        }

        Check.WithCustomMessage($"{offenders.Count} configuration(s) declare an oracle that never runs:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}")
             .That(offenders).IsEmpty();
    }

    [Fact(DisplayName = "Every Stryker configuration names the test projects that judge its mutants.")]
    public void EveryConfigurationNamesItsOracle() {
        List<string> configurations = Configurations();

        Check.WithCustomMessage($"Only {configurations.Count} Stryker configuration(s) found under build/stryker; the scan lost its target.")
             .That(configurations.Count).IsGreaterOrEqualThan(ConfigurationFloor);

        List<string> offenders = [];

        foreach (string configuration in configurations) {
            // Chosen, never discovered: a configuration that names no test project leaves the oracle to whatever
            // the engine happens to find, which is the state the first theory exists to make impossible to reach
            // by deleting the declaration rather than by honouring it.
            if (Settings(configuration).TryGetProperty("test-projects", out JsonElement declared) && declared.GetArrayLength() > 0) { continue; }

            offenders.Add($"{Path.GetFileName(configuration)} names no test project, so nothing states which suite has to kill its mutants.");
        }

        Check.WithCustomMessage($"{offenders.Count} configuration(s) leave their oracle undeclared:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}")
             .That(offenders).IsEmpty();
    }

    private static JsonElement Settings(string configuration) {
        // The engine reads every setting from under this one object, so a file shaped otherwise is not a Stryker
        // configuration at all and the caller should hear that rather than see an empty answer.
        return JsonDocument.Parse(File.ReadAllText(configuration)).RootElement.GetProperty("stryker-config");
    }

    private static List<string> Configurations() {
        return Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "build", "stryker"), "*.json")
                        .OrderBy(path => path, StringComparer.Ordinal)
                        .ToList();
    }

    private static string RepositoryRoot() {
        AssemblyMetadataAttribute root = typeof(MutationOracleConventionTests).Assembly
                                                                             .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                                             .Single(metadata => metadata.Key == "RepositoryRoot");

        return Path.GetFullPath(root.Value!);
    }

}
