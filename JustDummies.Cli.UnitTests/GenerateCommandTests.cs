using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        Check.That(run.Output).Contains("✓ DummyOrder.cs");
        Check.That(run.Error).IsEmpty();
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("Dummy.Int32().Positive()");
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
        Check.That(run.Error).Contains("✓ DummyOrder.cs");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    // A file carrying TODOs is a success: the write succeeded, and the developer's own build reports the rest
    // (ADR-0060).
    [Fact(DisplayName = "A file with a TODO in it is still a file, and still exit 0.")]
    public async Task AFileWithATodoStillSucceeds() {
        Run run = await Generate(Settings("Warehouse"), Compilation("""
                                                                    namespace Shop.Domain;

                                                                    public sealed class Crate<T> { public Crate() { } }

                                                                    public sealed class Warehouse {
                                                                        public Warehouse(Crate<int> crate) { }
                                                                    }
                                                                    """));

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("TODO");
        Check.That(File.Exists(Path.Combine(directory, "DummyWarehouse.cs"))).IsTrue();
    }

    [Fact(DisplayName = "An existing file is refused, and the run fails without touching it.")]
    public async Task AnExistingFileIsRefused() {
        string path = Path.Combine(directory, "DummyOrder.cs");

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

        await File.WriteAllTextAsync(Path.Combine(directory, "DummyOrder.cs"), "// mine",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("public sealed partial class DummyOrder");
    }

    [Fact(DisplayName = "--namespace puts the generator where it was asked to go.")]
    public async Task ANamespaceOverrideIsHonoured() {
        GenerateSettings settings = Settings("Order");

        settings.Namespace = "Shop.Tests.Dummies";

        await Generate(settings);

        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Tests.Dummies;");
    }

    [Fact(DisplayName = "--entry-point static:<Name> writes a second file and recaps the call it opens.")]
    public async Task AStaticEntryPointWritesASecondFile() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "static:Dummies";

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("✓ DummyOrder.Entry.cs — entry point Dummies.Order()");
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.Entry.cs"), TestContext.Current.CancellationToken))
             .Contains("public static partial class Dummies {");
    }

    [Fact(DisplayName = "--entry-point dummy writes the extension member, on a project that can compile it.")]
    public async Task AnEntryPointOnDummyWritesTheExtensionMember() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "dummy";

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("entry point Dummy.Order()");
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.Entry.cs"), TestContext.Current.CancellationToken))
             .Contains("extension(Dummy) {");
    }

    /// <summary>
    ///     The refusal of §7 that is about the project rather than the type: what <c>--entry-point dummy</c>
    ///     writes needs C# 14, and this project does not compile at it.
    /// </summary>
    /// <remarks>
    ///     Refused before the first scaffold, so a run over several types says it once — and refused rather
    ///     than downgraded to a static root, which the developer would only discover at the call site.
    /// </remarks>
    [Fact(DisplayName = "--entry-point dummy is refused below C# 14, before anything is written.")]
    public async Task AnEntryPointOnDummyIsRefusedBelowCSharp14() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "dummy";

        Run run = await Generate(settings, Compilation(Domain, language: LanguageVersion.CSharp12));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("C# 14");
        Check.That(run.Error).Contains("--entry-point static:<Name>");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    // The same project, and the same option, once it can compile what the option writes.
    [Fact(DisplayName = "--entry-point static:<Name> needs no C# 14.")]
    public async Task AStaticEntryPointNeedsNoCSharp14() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "static:Dummies";

        Run run = await Generate(settings, Compilation(Domain, language: LanguageVersion.CSharp12));

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(File.Exists(Path.Combine(directory, "DummyOrder.Entry.cs"))).IsTrue();
    }

    /// <summary>
    ///     One scaffold is one unit of work on disk: either both its files land, or neither does.
    /// </summary>
    /// <remarks>
    ///     Half a scaffold under the exit code of a failure would be the worst of both — and the re-run with
    ///     <c>--force</c> that follows would silently overwrite the half that had landed.
    /// </remarks>
    [Fact(DisplayName = "An existing entry-point file stops the generator being written too.")]
    public async Task AnExistingEntryPointFileStopsTheWholeScaffold() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "static:Dummies";

        await File.WriteAllTextAsync(Path.Combine(directory, "DummyOrder.Entry.cs"), "// mine",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("DummyOrder.Entry.cs");
        Check.That(File.Exists(Path.Combine(directory, "DummyOrder.cs"))).IsFalse();
    }

    [Fact(DisplayName = "--dry-run prints both files and writes neither.")]
    public async Task ADryRunPrintsBothFiles() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "dummy";
        settings.DryRun     = true;

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("public sealed partial class DummyOrder");
        Check.That(run.Output).Contains("extension(Dummy) {");
        Check.That(run.Error).Contains("Both files are on stdout");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    // ADR-0062 stays intact: the generator does not move, so no call site pays an import for it. Only the
    // entry point does, which is what makes one root reachable across several namespaces.
    [Fact(DisplayName = "--entry-point-namespace moves the entry point and leaves the generator alone.")]
    public async Task AnEntryPointNamespaceMovesOnlyTheEntryPoint() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint          = "static:Dummies";
        settings.EntryPointNamespace = "Shop.Tests.Dummies";

        await Generate(settings);

        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Domain;");

        string entry = await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.Entry.cs"),
                                                   TestContext.Current.CancellationToken);

        Check.That(entry).Contains("namespace Shop.Tests.Dummies;");
        Check.That(entry).Contains("using Shop.Domain;");
    }

    [Fact(DisplayName = "No entry point asked for, one file written — the default is unchanged.")]
    public async Task NoEntryPointAskedForWritesOneFile() {
        await Generate(Settings("Order"));

        Check.That(File.Exists(Path.Combine(directory, "DummyOrder.Entry.cs"))).IsFalse();
    }

    /// <summary>
    ///     The number the exit code cannot carry (§6.1).
    /// </summary>
    /// <remarks>
    ///     §7 makes a file written with open parameters a success, which is right for a developer and useless
    ///     to a script over forty types: exit <c>0</c> reads the same either way. This is what closes that.
    /// </remarks>
    [Fact(DisplayName = "--format json reports the open parameters the exit code cannot.")]
    public async Task JsonReportsTheOpenParametersTheExitCodeCannot() {
        GenerateSettings settings = Settings("Warehouse");

        settings.Format = "json";

        Run run = await Generate(settings, Compilation("""
                                                       namespace Shop.Domain;

                                                       public sealed class Crate<T> { public Crate() { } }

                                                       public sealed class Warehouse {
                                                           public Warehouse(Crate<int> crate, string name) { }
                                                       }
                                                       """));

        JsonElement report = JsonDocument.Parse(run.Output).RootElement;

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(report.GetProperty("summary").GetProperty("openParameters").GetInt32()).IsEqualTo(1);
        Check.That(report.GetProperty("summary").GetProperty("scaffolded").GetInt32()).IsEqualTo(1);
        Check.That(report.GetProperty("results")[0].GetProperty("openParameters").GetInt32()).IsEqualTo(1);
    }

    /// <summary>
    ///     The other number a script branches on, and the row it has to agree with (§6.1).
    /// </summary>
    /// <remarks>
    ///     A parameter the engine inferred but cannot vouch for (§5.6) is <b>resolved</b> — it carries an
    ///     expression — and its file still does not compile. Counting it under <c>openParameters</c> would make
    ///     that number disagree with the rows reporting <c>resolved: false</c>, so it has a count of its own and
    ///     the row states the same fact. A script summing rows and a script reading the summary must not reach
    ///     two different answers about one document.
    /// </remarks>
    [Fact(DisplayName = "--format json counts a parameter to verify apart from an open one, and its row agrees.")]
    public async Task JsonCountsAParameterToVerifyApartFromAnOpenOne() {
        GenerateSettings settings = Settings("Reference");

        settings.Format = "json";

        Run run = await Generate(settings, Compilation("""
                                                       namespace Shop.Domain;

                                                       public sealed class Reference {

                                                           private readonly string value;

                                                           public Reference(string value) {
                                                               Validate(value);
                                                               this.value = value;
                                                           }

                                                           private static void Validate(string candidate) {
                                                               if (string.IsNullOrWhiteSpace(candidate)) { throw new System.ArgumentException(nameof(candidate)); }
                                                           }

                                                       }
                                                       """));

        JsonElement report    = JsonDocument.Parse(run.Output).RootElement;
        JsonElement summary   = report.GetProperty("summary");
        JsonElement parameter = report.GetProperty("results")[0].GetProperty("parameters")[0];

        Check.That(summary.GetProperty("openParameters").GetInt32()).IsEqualTo(0);
        Check.That(summary.GetProperty("parametersToVerify").GetInt32()).IsEqualTo(1);

        Check.That(parameter.GetProperty("resolved").GetBoolean()).IsTrue();
        Check.That(parameter.GetProperty("requiresVerification").GetBoolean()).IsTrue();
        Check.That(parameter.GetProperty("expression").GetString()).IsNotNull();
    }

    [Fact(DisplayName = "--format json carries each parameter, its expression and its provenance.")]
    public async Task JsonCarriesEachParameterAndItsProvenance() {
        GenerateSettings settings = Settings("Order");

        settings.Format = "json";

        Run          run       = await Generate(settings);
        JsonElement  quantity  = JsonDocument.Parse(run.Output).RootElement
                                             .GetProperty("results")[0].GetProperty("parameters")[0];

        Check.That(quantity.GetProperty("name").GetString()).IsEqualTo("quantity");
        Check.That(quantity.GetProperty("expression").GetString()).IsEqualTo("Dummy.Int32().Positive()");
        Check.That(quantity.GetProperty("resolved").GetBoolean()).IsTrue();
        Check.That(quantity.GetProperty("provenance")[0].GetString()).IsEqualTo("guard");
    }

    // stdout is the machine channel and carries the document alone; the recap is for a reader and would make
    // it unparseable.
    [Fact(DisplayName = "--format json puts one document on stdout, and no recap.")]
    public async Task JsonPutsOneDocumentOnStdoutAndNoRecap() {
        GenerateSettings settings = Settings("Order");

        settings.Format = "json";

        Run run = await Generate(settings);

        Check.That(run.Output).StartsWith("{");
        Check.That(run.Output).Not.Contains("Analyzing Shop.Domain.Order");
        Check.That(run.Output).Not.Contains("✓ DummyOrder.cs");
        Check.That(JsonDocument.Parse(run.Output).RootElement.GetProperty("results")[0]
                               .GetProperty("files")[0].GetProperty("written").GetBoolean()).IsTrue();
    }

    // Under --dry-run stdout would otherwise carry the file itself, which would not parse as JSON — so the
    // text travels inside the document instead of being lost.
    [Fact(DisplayName = "--format json --dry-run carries each file's text in the document.")]
    public async Task JsonDryRunCarriesEachFilesText() {
        GenerateSettings settings = Settings("Order");

        settings.Format = "json";
        settings.DryRun = true;

        Run         run  = await Generate(settings);
        JsonElement file = JsonDocument.Parse(run.Output).RootElement
                                       .GetProperty("results")[0].GetProperty("files")[0];

        Check.That(file.GetProperty("written").GetBoolean()).IsFalse();
        Check.That(file.GetProperty("path").ValueKind).IsEqualTo(JsonValueKind.Null);
        Check.That(file.GetProperty("text").GetString()).Contains("public sealed partial class DummyOrder");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    [Fact(DisplayName = "--format json names the entry point it emitted.")]
    public async Task JsonNamesTheEntryPointItEmitted() {
        GenerateSettings settings = Settings("Order");

        settings.Format     = "json";
        settings.EntryPoint = "static:Dummies";

        Run         run   = await Generate(settings);
        JsonElement entry = JsonDocument.Parse(run.Output).RootElement
                                        .GetProperty("results")[0].GetProperty("entryPoint");

        Check.That(entry.GetProperty("file").GetString()).IsEqualTo("DummyOrder.Entry.cs");
        Check.That(entry.GetProperty("call").GetString()).IsEqualTo("Dummies.Order()");
    }

    [Fact(DisplayName = "--format json records a refused argument, with its candidates.")]
    public async Task JsonRecordsARefusedArgument() {
        GenerateSettings settings = Settings("Ordr");

        settings.Format = "json";

        Run         run    = await Generate(settings);
        JsonElement result = JsonDocument.Parse(run.Output).RootElement.GetProperty("results")[0];

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(result.GetProperty("status").GetString()).IsEqualTo("TypeNotFound");
        Check.That(result.GetProperty("candidates")[0].GetString()).IsEqualTo("Order");
    }

    // "stdout carries one JSON document" with no exception to remember: a run that stopped before its first
    // scaffold produces one too, naming why, rather than leaving a script to parse nothing.
    [Fact(DisplayName = "--format json still produces a document when the run never started.")]
    public async Task JsonStillProducesADocumentWhenTheRunNeverStarted() {
        GenerateSettings settings = Settings("Order");

        settings.Format  = "json";
        settings.Project = Path.Combine(directory, "Absent.csproj");

        Run run = await Run.Of(settings, (_, _) => throw new InvalidOperationException("The project must not be opened."));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(JsonDocument.Parse(run.Output).RootElement.GetProperty("refusal").GetString()).IsEqualTo("NoProject");
    }

    [Fact(DisplayName = "A dum.json beside the project sets what the command line did not.")]
    public async Task ADumJsonBesideTheProjectSetsTheDefaults() {
        GenerateSettings settings = Settings("Order");

        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(settings.Project)!, "dum.json"),
                                     """{ "namespace": "Shop.Tests.Dummies", "entryPoint": "static:Dummies" }""",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "DummyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Tests.Dummies;");
        Check.That(File.Exists(Path.Combine(directory, "DummyOrder.Entry.cs"))).IsTrue();
    }

    // Refused, not ignored: a default someone believes is in force and is not would be worse than no file. It
    // is exit 2 because the tool never got as far as scaffolding — what it could not read is an instruction.
    [Fact(DisplayName = "A dum.json the tool cannot read stops the run, at exit 2.")]
    public async Task AnUnreadableDumJsonStopsTheRun() {
        GenerateSettings settings = Settings("Order");

        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(settings.Project)!, "dum.json"),
                                     """{ "entryPoint": "static:Dummy" }""",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(2);
        Check.That(run.Error).Contains("Dummy.Int32()");
        Check.That(run.Error).Contains("dum.json");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
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
        Check.That(File.Exists(Path.Combine(directory, "DummyOrder.cs"))).IsTrue();
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

    /// <summary>
    ///     The compilation the command reads, with or without the library it resolves against.
    /// </summary>
    /// <remarks>
    ///     The language version is a parameter because one refusal turns on it and on nothing else: what
    ///     <c>--entry-point dummy</c> writes is a C# 14 construct, and the target framework has no say in it
    ///     (§4.5).
    /// </remarks>
    private static CSharpCompilation Compilation(string source,
                                                 bool withLibrary = true,
                                                 LanguageVersion language = LanguageVersion.Latest) {
        return CSharpCompilation.Create("Shop.Tests",
                                        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(language))],
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

        if (withLibrary) { references.Add(MetadataReference.CreateFromFile(typeof(global::JustDummies.Dummy).Assembly.Location)); }

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
