using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NFluent;

using Spectre.Console;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     <c>dum generate</c>, end to end over a compilation built here: the exit codes of §7 and where each
///     stream's text goes.
/// </summary>
/// <remarks>
///     Only the project loading is stood in for. Everything after it is the real thing — the real lookup, the
///     real resolution, the real emitter, the real writer — because the rows of §7 are about what a developer
///     meets, and a suite that replaced the engine too would only be checking that this file calls the methods
///     it calls.
/// </remarks>
public sealed class GenerateCommandTests : IDisposable {

    /// <summary>A guarded domain type, and one that nothing can construct.</summary>
    private const string Domain = """
                                  using System;

                                  namespace Shop.Domain;

                                  public sealed class Order {

                                      private readonly int kept;

                                      public Order(int quantity) {
                                          if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

                                          kept = quantity;
                                      }

                                  }

                                  public sealed class Basket {
                                      private Basket() { }
                                  }
                                  """;

    private readonly string directory = Directory.CreateTempSubdirectory("dum-generate-").FullName;

    /// <inheritdoc />
    public void Dispose() {
        Directory.Delete(directory, recursive: true);
    }

    [Fact(DisplayName = "A scaffold writes the file, recaps it on stdout, and succeeds.")]
    public async Task AScaffoldWritesTheFileAndRecapsIt() {
        Run run = await Generate(Settings("Order"));

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("Analyzing Shop.Domain.Order");
        Check.That(run.Output).Contains("✓ AnyOrder.cs");
        Check.That(run.Error).IsEmpty();
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("Any.Int32().Positive()");
    }

    /// <summary>
    ///     <c>--dry-run</c> puts the file on stdout and the recap on stderr, so one can be piped while the
    ///     other is read (§6).
    /// </summary>
    [Fact(DisplayName = "--dry-run prints the file to stdout, the recap to stderr, and writes nothing.")]
    public async Task ADryRunWritesNothing() {
        GenerateSettings settings = Settings("Order");

        settings.DryRun = true;

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).StartsWith("// Scaffolded by dum");
        Check.That(run.Error).Contains("✓ AnyOrder.cs");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    // A file carrying TODOs is a success: the write succeeded, and the developer's own build reports the rest
    // (ADR-0060).
    [Fact(DisplayName = "A file with a TODO in it is still a file, and still exit 0.")]
    public async Task AFileWithATodoStillSucceeds() {
        Run run = await Generate(Settings("Warehouse"), Compilation("""
                                                                    namespace Shop.Domain;

                                                                    public sealed class Crate { public Crate(int n) { } }

                                                                    public sealed class Warehouse {
                                                                        public Warehouse(Crate crate) { }
                                                                    }
                                                                    """));

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("TODO");
        Check.That(File.Exists(Path.Combine(directory, "AnyWarehouse.cs"))).IsTrue();
    }

    [Fact(DisplayName = "An existing file is refused, and the run fails without touching it.")]
    public async Task AnExistingFileIsRefused() {
        string path = Path.Combine(directory, "AnyOrder.cs");

        await File.WriteAllTextAsync(path, "// mine", TestContext.Current.CancellationToken);

        Run run = await Generate(Settings("Order"));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("--force");
        Check.That(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).IsEqualTo("// mine");
    }

    [Fact(DisplayName = "--force writes over it.")]
    public async Task ForceWritesOverIt() {
        GenerateSettings settings = Settings("Order");

        settings.Force = true;

        await File.WriteAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), "// mine",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("public sealed partial class AnyOrder");
    }

    [Fact(DisplayName = "--namespace puts the generator where it was asked to go.")]
    public async Task ANamespaceOverrideIsHonoured() {
        GenerateSettings settings = Settings("Order");

        settings.Namespace = "Shop.Tests.Dummies";

        await Generate(settings);

        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Tests.Dummies;");
    }

    [Fact(DisplayName = "A type that matched nothing fails, on stderr, with the closest name.")]
    public async Task ATypeThatMatchedNothingFails() {
        Run run = await Generate(Settings("Ordr"));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("Order");
        Check.That(run.Output).IsEmpty();
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    [Fact(DisplayName = "A type nothing constructs fails, saying what Generate() would have needed.")]
    public async Task ATypeNothingConstructsFails() {
        Run run = await Generate(Settings("Basket"));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("constructor");
    }

    /// <summary>
    ///     The types are processed independently and the exit code is the worst of them (§7).
    /// </summary>
    /// <remarks>
    ///     Both halves matter, and they pull in opposite directions: one failure must not cost the developer
    ///     the files that did scaffold, and it must not be lost either — a script reading exit 0 would take a
    ///     partial run for a whole one.
    /// </remarks>
    [Fact(DisplayName = "One failure among several types costs the exit code, not the other files.")]
    public async Task OneFailureCostsTheExitCodeAndNotTheOtherFiles() {
        Run run = await Generate(Settings("Ordr", "Order"));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(File.Exists(Path.Combine(directory, "AnyOrder.cs"))).IsTrue();
    }

    // Not a fact about the type: without the package nothing in this project resolves (ADR-0059), so the two
    // lines are said once rather than once per argument.
    [Fact(DisplayName = "A project without the library says so once, however many types were asked for.")]
    public async Task AProjectWithoutTheLibrarySaysSoOnce() {
        Run run = await Generate(Settings("Order", "Basket"), Compilation(Domain, withLibrary: false));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(Occurrences(run.Error, "dotnet add package JustDummies")).IsEqualTo(1);
    }

    [Fact(DisplayName = "A project that will not open fails with the diagnostics, verbatim.")]
    public async Task AProjectThatWillNotOpenFails() {
        Run run = await Run.Of(Settings("Order"),
                               (_, _) => Task.FromResult(LoadedProject.Failed(["MSB4025: the project file could not be opened."])));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("MSB4025: the project file could not be opened.");
        Check.That(run.Output).IsEmpty();
    }

    // On the way through, not only on failure: a project that opened with a warning about an unresolved
    // reference is exactly the project whose scaffold reads as "the tool inferred nothing".
    [Fact(DisplayName = "A project that opened with diagnostics still reports them.")]
    public async Task AProjectThatOpenedWithDiagnosticsStillReportsThem() {
        Run run = await Run.Of(Settings("Order"),
                               (_, _) => Task.FromResult(LoadedProject.Opened(Compilation(Domain), ["Reference not found."])));

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Error).Contains("! Reference not found.");
    }

    [Fact(DisplayName = "No project to analyze fails before anything is opened.")]
    public async Task NoProjectToAnalyzeFailsFirst() {
        GenerateSettings settings = Settings("Order");

        settings.Project = Path.Combine(directory, "Absent.csproj");

        Run run = await Run.Of(settings, (_, _) => throw new InvalidOperationException("The project must not be opened."));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("Absent.csproj");
    }

    private static int Occurrences(string text, string what) {
        return text.Split(what).Length - 1;
    }

    private GenerateSettings Settings(params string[] types) {
        return new GenerateSettings { Types = types, Output = directory, Project = Somewhere() };
    }

    /// <summary>
    ///     A project file that exists, so the locator settles on it and the opener is reached.
    /// </summary>
    /// <remarks>
    ///     Beside the output directory rather than in it, so that "nothing was written" is checkable by
    ///     looking at what is there.
    /// </remarks>
    private string Somewhere() {
        string project = Directory.CreateDirectory(Path.Combine(directory, "project")).FullName;
        string path    = Path.Combine(project, "Shop.Tests.csproj");

        if (!File.Exists(path)) { File.WriteAllText(path, "<Project Sdk=\"Microsoft.NET.Sdk\" />"); }

        return path;
    }

    private static Task<Run> Generate(GenerateSettings settings) {
        return Generate(settings, Compilation(Domain));
    }

    private static Task<Run> Generate(GenerateSettings settings, Compilation compilation) {
        return Run.Of(settings, (_, _) => Task.FromResult(LoadedProject.Opened(compilation, [])));
    }

    /// <summary>The compilation the command reads, with or without the library it resolves against.</summary>
    private static CSharpCompilation Compilation(string source, bool withLibrary = true) {
        return CSharpCompilation.Create("Shop.Tests",
                                        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
                                        References(withLibrary),
                                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                     nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>
    ///     The runtime's own assemblies, and the library when the case is about having it.
    /// </summary>
    /// <remarks>
    ///     This suite's output carries <c>JustDummies.dll</c> and the runtime lists it among the trusted
    ///     assemblies, so it is filtered out by name: left in, a compilation built <b>without</b> the library
    ///     would still see it, and §7's row for a project that does not reference it could never be checked.
    /// </remarks>
    private static ImmutableArray<MetadataReference> References(bool withLibrary) {
        List<MetadataReference> references = [];
        string                  trusted    = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;

        foreach (string path in trusted.Split(Path.PathSeparator)) {
            if (path.Length == 0 || Path.GetFileName(path).StartsWith("JustDummies", StringComparison.Ordinal)) { continue; }

            try {
                references.Add(MetadataReference.CreateFromFile(path));
            } catch (Exception unloadable) when (unloadable is IOException or BadImageFormatException or ArgumentException) {
                // A native or otherwise unloadable entry carries no metadata; skipping it is correct.
            }
        }

        if (withLibrary) { references.Add(MetadataReference.CreateFromFile(typeof(global::JustDummies.Any).Assembly.Location)); }

        return [.. references];
    }

    /// <summary>One invocation, and everything it said.</summary>
    private sealed record Run(int ExitCode, string Output, string Error) {

        internal static async Task<Run> Of(GenerateSettings settings, ProjectOpener open) {
            StringWriter output = new();
            StringWriter error  = new();

            IAnsiConsole outputConsole = ToolConsole.On(output);
            IAnsiConsole errorConsole  = ToolConsole.On(error);

            GenerateCommand command = new(new ToolConsoles(outputConsole, errorConsole), open);

            int exitCode = await command.RunAsync(settings, TestContext.Current.CancellationToken);

            return new Run(exitCode, output.ToString(), error.ToString());
        }

    }

}
