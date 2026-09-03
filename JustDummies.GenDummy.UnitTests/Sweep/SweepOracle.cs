using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.GenDummy.UnitTests.Sweep;

/// <summary>
///     Seven rules over one generated shape, in the order that keeps the sweep honest about its own bugs.
/// </summary>
/// <remarks>
///     None of them predicts what the engine will do. A bench that computed the expected verdict from the
///     axes would encode today's behaviour and become a change detector wearing a defect detector's clothes;
///     one that classified by the text of a compiler message would be reading prose. These are claims that
///     hold whatever the engine does — and rule 0 is the claim that holds about the bench itself.
/// </remarks>
internal static class SweepOracle {

    /// <summary>The namespace every generated domain, and so every emitted generator, is written into.</summary>
    private const string Namespace = "Shop.Domain";

    /// <summary>
    ///     Below this the "must not refuse" half of rule 6 declines to speak.
    /// </summary>
    /// <remarks>
    ///     Drawing N distinct values out of a pool of exactly N takes a redraw loop that gets luckier and
    ///     luckier to converge, and the library bounds its redraws and fails rather than looping (ADR-0004,
    ///     ADR-0012, ADR-0027). A refusal near the ceiling is therefore a documented outcome, not a defect,
    ///     and a rule that called it one would be reporting the library's own honesty. Four times the demand
    ///     is comfortable room; below it the sweep records the refusal and judges nothing.
    /// </remarks>
    private const int ComfortableHeadroom = 4;

    /// <summary>
    ///     Judges one shape, and says what it concluded and why.
    /// </summary>
    /// <remarks>
    ///     A finding an open entry of <see cref="SweepDefects" /> accounts for comes back marked as that
    ///     defect rather than as a fresh one — the bench stays green while a known defect stands, and says
    ///     which. Nothing else is softened: a finding no entry claims stays a finding.
    /// </remarks>
    internal static async Task<SweepOutcome> JudgeAsync(SweepShape shape, int draws, CancellationToken cancellationToken) {
        SweepOutcome outcome = await AgainstTheRulesAsync(shape, draws, cancellationToken);

        if (outcome.Verdict != SweepVerdict.Finding) { return outcome; }

        SweepDefects.SweepDefect? known = SweepDefects.Claiming(shape, outcome);

        return known is null
                   ? outcome
                   : new SweepOutcome(outcome.Name, outcome.Family, outcome.Status, outcome.Provenance,
                                      outcome.Compiles, outcome.Rules, outcome.Draw, SweepVerdict.KnownDefect, known.Id);
    }

    private static async Task<SweepOutcome> AgainstTheRulesAsync(SweepShape shape, int draws, CancellationToken cancellationToken) {
        // ---- Rule 0. The sweep's own input, before anything is asked of the engine. ----
        IReadOnlyList<string> domainErrors = EmittedCodeCompiler.ErrorsIn(EmittedCodeCompiler.CompileWith(string.Empty, shape.Domain));

        if (domainErrors.Count > 0) {
            return Bug(shape, "the generated domain does not compile on its own: " + string.Join("; ", domainErrors.Take(3)));
        }

        // ---- Rule 1. The engine answers, for the target and for every generator the target leans on. ----
        List<string>     emitted    = [];
        ScaffoldOutcome? scaffolded = null;

        foreach (string type in Wanted(shape)) {
            ScaffoldOutcome one = Subject.ScaffoldByName(type, shape.Domain);

            if (!one.Succeeded) {
                return Finding(shape, one.Status.ToString(), provenance: "-", compiles: "-",
                               type == shape.Target
                                   ? $"the engine refused a domain that compiles: {one.Status}."
                                   : $"the engine refused {type}, which the target's own file names: {one.Status}.");
            }

            scaffolded ??= one;
            emitted.Add(one.File!.SourceText);
        }

        string status     = scaffolded!.Status.ToString();
        string provenance = Provenances(scaffolded);

        CSharpCompilation         compiled = EmittedCodeCompiler.CompileAllWith(emitted, shape.Domain);
        IReadOnlyList<Diagnostic> errors   = Errors(compiled, cancellationToken);

        // ---- Rule 2. What does not compile, does not compile ON a sentinel. ----
        IReadOnlyList<Diagnostic> elsewhere = [.. errors.Where(error => !VerifySentinel.OnALine(LineOf(error)))];

        if (elsewhere.Count > 0) {
            return Finding(shape, status, provenance, Rendered(errors),
                           "the emitted file does not compile, away from any sentinel: " + Rendered(elsewhere));
        }

        if (errors.Count > 0) { return await BlockedAsync(shape, emitted, status, provenance, errors, cancellationToken); }

        // ---- Rule 4. The library's own rules, on the library's own output. ----
        string? raised = await RulesRaisedOn(compiled, cancellationToken);

        if (raised is not null) {
            return Finding(shape, status, provenance, "ok", $"the emitted file raises {raised}.", rules: raised);
        }

        // ---- Rules 5 and 6. What the generator actually produces. ----
        EmittedAssembly.DrawFailure? failure = EmittedAssembly.Attempt(compiled, $"{Namespace}.Dummy{shape.Target}", draws);

        return Drawn(shape, status, provenance, failure);
    }

    /// <summary>
    ///     A shape the engine will not ship silently: which sentinel it wrote decides what else is owed.
    /// </summary>
    private static async Task<SweepOutcome> BlockedAsync(SweepShape shape,
                                                         IReadOnlyList<string> emitted,
                                                         string status,
                                                         string provenance,
                                                         IReadOnlyList<Diagnostic> errors,
                                                         CancellationToken cancellationToken) {
        string compiles = Rendered(errors);

        // An open parameter has no base underneath, so there is nothing for rule 3 to resolve to.
        bool unresolved = errors.Any(error => LineOf(error).Contains(VerifySentinel.Supply, System.StringComparison.Ordinal));

        if (unresolved) {
            return new SweepOutcome(shape.Name, shape.Family, status, provenance, compiles, "-", "-", SweepVerdict.Unresolved);
        }

        // ---- Rule 3. Delete the blocking line, as §5.6 tells the developer to, and it compiles. ----
        CSharpCompilation resolved = EmittedCodeCompiler.CompileAllWith(emitted.Select(VerifySentinel.StrippedFrom), shape.Domain);
        IReadOnlyList<string> remaining = EmittedCodeCompiler.ErrorsIn(resolved);

        if (remaining.Count > 0) {
            return Finding(shape, status, provenance, compiles,
                           "the sentinel deleted, what the engine kept underneath does not compile: "
                         + string.Join("; ", remaining.Take(3)));
        }

        // ---- Rule 4, over the resolved file: what a developer would actually be left holding. ----
        string? raised = await RulesRaisedOn(resolved, cancellationToken);

        if (raised is not null) {
            return Finding(shape, status, provenance, compiles,
                           $"the sentinel deleted, the emitted file raises {raised}.", rules: raised);
        }

        return new SweepOutcome(shape.Name, shape.Family, status, provenance, compiles, "ok", "-",
                                SweepVerdict.BlockedForVerification);
    }

    /// <summary>
    ///     Rule 5 — a draw either produces a value or is refused in the first class — and rule 6 over it.
    /// </summary>
    private static SweepOutcome Drawn(SweepShape shape, string status, string provenance, EmittedAssembly.DrawFailure? failure) {
        bool refused = failure?.Kind == nameof(DummyGenerationException);

        if (failure is not null && !refused) {
            // The specification declares some of these in advance (§9), and a bench that called one a defect
            // would be reporting the specification back at itself. It still gets a row, and a count.
            if (shape.Residue is not null) {
                return new SweepOutcome(shape.Name, shape.Family, status, provenance, "ok", "ok", failure.ToString(),
                                        SweepVerdict.KnownResidue, shape.Residue);
            }

            return Finding(shape, status, provenance, "ok",
                           "the generator produced a value its own domain rejects, or failed outside the library's "
                         + "own refusal: " + failure,
                           rules: "ok", draw: failure.ToString());
        }

        // ---- Rule 6, and only where the domain's own text fixes the demand and the source fixes the pool. ----
        string? mismatch = Distinctness(shape, refused);

        if (mismatch is not null) {
            return Finding(shape, status, provenance, "ok", mismatch, rules: "ok", draw: failure?.ToString() ?? "ok");
        }

        return refused
                   ? new SweepOutcome(shape.Name, shape.Family, status, provenance, "ok", "ok", failure!.ToString(),
                                      SweepVerdict.RefusedByDesign)
                   : new SweepOutcome(shape.Name, shape.Family, status, provenance, "ok", "ok", "ok", SweepVerdict.Held);
    }

    /// <summary>
    ///     Whether the refusal, or the absence of one, contradicts how many distinct values the source holds.
    /// </summary>
    /// <remarks>
    ///     Both directions are claims about the domain's own text, not about the library: a set of five
    ///     distinct <c>Slot</c> cannot exist when <c>Slot</c> declares three members, and a set of two
    ///     <c>Wide</c> plainly can when it declares thirty-two. Between the two the answer depends on how the
    ///     library draws, and the sweep says nothing rather than guessing — which is the whole of what
    ///     <see cref="SweepAxes.Unknown" /> and <see cref="ComfortableHeadroom" /> are for.
    /// </remarks>
    private static string? Distinctness(SweepShape shape, bool refused) {
        if (shape.DistinctDemand <= 0 || shape.DistinctCapacity == SweepAxes.Unknown) { return null; }

        int demand   = shape.DistinctDemand;
        int capacity = shape.DistinctCapacity;

        if (demand > capacity && !refused) {
            return $"the domain demands {demand} distinct values of a type that declares {capacity}, "
                 + "and the generator produced one anyway.";
        }

        bool mustHold = demand <= 1 || (capacity >= ComfortableHeadroom * 2 && demand * ComfortableHeadroom <= capacity);

        if (mustHold && refused) {
            return $"the domain demands {demand} distinct values of a type that declares {capacity}, "
                 + "and the generator refused.";
        }

        return null;
    }

    private static async Task<string?> RulesRaisedOn(CSharpCompilation compilation, CancellationToken cancellationToken) {
        ImmutableArray<Diagnostic> raised = await compilation.WithAnalyzers(EmittedCodeCompiler.Analyzers)
                                                             .GetAnalyzerDiagnosticsAsync(cancellationToken);

        IReadOnlyList<Diagnostic> unwanted = [.. raised.Where(diagnostic => diagnostic.Id.StartsWith("JD", System.StringComparison.Ordinal))
                                                       .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning
                                                                         || !EmittedCodeCompiler.Assumed.Contains(diagnostic.Id))];

        return unwanted.Count == 0
                   ? null
                   : string.Join("; ", unwanted.Select(diagnostic => $"{diagnostic.Id} [{diagnostic.Severity}] {diagnostic.GetMessage()}"));
    }

    /// <summary>The target, then every generator its own file names — the set a developer ends up holding.</summary>
    private static IEnumerable<string> Wanted(SweepShape shape) {
        yield return shape.Target;

        foreach (string companion in shape.Companions) { yield return companion; }
    }

    private static IReadOnlyList<Diagnostic> Errors(CSharpCompilation compilation, CancellationToken cancellationToken) {
        return [.. compilation.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }

    /// <summary>
    ///     The source line the error sits on — which is how "on a sentinel" is decided.
    /// </summary>
    /// <remarks>
    ///     By location rather than by the compiler's wording: a message can be reworded between two versions
    ///     of Roslyn, and a bench that matched on prose would start calling a whole family of shapes findings
    ///     the day it was.
    /// </remarks>
    private static string LineOf(Diagnostic diagnostic) {
        SyntaxTree? tree = diagnostic.Location.SourceTree;

        if (tree is null) { return string.Empty; }

        int line = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
        Microsoft.CodeAnalysis.Text.TextLineCollection lines = tree.GetText().Lines;

        return line >= 0 && line < lines.Count ? lines[line].ToString() : string.Empty;
    }

    private static string Provenances(ScaffoldOutcome scaffolded) {
        return string.Join(", ", scaffolded.Plan!.Parameters.Select(parameter => $"{parameter.Name}={parameter.Provenance}"));
    }

    private static string Rendered(IReadOnlyList<Diagnostic> errors) {
        return string.Join("; ", errors.Take(3).Select(error => error.Id + ": " + error.GetMessage()));
    }

    private static SweepOutcome Bug(SweepShape shape, string reason) {
        return new SweepOutcome(shape.Name, shape.Family, "-", "-", "-", "-", "-", SweepVerdict.SweepBug, reason);
    }

    private static SweepOutcome Finding(SweepShape shape,
                                        string status,
                                        string provenance,
                                        string compiles,
                                        string reason,
                                        string rules = "-",
                                        string draw = "-") {
        return new SweepOutcome(shape.Name, shape.Family, status, provenance, compiles, rules, draw,
                                SweepVerdict.Finding, reason);
    }

}
