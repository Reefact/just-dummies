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

    /// <summary>
    ///     Read from the folder rather than listed here, so a golden added later cannot be left uncompiled.
    /// </summary>
    public static TheoryData<string> CompilableGoldens {
        get {
            TheoryData<string> goldens = new();

            foreach (string name in GoldenFile.All()) {
                if (name != OpenParameterGolden) { goldens.Add(name); }
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

    private static async Task<string[]> JustDummiesDiagnosticsIn(string source, CancellationToken cancellationToken) {
        CSharpCompilation          compilation = EmittedCodeCompiler.Compile(source);
        ImmutableArray<Diagnostic> raised      = await compilation.WithAnalyzers(EmittedCodeCompiler.Analyzers)
                                                                  .GetAnalyzerDiagnosticsAsync(cancellationToken);

        return raised.Where(diagnostic => diagnostic.Id.StartsWith("JD", StringComparison.Ordinal))
                     .Select(diagnostic => diagnostic.Id + " at " + diagnostic.Location.GetLineSpan())
                     .ToArray();
    }

}
