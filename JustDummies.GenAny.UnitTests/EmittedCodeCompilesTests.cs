using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
[SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = "Names the markers the tool emits by design (§5.5), not unfinished work here.")]
public sealed class EmittedCodeCompilesTests {

    /// <summary>The one approved file that must NOT compile, and the reason it exists.</summary>
    private const string OpenParameterGolden = "AnyOrderWithTodo";

    /// <summary>
    ///     The other approved file that must NOT compile — a different reason, the same mechanism (§5.5).
    /// </summary>
    private const string RequiresVerificationGolden = "AnyOrderRequiringVerification";

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
                if (name != OpenParameterGolden && name != RequiresVerificationGolden && !IsEntryPoint(name)) {
                    goldens.Add(name);
                }
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

    [Theory(DisplayName = "An emitted file raises no warning from the library's own rules.")]
    [MemberData(nameof(CompilableGoldens))]
    public async Task AnEmittedFileRaisesNoWarningFromTheLibrarysRules(string golden) {
        string[] raised = await JustDummiesWarningsIn(GoldenFile.ApprovedTextOf(golden), TestContext.Current.CancellationToken);

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

        Check.That(await JustDummiesWarningsIn(violation, TestContext.Current.CancellationToken)).Not.IsEmpty();
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
    ///     A composed parameter whose generator does not exist yet fails the build the same way, and that is
    ///     the whole of ADR-0089: the same mechanism as the open parameter above, spelled as the type name to
    ///     scaffold rather than as an invented identifier.
    /// </summary>
    /// <remarks>
    ///     The difference from §5.5 is what the developer reads. <c>TODO_supply_a_generator_for_reference</c>
    ///     says something is missing; <c>AnyOrderReference</c> says which type to run <c>dum generate</c> on.
    ///     So this file carries no sentinel and no comment — there is nothing left for one to add — and the
    ///     recap does not flag it either, because a file that does not compile is not a silence.
    /// </remarks>
    [Fact(DisplayName = "A composed parameter with no generator yet fails the build, naming the type to scaffold.")]
    public void AComposedParameterWithNoGeneratorYetFailsTheBuild() {
        const string domain = """
                              namespace Shop.Domain;

                              public sealed class OrderReference {
                                  public static OrderReference Create(string value) { return new OrderReference(); }
                              }

                              public sealed class Basket {
                                  public Basket(OrderReference reference) { }
                              }
                              """;

        ScaffoldOutcome outcome = Subject.ScaffoldByName("Basket", domain);

        Check.That(outcome.Succeeded).IsTrue();

        string emitted = outcome.File!.SourceText;

        // Straight into the initializer, with no method wrapping one call and no sentinel above it.
        Check.That(emitted).Contains("reference: new AnyOrderReference()");
        Check.That(emitted).Not.Contains("AnyValidReference");
        Check.That(outcome.File.ContainsTodo).IsFalse();

        Diagnostic[] errors = EmittedCodeCompiler.CompileWith(emitted, domain)
                                                 .GetDiagnostics(TestContext.Current.CancellationToken)
                                                 .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                                                 .ToArray();

        // Exactly one, and about the name: a second error would mean the emitter broke something else while
        // naming a generator the compilation does not carry.
        Check.That(errors.Select(error => error.Id)).IsEquivalentTo("CS0246");
        Check.That(errors.Single().GetMessage()).Contains("AnyOrderReference");
    }

    /// <summary>
    ///     A guard the engine cannot vouch for is not a defect of this file either: it is the same mechanism
    ///     as the open parameter above, applied where a generator WAS inferred — so it stays right underneath
    ///     the line that blocks it, once the developer deletes that line (§5.5, ADR-0082's follow-up).
    /// </summary>
    [Fact(DisplayName = "A parameter requiring verification fails the developer's build, with its working base intact.")]
    public void AParameterRequiringVerificationFailsTheDevelopersBuildWithItsBaseIntact() {
        string             text        = GoldenFile.ApprovedTextOf(RequiresVerificationGolden);
        CSharpCompilation  compilation = EmittedCodeCompiler.Compile(text);

        Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
                                         .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                                         .ToArray();

        // Exactly one, same as the open-parameter case: the discard assignment is what keeps a second,
        // unrelated CS0201 (not a valid statement expression) from muddying what the developer needs to read.
        Check.That(errors.Select(error => error.Id)).IsEquivalentTo("CS0103");
        Check.That(errors.Single().GetMessage()).Contains("TODO_verify_the_generator_for_customer");

        // The point of this mechanism, proven rather than assumed: the working recipe is still there to keep
        // or replace, one line below the one that blocks compilation.
        Check.That(text).Contains("return new AnyCustomer();");
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

    /// <summary>
    ///     The odd parameter names §17 promises work, and the two ways they used not to.
    /// </summary>
    /// <remarks>
    ///     Roslyn reports <c>@event</c> as <c>event</c>, so writing the name down as it comes gave a file that
    ///     did not <b>parse</b> — and a file that does not parse carries no named identifier at a line for
    ///     ADR-0060 to point the developer at, while the recap claimed every parameter inferred.
    ///     <para>
    ///         <c>_id</c> failed the other way round, and worse: the field §4.2 derives from it carried the same
    ///         identifier as the constructor parameter, so the emitted assignment was <c>_id = _id</c>. That
    ///         compiles — three warnings, no error — and leaves the field null, so every draw throws and no
    ///         <c>WithId(…)</c> can rescue it, since the pinning overloads route through the same constructor.
    ///         Compiling is therefore not enough here: the assignment has to be read as well.
    ///     </para>
    ///     <para>
    ///         Neither name is exotic. <c>@event</c> is ordinary in an event-sourced domain, and an
    ///         underscore-prefixed constructor parameter is one house style among several.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A parameter whose name needs escaping or stripping emits a file that compiles.")]
    [InlineData("@event", "@event", "_event")]
    [InlineData("@class", "@class", "_class")]
    [InlineData("_id", "id", "_id")]
    public void AnOddParameterNameEmitsAFileThatCompiles(string declared, string identifier, string field) {
        string domain = $$"""
                          namespace Shop.Domain;

                          public sealed class Envelope {
                              public Envelope(string {{declared}}) { }
                          }
                          """;

        ScaffoldOutcome outcome = Subject.ScaffoldByName("Envelope", domain);

        Check.That(outcome.Succeeded).IsTrue();

        string emitted = outcome.File!.SourceText;

        // The parameter carries the escape Roslyn dropped, and the field it feeds is a different identifier.
        Check.That(emitted).Contains($"IAny<string> {identifier})");
        Check.That(emitted).Contains($"{field} = {identifier};");
        Check.That(EmittedCodeCompiler.ErrorsIn(EmittedCodeCompiler.CompileWith(emitted, domain))).IsEmpty();
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

    /// <summary>
    ///     The library's own rules raised at warning level or above on <paramref name="source" />.
    /// </summary>
    /// <remarks>
    ///     Informational rules are deliberately excluded. The scaffolder writes each file once and transfers
    ///     ownership of it to the developer (ADR-0056), so what it emits is a starting point, not a finished
    ///     arrange line: JD030 pointing at a string whose length nobody has declared yet is that rule doing its
    ///     job on a file whose author has not arrived. A warning is different — it says the emitted code is
    ///     wrong on its own terms, and that is this suite's business.
    /// </remarks>
    private static async Task<string[]> JustDummiesWarningsIn(string source, CancellationToken cancellationToken) {
        CSharpCompilation          compilation = EmittedCodeCompiler.Compile(source);
        ImmutableArray<Diagnostic> raised      = await compilation.WithAnalyzers(EmittedCodeCompiler.Analyzers)
                                                                  .GetAnalyzerDiagnosticsAsync(cancellationToken);

        return raised.Where(diagnostic => diagnostic.Id.StartsWith("JD", StringComparison.Ordinal))
                     .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
                     .Select(diagnostic => diagnostic.Id + " at " + diagnostic.Location.GetLineSpan())
                     .ToArray();
    }

}
