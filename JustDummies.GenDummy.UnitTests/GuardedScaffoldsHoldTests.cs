using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

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
///         verdict on it: <see cref="EmittedCodeCompiler.Assumed" /> lists the ones the engine stands
///         behind, and anything else fails until someone decides which it is.
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

    public static TheoryData<string> Satisfiable => Rows(GuardCorpus.SatisfiableNames());

    public static TheoryData<string> BeyondTheEngine => Rows(GuardCorpus.BeyondTheEngineNames());

    public static TheoryData<string> RequiresVerification => Rows(GuardCorpus.RequiresVerificationNames());

    private static TheoryData<string> Rows(IEnumerable<string> names) {
        TheoryData<string> shapes = [];

        foreach (string name in names) { shapes.Add(name); }

        return shapes;
    }

    [Theory(DisplayName = "A guarded scaffold compiles in the developer's project.")]
    [MemberData(nameof(Satisfiable))]
    public void AGuardedScaffoldCompiles(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        Check.That(EmittedCodeCompiler.ErrorsIn(CompiledFor(shape))).IsEmpty();
    }

    [Theory(DisplayName = "A guarded scaffold raises no rule of the library's own, above information.")]
    [MemberData(nameof(Satisfiable))]
    public async Task AGuardedScaffoldRaisesNoRule(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        IReadOnlyList<Diagnostic> raised = await JustDummiesRulesOn(CompiledFor(shape),
                                                                   TestContext.Current.CancellationToken);

        Check.That(raised.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).Select(Render)).IsEmpty();
        Check.That(raised.Where(diagnostic => !EmittedCodeCompiler.Assumed.Contains(diagnostic.Id)).Select(Render)).IsEmpty();
    }

    [Theory(DisplayName = "A guarded scaffold draws values its own domain accepts.")]
    [MemberData(nameof(Satisfiable))]
    public void AGuardedScaffoldDraws(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        string? failure = EmittedAssembly.DrawFrom(CompiledFor(shape), $"Shop.Domain.Dummy{shape.Target}", Draws);

        Check.WithCustomMessage($"Dummy{shape.Target}: {failure}").That(failure).IsNull();
    }

    /// <summary>
    ///     A domain no generator can satisfy is refused cleanly, and the refusal is on the record — as a
    ///     working generator the engine vouches for, or as a factory that blocks compilation where it cannot.
    /// </summary>
    /// <remarks>
    ///     A drop the engine fully understands — <c>ConstraintUnavailable</c>: a guard read, understood, and
    ///     precisely known to have no member on this generator — still has to construct: a generator that
    ///     throws the moment it is built is unusable and no <c>With…</c> call rescues it. A drop it cannot
    ///     vouch for — <c>UnreadGuards</c>, which now also covers a size past what the library will produce
    ///     or a count past what the element row can draw — is worse than merely throwing on every draw: it
    ///     throws on <b>every</b> one, which is exactly the shape the factory's own doubt mechanism (§5.5)
    ///     exists for, so it blocks compilation there instead, with the engine's best attempt kept underneath
    ///     as what to verify or replace. Either way the recap still has to say so, because the alternative is
    ///     a file reporting every parameter inferred over an invariant nobody honoured.
    /// </remarks>
    [Theory(DisplayName = "A domain beyond the engine is refused and reported — constructed where the engine vouches for the drop, blocked where it does not.")]
    [MemberData(nameof(BeyondTheEngine))]
    public void ADomainBeyondTheEngineIsRefusedAndReported(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        ScaffoldOutcome outcome = Scaffolded(shape);

        Check.WithCustomMessage($"Dummy{shape.Target} honoured nothing and said nothing.")
             .That(outcome.Plan!.Parameters.Any(parameter => parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)
                                                          || parameter.Provenance.HasFlag(Provenance.UnreadGuards)
                                                          || parameter.Provenance.HasFlag(Provenance.ConstraintUnavailable)))
             .IsTrue();

        if (outcome.Plan.Parameters.Any(parameter => parameter.RequiresVerification)) {
            Check.WithCustomMessage($"Dummy{shape.Target} compiled despite a drop the engine cannot vouch for.")
                 .That(EmittedCodeCompiler.ErrorsIn(Compiled(outcome, shape)))
                 .Not.IsEmpty();
        } else {
            string? failure = EmittedAssembly.DrawFrom(Compiled(outcome, shape), $"Shop.Domain.Dummy{shape.Target}", count: 0);

            Check.WithCustomMessage($"Dummy{shape.Target}: {failure}").That(failure).IsNull();
        }
    }

    /// <summary>
    ///     A guard the engine could not vouch for blocks compilation, and what it kept underneath is not a
    ///     placeholder — deleting the blocking line, as §5.6 tells the developer to, leaves a chain that
    ///     compiles and raises no rule of the library's own. It does not leave a chain proven correct: this
    ///     shape's own base recipe still fails against the real constructor, which is the whole reason blocking
    ///     compilation was the right call rather than a formality.
    /// </summary>
    /// <remarks>
    ///     The other theories above prove a golden file's <i>shape</i>; this one proves the claim behind
    ///     it — and the claim was never "dum's guess is correct". It is "dum's guess is real, not a stub", and
    ///     that a generator this unverified, shipped as ordinary code, is exactly the silent failure ADR-0083
    ///     exists to stop.
    /// </remarks>
    [Theory(DisplayName = "A guard the engine cannot vouch for blocks compilation, over a base that is real but still unverified.")]
    [MemberData(nameof(RequiresVerification))]
    public async Task ARequiresVerificationBlocksCompilationOverAnUnverifiedBase(string shapeName) {
        GuardCorpus.GuardedShape shape = GuardCorpus.Named(shapeName);

        if (shape.Defect is not null) { Assert.Skip($"{shape.Defect} — the engine does not hold this shape yet."); }

        ScaffoldOutcome outcome = Scaffolded(shape);

        Check.WithCustomMessage($"Dummy{shape.Target} was not marked as requiring verification.")
             .That(outcome.Plan!.Parameters.Any(parameter => parameter.RequiresVerification))
             .IsTrue();
        Check.WithCustomMessage($"Dummy{shape.Target} compiled despite a drop the engine cannot vouch for.")
             .That(EmittedCodeCompiler.ErrorsIn(Compiled(outcome, shape)))
             .Not.IsEmpty();

        CSharpCompilation resolved = EmittedCodeCompiler.CompileWith(VerifySentinel.StrippedFrom(outcome.File!.SourceText), shape.Domain);

        Check.WithCustomMessage($"Dummy{shape.Target}, with the blocking line deleted, still does not compile.")
             .That(EmittedCodeCompiler.ErrorsIn(resolved))
             .IsEmpty();

        IReadOnlyList<Diagnostic> raised = await JustDummiesRulesOn(resolved, TestContext.Current.CancellationToken);

        Check.That(raised.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).Select(Render)).IsEmpty();

        // Pinned rather than merely tolerated: this shape is in the corpus BECAUSE its base recipe throws once
        // resolved, and a green run here silently, with nothing catching it, is this test proving the opposite
        // of what it is for.
        string? failure = EmittedAssembly.DrawFrom(resolved, $"Shop.Domain.Dummy{shape.Target}", Draws);

        Check.WithCustomMessage($"Dummy{shape.Target} drew {Draws} values its own domain accepted — the unverified base "
                              + "turned out sound, which this shape was chosen to show is not guaranteed.")
             .That(failure)
             .Not.IsNull();
    }

    /// <summary>The shape scaffolded, and its generator compiled beside the domain it names.</summary>
    private static CSharpCompilation CompiledFor(GuardCorpus.GuardedShape shape) {
        return Compiled(Scaffolded(shape), shape);
    }

    private static ScaffoldOutcome Scaffolded(GuardCorpus.GuardedShape shape) {
        ScaffoldOutcome outcome = Subject.ScaffoldByName(shape.Target, shape.Domain);

        Check.WithCustomMessage($"{shape.Target} did not scaffold: {outcome.Status}.")
             .That(outcome.Succeeded)
             .IsTrue();

        return outcome;
    }

    private static CSharpCompilation Compiled(ScaffoldOutcome outcome, GuardCorpus.GuardedShape shape) {
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
