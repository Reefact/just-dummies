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
///     <c>any generate</c>, end to end over a compilation built here: the exit codes of §7 and where each
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

    /// <summary>
    ///     A type and the type its name is a prefix of, in one namespace: <c>Voice</c>'s entry class is
    ///     <c>AnyVoiceEntry</c>, which is <c>VoiceEntry</c>'s generator (§4.5).
    /// </summary>
    private const string Voices = """
                                  using System.Collections.Generic;

                                  namespace Shop.Domain;

                                  public sealed class VoiceEntry {
                                      public VoiceEntry(int pitch) { }
                                  }

                                  public sealed class Voice {
                                      public Voice(int number, IReadOnlyList<VoiceEntry> entries) { }
                                  }
                                  """;

    private readonly string directory = Directory.CreateTempSubdirectory("any-generate-").FullName;

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
        Check.That(run.Output).StartsWith("// Scaffolded by `any`");
        Check.That(run.Error).Contains("✓ AnyOrder.cs");
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

        settings.Namespace = "Shop.Tests.Arbitrary";

        await Generate(settings);

        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Tests.Arbitrary;");
    }

    // --namespace moves the file, not the type: the recap and the report name what was analysed, and
    // `Shop.Tests.Arbitrary.Order` is a type nobody declared.
    [Fact(DisplayName = "--namespace leaves the recap naming the type where it is declared.")]
    public async Task ANamespaceOverrideLeavesTheRecapNamingTheDeclaredType() {
        GenerateSettings settings = Settings("Order");

        settings.Namespace = "Shop.Tests.Arbitrary";

        Run run = await Generate(settings);

        Check.That(run.Output).Contains("Analyzing Shop.Domain.Order");
        Check.That(run.Output).Not.Contains("Shop.Tests.Arbitrary.Order");
    }

    [Fact(DisplayName = "--namespace leaves the JSON report naming the type where it is declared.")]
    public async Task ANamespaceOverrideLeavesTheJsonNamingTheDeclaredType() {
        GenerateSettings settings = Settings("Order");

        settings.Namespace = "Shop.Tests.Arbitrary";
        settings.Format    = "json";

        Run         run    = await Generate(settings);
        JsonElement result = JsonDocument.Parse(run.Output).RootElement.GetProperty("results")[0];

        Check.That(result.GetProperty("type").GetString()).IsEqualTo("Shop.Domain.Order");
    }

    [Fact(DisplayName = "--entry-point static:<Name> writes a second file and recaps the call it opens.")]
    public async Task AStaticEntryPointWritesASecondFile() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "static:Arbitrary";

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("✓ AnyOrder.Entry.cs — entry point Arbitrary.Order()");
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.Entry.cs"), TestContext.Current.CancellationToken))
             .Contains("public static partial class Arbitrary {");
    }

    [Fact(DisplayName = "--entry-point any writes the extension member, on a project that can compile it.")]
    public async Task AnEntryPointOnAnyWritesTheExtensionMember() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "any";

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("entry point Any.Order()");
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.Entry.cs"), TestContext.Current.CancellationToken))
             .Contains("extension(Any) {");
    }

    /// <summary>
    ///     The refusal of §7 that is about the project rather than the type: what <c>--entry-point any</c>
    ///     writes needs C# 14, and this project does not compile at it.
    /// </summary>
    /// <remarks>
    ///     Refused before the first scaffold, so a run over several types says it once — and refused rather
    ///     than downgraded to a static root, which the developer would only discover at the call site.
    /// </remarks>
    [Fact(DisplayName = "--entry-point any is refused below C# 14, before anything is written.")]
    public async Task AnEntryPointOnAnyIsRefusedBelowCSharp14() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "any";

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

        settings.EntryPoint = "static:Arbitrary";

        Run run = await Generate(settings, Compilation(Domain, language: LanguageVersion.CSharp12));

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(File.Exists(Path.Combine(directory, "AnyOrder.Entry.cs"))).IsTrue();
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

        settings.EntryPoint = "static:Arbitrary";

        await File.WriteAllTextAsync(Path.Combine(directory, "AnyOrder.Entry.cs"), "// mine",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("AnyOrder.Entry.cs");
        Check.That(File.Exists(Path.Combine(directory, "AnyOrder.cs"))).IsFalse();
    }

    /// <summary>
    ///     The pre-write name check of §4.5: <c>--entry-point any</c> declares <c>Any{Type}Entry</c>, which is
    ///     exactly the generator name of a domain type <c>{Type}Entry</c>.
    /// </summary>
    /// <remarks>
    ///     Refused, not renamed, and before anything is written for that type: a run that reported
    ///     <c>✓ AnyVoice.Entry.cs</c> and then failed the developer's build with <c>CS0260</c> is the outcome
    ///     ADR-0046 names rather than works around. The other type of the batch is unaffected — the types are
    ///     processed independently (§7), and its files are the ones the entry point would have collided with.
    /// </remarks>
    [Fact(DisplayName = "An entry class named like another type's generator is refused before it is written.")]
    public async Task AnEntryClassNamedLikeAnotherGeneratorIsRefused() {
        GenerateSettings settings = Settings("VoiceEntry", "Voice");

        settings.EntryPoint = "any";

        Run run = await Generate(settings, Compilation(Voices));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("AnyVoiceEntry");
        Check.That(run.Error).Contains("AnyVoice.Entry.cs");
        Check.That(run.Error).Contains("AnyVoiceEntry.cs");
        Check.That(run.Error).Contains("--entry-point-namespace");
        Check.That(File.Exists(Path.Combine(directory, "AnyVoice.cs"))).IsFalse();
        Check.That(File.Exists(Path.Combine(directory, "AnyVoice.Entry.cs"))).IsFalse();
        Check.That(File.Exists(Path.Combine(directory, "AnyVoiceEntry.cs"))).IsTrue();
        Check.That(File.Exists(Path.Combine(directory, "AnyVoiceEntry.Entry.cs"))).IsTrue();
    }

    // The same clash, met one run later: the generator of VoiceEntry is already in the project, so the entry
    // class of Voice would redeclare a type the compilation holds. --dry-run has to say it too — it is the
    // run a developer makes to see what would happen, and exit 0 there was the report's own finding.
    [Fact(DisplayName = "--dry-run refuses an entry class whose name the project already declares.")]
    public async Task ADryRunRefusesAnEntryClassTheProjectAlreadyDeclares() {
        GenerateSettings settings = Settings("Voice");

        settings.EntryPoint = "any";
        settings.DryRun     = true;

        CSharpCompilation compilation = Compilation(Voices).AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace Shop.Domain;\n\npublic sealed class AnyVoiceEntry { }\n",
                                       new CSharpParseOptions(LanguageVersion.Latest),
                                       path: Path.Combine(directory, "AnyVoiceEntry.cs"),
                                       cancellationToken: TestContext.Current.CancellationToken));

        Run run = await Generate(settings, compilation);

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("AnyVoiceEntry.cs");
        Check.That(run.Error).Contains("AnyVoice.Entry.cs");
        Check.That(run.Output).IsEmpty();
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    /// <summary>
    ///     The entry point a previous run wrote is the file <c>--force</c> replaces, not a clash (review of #417).
    /// </summary>
    /// <remarks>
    ///     On the second run the compilation already holds <c>AnyOrderEntry</c>, declared by the very
    ///     <c>AnyOrder.Entry.cs</c> this run would write — and the file's own header names <c>--force</c> as the
    ///     way to overwrite it. Refusing that as a name clash left the entry-point mode unable to regenerate its
    ///     own output.
    /// </remarks>
    [Fact(DisplayName = "A second run with --force regenerates the entry point it wrote the first time.")]
    public async Task ASecondRunWithForceRegeneratesTheEntryPoint() {
        GenerateSettings first = Settings("Order");

        first.EntryPoint = "any";

        Check.That((await Generate(first)).ExitCode).IsEqualTo(0);

        CSharpCompilation withTheFirstRun = WithFilesOf(Compilation(Domain), "AnyOrder.cs", "AnyOrder.Entry.cs");

        GenerateSettings forced = Settings("Order");

        forced.EntryPoint = "any";
        forced.Force      = true;

        Run run = await Generate(forced, withTheFirstRun);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Error).Not.Contains("already taken");
    }

    // The same second run without --force is refused for the reason the developer can act on: the file exists.
    [Fact(DisplayName = "A second run without --force is refused because the file exists, not as a name clash.")]
    public async Task ASecondRunWithoutForceIsRefusedBecauseTheFileExists() {
        GenerateSettings first = Settings("Order");

        first.EntryPoint = "any";

        await Generate(first);

        GenerateSettings again = Settings("Order");

        again.EntryPoint = "any";

        Run run = await Generate(again, WithFilesOf(Compilation(Domain), "AnyOrder.cs", "AnyOrder.Entry.cs"));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("--force");
        Check.That(run.Error).Not.Contains("already taken");
    }

    /// <summary>
    ///     Only the file this run actually replaces is exempt: the same name in another directory is a clash
    ///     (review of #417).
    /// </summary>
    /// <remarks>
    ///     <c>src/Legacy/AnyOrder.Entry.cs</c> already declares <c>AnyOrderEntry</c>; a run with
    ///     <c>--output src/Generated</c> would write a second file of the same name elsewhere and the project would
    ///     fail with <c>CS0101</c>. Comparing the file name alone let that through, <c>--force</c> or not.
    /// </remarks>
    [Fact(DisplayName = "The same entry-point file in another directory is a clash, not the file being replaced.")]
    public async Task TheSameEntryPointFileInAnotherDirectoryIsAClash() {
        GenerateSettings legacy = Settings("Order");

        legacy.EntryPoint = "any";
        legacy.Output     = Path.Combine(directory, "Legacy");

        Check.That((await Generate(legacy)).ExitCode).IsEqualTo(0);

        CSharpCompilation withTheLegacyFile = WithFilesOf(Compilation(Domain), Path.Combine("Legacy", "AnyOrder.cs"), Path.Combine("Legacy", "AnyOrder.Entry.cs"));

        GenerateSettings generated = Settings("Order");

        generated.EntryPoint = "any";
        generated.Output     = Path.Combine(directory, "Generated");
        generated.Force      = true;

        Run run = await Generate(generated, withTheLegacyFile);

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(run.Error).Contains("already taken");
        Check.That(Directory.Exists(Path.Combine(directory, "Generated"))).IsFalse();
    }

    [Fact(DisplayName = "--format json records an entry class whose name is taken, naming both declarations.")]
    public async Task JsonRecordsAnEntryClassWhoseNameIsTaken() {
        GenerateSettings settings = Settings("VoiceEntry", "Voice");

        settings.EntryPoint = "any";
        settings.Format     = "json";

        Run         run    = await Generate(settings, Compilation(Voices));
        JsonElement result = JsonDocument.Parse(run.Output).RootElement.GetProperty("results")[1];

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(result.GetProperty("status").GetString()).IsEqualTo("EntryPointNameTaken");
        Check.That(result.GetProperty("candidates")[0].GetString()).Contains("AnyVoice.Entry.cs");
        Check.That(result.GetProperty("candidates")[1].GetString()).Contains("AnyVoiceEntry.cs");
    }

    [Fact(DisplayName = "--dry-run prints both files and writes neither.")]
    public async Task ADryRunPrintsBothFiles() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint = "any";
        settings.DryRun     = true;

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(run.Output).Contains("public sealed partial class AnyOrder");
        Check.That(run.Output).Contains("extension(Any) {");
        Check.That(run.Error).Contains("Both files are on stdout");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    // ADR-0062 stays intact: the generator does not move, so no call site pays an import for it. Only the
    // entry point does, which is what makes one root reachable across several namespaces.
    [Fact(DisplayName = "--entry-point-namespace moves the entry point and leaves the generator alone.")]
    public async Task AnEntryPointNamespaceMovesOnlyTheEntryPoint() {
        GenerateSettings settings = Settings("Order");

        settings.EntryPoint          = "static:Arbitrary";
        settings.EntryPointNamespace = "Shop.Tests.Arbitrary";

        await Generate(settings);

        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Domain;");

        string entry = await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.Entry.cs"),
                                                   TestContext.Current.CancellationToken);

        Check.That(entry).Contains("namespace Shop.Tests.Arbitrary;");
        Check.That(entry).Contains("using Shop.Domain;");
    }

    [Fact(DisplayName = "No entry point asked for, one file written — the default is unchanged.")]
    public async Task NoEntryPointAskedForWritesOneFile() {
        await Generate(Settings("Order"));

        Check.That(File.Exists(Path.Combine(directory, "AnyOrder.Entry.cs"))).IsFalse();
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

    // The row's `type` is the file's spelling, which without nullable annotations has no `?`; the flag is what tells a
    // script the domain declared one, the same fact the recap notes under its rows (issue #344).
    [Fact(DisplayName = "--format json says on each row whether a declared nullable annotation was dropped.")]
    public async Task JsonSaysWhetherANullableAnnotationWasDropped() {
        GenerateSettings settings = Settings("Party");

        settings.Format = "json";

        CSharpCompilation compilation = Compilation("""
                                                    namespace Shop.Domain;

                                                    public sealed class Party {
                                                        public Party(string? note, string name) { }
                                                    }
                                                    """);

        Run run = await Generate(settings, compilation.WithOptions(compilation.Options.WithNullableContextOptions(NullableContextOptions.Disable)));

        JsonElement parameters = JsonDocument.Parse(run.Output).RootElement.GetProperty("results")[0].GetProperty("parameters");

        Check.That(parameters[0].GetProperty("type").GetString()).IsEqualTo("string");
        Check.That(parameters[0].GetProperty("nullableAnnotationDropped").GetBoolean()).IsTrue();
        Check.That(parameters[1].GetProperty("nullableAnnotationDropped").GetBoolean()).IsFalse();
    }

    [Fact(DisplayName = "--format json carries each parameter, its expression and its provenance.")]
    public async Task JsonCarriesEachParameterAndItsProvenance() {
        GenerateSettings settings = Settings("Order");

        settings.Format = "json";

        Run          run       = await Generate(settings);
        JsonElement  quantity  = JsonDocument.Parse(run.Output).RootElement
                                             .GetProperty("results")[0].GetProperty("parameters")[0];

        Check.That(quantity.GetProperty("name").GetString()).IsEqualTo("quantity");
        Check.That(quantity.GetProperty("expression").GetString()).IsEqualTo("Any.Int32().Positive()");
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
        Check.That(run.Output).Not.Contains("✓ AnyOrder.cs");
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
        Check.That(file.GetProperty("text").GetString()).Contains("public sealed partial class AnyOrder");
        Check.That(Directory.GetFiles(directory)).IsEmpty();
    }

    [Fact(DisplayName = "--format json names the entry point it emitted.")]
    public async Task JsonNamesTheEntryPointItEmitted() {
        GenerateSettings settings = Settings("Order");

        settings.Format     = "json";
        settings.EntryPoint = "static:Arbitrary";

        Run         run   = await Generate(settings);
        JsonElement entry = JsonDocument.Parse(run.Output).RootElement
                                        .GetProperty("results")[0].GetProperty("entryPoint");

        Check.That(entry.GetProperty("file").GetString()).IsEqualTo("AnyOrder.Entry.cs");
        Check.That(entry.GetProperty("call").GetString()).IsEqualTo("Arbitrary.Order()");
    }

    [Fact(DisplayName = "--format json names the guard a parameter to verify could not have read.")]
    public async Task JsonNamesTheUnreadGuard() {
        GenerateSettings settings = Settings("Gauge");

        settings.Format = "json";

        Run run = await Generate(settings, Compilation("""
                                                       namespace Shop.Domain;

                                                       public sealed class Gauge {
                                                           public Gauge(int level) {
                                                               if (level % 2 == 1) { throw new System.ArgumentOutOfRangeException(nameof(level)); }
                                                           }
                                                       }
                                                       """));

        JsonElement unread = JsonDocument.Parse(run.Output).RootElement
                                         .GetProperty("results")[0].GetProperty("parameters")[0].GetProperty("unreadGuard");

        Check.That(unread.GetProperty("line").GetInt32()).IsEqualTo(5);
        Check.That(unread.GetProperty("excerpt").GetString()).StartsWith("if (level % 2 == 1)");
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

    // The engine scaffolded, the shell refused to write: the report has to say what the exit code says, or a
    // script reading the summary believes in a file that was never written.
    [Fact(DisplayName = "--format json records a file it refused to overwrite as a failure.")]
    public async Task JsonRecordsAnExistingFileAsAFailure() {
        string           path     = Path.Combine(directory, "AnyOrder.cs");
        GenerateSettings settings = Settings("Order");

        settings.Format = "json";

        await File.WriteAllTextAsync(path, "// mine", TestContext.Current.CancellationToken);

        Run         run     = await Generate(settings);
        JsonElement report  = JsonDocument.Parse(run.Output).RootElement;
        JsonElement result  = report.GetProperty("results")[0];
        JsonElement file    = result.GetProperty("files")[0];
        JsonElement summary = report.GetProperty("summary");

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(result.GetProperty("status").GetString()).IsEqualTo("FileExists");
        Check.That(file.GetProperty("path").GetString()).IsEqualTo(path);
        Check.That(file.GetProperty("written").GetBoolean()).IsFalse();
        Check.That(summary.GetProperty("scaffolded").GetInt32()).IsEqualTo(0);
        Check.That(summary.GetProperty("failed").GetInt32()).IsEqualTo(1);
        Check.That(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).IsEqualTo("// mine");
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

    [Fact(DisplayName = "An any.json beside the project sets what the command line did not.")]
    public async Task AnAnyJsonBesideTheProjectSetsTheDefaults() {
        GenerateSettings settings = Settings("Order");

        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(settings.Project)!, "any.json"),
                                     """{ "namespace": "Shop.Tests.Arbitrary", "entryPoint": "static:Arbitrary" }""",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(0);
        Check.That(await File.ReadAllTextAsync(Path.Combine(directory, "AnyOrder.cs"), TestContext.Current.CancellationToken))
             .Contains("namespace Shop.Tests.Arbitrary;");
        Check.That(File.Exists(Path.Combine(directory, "AnyOrder.Entry.cs"))).IsTrue();
    }

    // Refused, not ignored: a default someone believes is in force and is not would be worse than no file. It
    // is exit 2 because the tool never got as far as scaffolding — what it could not read is an instruction.
    [Fact(DisplayName = "An any.json the tool cannot read stops the run, at exit 2.")]
    public async Task AnUnreadableAnyJsonStopsTheRun() {
        GenerateSettings settings = Settings("Order");

        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(settings.Project)!, "any.json"),
                                     """{ "entryPoint": "static:Any" }""",
                                     TestContext.Current.CancellationToken);

        Run run = await Generate(settings);

        Check.That(run.ExitCode).IsEqualTo(2);
        Check.That(run.Error).Contains("Any.Int32()");
        Check.That(run.Error).Contains("any.json");
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

    // An unrestored project opens without its packages and looks exactly like one that never named the
    // library; telling its developer to add a reference they can see in their .csproj is the wrong remedy.
    [Fact(DisplayName = "A project that was never restored is told to restore, not to add the package.")]
    public async Task AnUnrestoredProjectIsToldToRestore() {
        Run run = await Run.Of(Settings("Order", "Basket"),
                               (_, _) => Task.FromResult(LoadedProject.Unrestored(Compilation(Domain, withLibrary: false), [])));

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(Occurrences(run.Error, "dotnet restore")).IsEqualTo(1);
        Check.That(run.Error).Not.Contains("dotnet add package");
    }

    [Fact(DisplayName = "--format json records a project that was never restored as such.")]
    public async Task JsonRecordsAnUnrestoredProject() {
        GenerateSettings settings = Settings("Order");

        settings.Format = "json";

        Run run = await Run.Of(settings,
                               (_, _) => Task.FromResult(LoadedProject.Unrestored(Compilation(Domain, withLibrary: false), [])));
        JsonElement result = JsonDocument.Parse(run.Output).RootElement.GetProperty("results")[0];

        Check.That(run.ExitCode).IsEqualTo(1);
        Check.That(result.GetProperty("status").GetString()).IsEqualTo("ProjectNotRestored");
    }

    // A project that resolves the library scaffolds whatever the assets file says: the check only ever
    // changes which refusal a missing library gets.
    [Fact(DisplayName = "A project that resolves the library scaffolds, restored or not.")]
    public async Task AProjectResolvingTheLibraryScaffoldsRestoredOrNot() {
        Run run = await Run.Of(Settings("Order"),
                               (_, _) => Task.FromResult(LoadedProject.Unrestored(Compilation(Domain), [])));

        Check.That(run.ExitCode).IsEqualTo(0);
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

    /// <summary>The compilation as the next run sees it: the files a previous run wrote, added to the project.</summary>
    private CSharpCompilation WithFilesOf(CSharpCompilation compilation, params string[] written) {
        foreach (string file in written) {
            string path = Path.Combine(directory, file);

            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(File.ReadAllText(path),
                                                                                new CSharpParseOptions(LanguageVersion.Latest),
                                                                                path: path,
                                                                                cancellationToken: TestContext.Current.CancellationToken));
        }

        return compilation;
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
    ///     <c>--entry-point any</c> writes is a C# 14 construct, and the target framework has no say in it
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
