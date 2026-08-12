using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     What the golden files prove they look like, this suite proves they <b>are</b>.
/// </summary>
/// <remarks>
///     An approved file only records what the emitter produced; it cannot tell whether that text binds against
///     the real library, nor whether the library's own rules approve of it. Both are checked here, and the
///     second only means something because <see cref="AnEmittedFileIsCheckedByAnalyzersThatAreActuallyLoaded" />
///     shows a violation being caught in the same harness — otherwise "no diagnostics" and "no analyzers" look
///     exactly alike (§17.2).
/// </remarks>
public sealed class EmittedCodeCompilesTests {

    /// <summary>The one approved file that must NOT compile, and the reason it exists.</summary>
    private const string OpenParameterGolden = "AnyOrderWithTodo";

    /// <summary>What an entry-point golden's name carries, since it is the generator's plus a shape (§4.5).</summary>
    private const string EntryPointMarker = ".Entry.";

    /// <summary>
    ///     Each entry-point golden and the generator golden it reaches.
    /// </summary>
    /// <remarks>
    ///     Paired rather than compiled alone, because alone is not a state either file is ever in: the entry
    ///     point names the generator, and the two land in the developer's project together.
    ///     <see cref="EveryEntryPointGoldenIsCompiledWithItsGenerator" /> is what keeps this list honest.
    /// </remarks>
    public static TheoryData<string, string> EntryPointGoldens => new() {
        { "AnyOrder", "AnyOrder.Entry.Static" },
        { "AnyOrder", "AnyOrder.Entry.Any" },
        { "AnyPattern", "AnyPattern.Entry.Moved" },
        { "AnySession", "AnySession.Entry.Any" }
    };

    /// <summary>
    ///     Read from the folder rather than listed here, so a golden added later cannot be left uncompiled.
    /// </summary>
    public static TheoryData<string> CompilableGoldens {
        get {
            TheoryData<string> goldens = [];

            foreach (string name in GoldenFile.All()) {
                if (name != OpenParameterGolden && !IsEntryPoint(name)) { goldens.Add(name); }
            }

            return goldens;
        }
    }

    [Theory(DisplayName = "An emitted file compiles against the library, with no error.")]
    [MemberData(nameof(CompilableGoldens))]
    public void AnEmittedFileCompilesAgainstTheLibrary(string golden) {
        CSharpCompilation compilation = EmittedCodeCompiler.Compile(GoldenFile.ApprovedTextOf(golden));

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();
    }

    [Theory(DisplayName = "An emitted file raises none of the library's own rules.")]
    [MemberData(nameof(CompilableGoldens))]
    public async Task AnEmittedFileRaisesNoneOfTheLibrarysRules(string golden) {
        string[] raised = await JustDummiesDiagnosticsIn(GoldenFile.ApprovedTextOf(golden), TestContext.Current.CancellationToken);

        Check.That(raised).IsEmpty();
    }

    // Without this, the test above proves nothing: a harness that failed to load the analyzers would report
    // no diagnostics on every file and stay green forever (§17.2).
    [Fact(DisplayName = "The analyzers checking those files are actually loaded.")]
    public async Task AnEmittedFileIsCheckedByAnalyzersThatAreActuallyLoaded() {
        // A constraint chain left as a statement: it reads as if it mutated something, and it mutated nothing.
        // JD006, and one of the plainest violations the rule set has.
        const string violation = """
                                 using JustDummies;

                                 public static class Control {
                                     public static void DropsAConstrainedChain() {
                                         Any.String().NonEmpty();
                                     }
                                 }
                                 """;

        Check.That(await JustDummiesDiagnosticsIn(violation, TestContext.Current.CancellationToken)).Not.IsEmpty();
    }

    [Theory(DisplayName = "An entry point compiles with the generator it reaches, with no error.")]
    [MemberData(nameof(EntryPointGoldens))]
    public void AnEntryPointCompilesWithTheGeneratorItReaches(string generator, string entryPoint) {
        CSharpCompilation compilation = EmittedCodeCompiler.CompileTogether(GoldenFile.ApprovedTextOf(generator),
                                                                           GoldenFile.ApprovedTextOf(entryPoint));

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();
    }

    // The pair list above is written out, unlike CompilableGoldens which reads the folder. This is what stops
    // that from becoming a way to add a golden nothing compiles.
    [Fact(DisplayName = "Every entry-point golden is compiled with its generator.")]
    public void EveryEntryPointGoldenIsCompiledWithItsGenerator() {
        string[] onDisk = [.. GoldenFile.All().Where(IsEntryPoint)];
        string[] paired = [.. EntryPointGoldens.Select(row => row.Data.Item2)
                                               .OrderBy(name => name, StringComparer.Ordinal)];

        Check.That(paired).IsEqualTo(onDisk);
    }

    /// <summary>
    ///     The claim §4.5 rests on: <c>--entry-point any</c> needs C# 14, and nothing else the tool writes does.
    /// </summary>
    /// <remarks>
    ///     Asserted from both sides on purpose. That the extension member fails below C# 14 is what makes the
    ///     CLI's refusal a service rather than a formality; that the static root parses at 7.3 is what keeps
    ///     §4.4's floor a property of every file but this one.
    /// </remarks>
    [Fact(DisplayName = "The extension-member entry point needs C# 14, and the static root does not.")]
    public void TheExtensionMemberEntryPointNeedsCSharp14() {
        Check.That(ParseErrorsAt("AnyOrder.Entry.Any", LanguageVersion.CSharp13)).Not.IsEmpty();
        Check.That(ParseErrorsAt("AnyOrder.Entry.Any", LanguageVersion.CSharp14)).IsEmpty();
        Check.That(ParseErrorsAt("AnyPattern.Entry.Moved", LanguageVersion.CSharp7_3)).IsEmpty();
    }

    /// <summary>
    ///     The open parameter of §5.5 is not a defect of this file: it is the mechanism. The developer's build
    ///     names the missing identifier, at that line, in the IDE and in CI, the minute the file is written
    ///     (ADR-0060).
    /// </summary>
    [Fact(DisplayName = "An open parameter fails the developer's build, at its line, by name.")]
    public void AnOpenParameterFailsTheDevelopersBuild() {
        CSharpCompilation compilation = EmittedCodeCompiler.Compile(GoldenFile.ApprovedTextOf(OpenParameterGolden));

        Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
                                         .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                                         .ToArray();

        // Exactly one, and about the identifier: a second error would mean the emitter broke something else
        // while leaving the parameter open.
        Check.That(errors.Select(error => error.Id)).IsEquivalentTo("CS0103");
        Check.That(errors.Single().GetMessage()).Contains("TODO_supply_a_generator_for_customer");
    }

    /// <summary>
    ///     §4.4: the emitted code uses no construct newer than C# 7.3, because it lands in the developer's
    ///     project and compiles at that project's language version.
    /// </summary>
    /// <remarks>
    ///     The two files below are the ones that can be read at 7.3 in full. The others differ from them by the
    ///     namespace form alone, which §4.4 exempts by name — it is copied from the target type's own file, so
    ///     a file-scoped namespace is emitted only where the developer already writes one.
    /// </remarks>
    [Theory(DisplayName = "An emitted file parses at the C# 7.3 floor.")]
    [InlineData("AnyPattern")]
    [InlineData("AnySession")]
    public void AnEmittedFileParsesAtTheLanguageFloor(string golden) {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(GoldenFile.ApprovedTextOf(golden),
                                                     new CSharpParseOptions(LanguageVersion.CSharp7_3),
                                                     cancellationToken: TestContext.Current.CancellationToken);

        Check.That(tree.GetDiagnostics(TestContext.Current.CancellationToken).Select(diagnostic => diagnostic.Id)).IsEmpty();
    }

    private static bool IsEntryPoint(string golden) {
        return golden.Contains(EntryPointMarker, StringComparison.Ordinal);
    }

    private static string[] ParseErrorsAt(string golden, LanguageVersion version) {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(GoldenFile.ApprovedTextOf(golden),
                                                     new CSharpParseOptions(version),
                                                     cancellationToken: TestContext.Current.CancellationToken);

        return [.. tree.GetDiagnostics(TestContext.Current.CancellationToken).Select(diagnostic => diagnostic.Id)];
    }

    private static async Task<string[]> JustDummiesDiagnosticsIn(string source, CancellationToken cancellationToken) {
        CSharpCompilation          compilation = EmittedCodeCompiler.Compile(source);
        ImmutableArray<Diagnostic> raised      = await compilation.WithAnalyzers(EmittedCodeCompiler.Analyzers)
                                                                  .GetAnalyzerDiagnosticsAsync(cancellationToken);

        return raised.Where(diagnostic => diagnostic.Id.StartsWith("JD", StringComparison.Ordinal))
                     .Select(diagnostic => diagnostic.Id + " at " + diagnostic.Location.GetLineSpan())
                     .ToArray();
    }

}
