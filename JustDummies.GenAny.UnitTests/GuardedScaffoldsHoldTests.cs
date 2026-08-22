using System.Collections.Generic;
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
///     Three oracles over <see cref="GuardCorpus" />, and each answers a question the others cannot.
/// </summary>
/// <remarks>
///     The compiler says whether the file binds. The library's own analyzers say whether the chain inside it is
///     one the library would accept — and the rule the maintainer set is that the tool never emits code they
///     report at warning level or above, since a scaffold arrives before its author and cannot answer for
///     itself. Neither of them says whether the generator produces a value: a chain can be legal, declarable,
///     silent under every rule, and still say something other than what the guards said. Only the domain's own
///     constructor decides that, and drawing is how it gets asked.
///     <para>
///         Informational rules are a third case, and are neither ignored nor obeyed. A scaffold knows what it
///         meant to write, so an <c>Info</c> on emitted output is a review of that intention rather than a
///         verdict on it: <see cref="Assumed" /> lists the ones the engine stands behind, and anything else
///         fails until someone decides which it is.
///     </para>
/// </remarks>
public sealed class GuardedScaffoldsHoldTests {

    /// <summary>How many values each shape is asked for.</summary>
    /// <remarks>
    ///     Enough for a constraint the engine dropped to show: the enum shape rejects roughly one draw in
    ///     three, so a run this long missing it is not a possibility worth arithmetic. Everything else in the
    ///     corpus fails on the first draw or at construction.
    /// </remarks>
    private const int Draws = 200;

    /// <summary>
    ///     The informational rules the engine stands behind on its own output.
    /// </summary>
    /// <remarks>
    ///     One entry, and it earns its place: <c>JD030</c> reports a string whose length nobody has declared,
    ///     and nobody has — the domain says nothing about it and the engine will not invent a bound to quieten
    ///     a rule. Every other <c>Info</c> reports something the engine chose, and a choice it cannot defend is
    ///     one it should not have made: <c>JD031</c> means it wrote two bounds where it meant a range, and
    ///     <c>JD024</c> means it wrote a constraint that narrows nothing.
    /// </remarks>
    private static readonly ImmutableHashSet<string> Assumed = ImmutableHashSet.Create("JD030");

    public static TheoryData<string> Corpus {
        get {
            TheoryData<string> shapes = [];

            foreach (string name in GuardCorpus.Names()) { shapes.Add(name); }

            return shapes;
        }
    }

    [Theory(DisplayName = "A guarded scaffold compiles in the developer's project.")]
    [MemberData(nameof(Corpus))]
    public void AGuardedScaffoldCompiles(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        Check.That(EmittedCodeCompiler.ErrorsIn(CompiledFor(shape))).IsEmpty();
    }

    [Theory(DisplayName = "A guarded scaffold raises no rule of the library's own, above information.")]
    [MemberData(nameof(Corpus))]
    public async Task AGuardedScaffoldRaisesNoRule(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        IReadOnlyList<Diagnostic> raised = await JustDummiesRulesOn(CompiledFor(shape),
                                                                   TestContext.Current.CancellationToken);

        Check.That(raised.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).Select(Render)).IsEmpty();
        Check.That(raised.Where(diagnostic => !Assumed.Contains(diagnostic.Id)).Select(Render)).IsEmpty();
    }

    [Theory(DisplayName = "A guarded scaffold draws values its own domain accepts.")]
    [MemberData(nameof(Corpus))]
    public void AGuardedScaffoldDraws(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        string? failure = EmittedAssembly.DrawFrom(CompiledFor(shape), $"Shop.Domain.Any{shape.Target}", Draws);

        Check.WithCustomMessage($"Any{shape.Target}: {failure}").That(failure).IsNull();
    }

    /// <summary>The shape scaffolded, and its generator compiled beside the domain it names.</summary>
    private static CSharpCompilation CompiledFor(GuardCorpus.GuardedShape shape) {
        ScaffoldOutcome outcome = Subject.ScaffoldByName(shape.Target, shape.Domain);

        Check.WithCustomMessage($"{shape.Target} did not scaffold: {outcome.Status}.")
             .That(outcome.Succeeded)
             .IsTrue();

        return EmittedCodeCompiler.CompileWith(outcome.File!.SourceText, shape.Domain);
    }

    private static async Task<IReadOnlyList<Diagnostic>> JustDummiesRulesOn(CSharpCompilation compilation,
                                                                           CancellationToken cancellationToken) {
        ImmutableArray<Diagnostic> raised = await compilation.WithAnalyzers(EmittedCodeCompiler.Analyzers)
                                                             .GetAnalyzerDiagnosticsAsync(cancellationToken);

        return [.. raised.Where(diagnostic => diagnostic.Id.StartsWith("JD", System.StringComparison.Ordinal))];
    }

    private static string Render(Diagnostic diagnostic) {
        return $"{diagnostic.Id} [{diagnostic.Severity}] {diagnostic.GetMessage()}";
    }

}
