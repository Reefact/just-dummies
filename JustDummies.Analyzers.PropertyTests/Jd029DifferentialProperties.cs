using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential property for <c>JD029</c> on the numeric families: random chains holding a value set are
///     compiled for the analyzer and executed by the run time, and the rule must report a pooled value exactly once
///     when the run time never draws it, blaming a constraint that refuses it on its own.
/// </summary>
/// <remarks>
///     <para>
///         The rule's remarks call its claim one-sided: a value it reports is refused, while a constraint whose argument
///         is not a compile-time constant is skipped and leaves a refused value unreported. The generated chains write
///         every argument as a constant from the switch the rule reads, so here both halves are owed — a report on a
///         value that draws is a false accusation, and a refused value left silent is a spelling the rule fails to
///         fold.
///     </para>
///     <para>
///         The diagnostic names one constraint. That one must refuse the value by itself, checked by the run time on a
///         chain holding only the value and that constraint: a message blaming a constraint that lets the value through
///         sends the reader to the wrong call.
///     </para>
/// </remarks>
public sealed class Jd029DifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 300;

    private const string Jd029 = "JD029";

    /// <summary>The message's fixed frame, around the constraint it names.</summary>
    private const string Prefix = "This value never draws: ";

    private const string Suffix = " refuses it";

    [Fact(DisplayName = "JD029 reports a pooled number exactly once when the run time never draws it, and blames a constraint that refuses it.")]
    public void Jd029AgreesWithTheRunTime() {
        PooledValueNeverDrawsAnalyzer analyzer = new();

        Prop.ForAll(PoolChains.Arbitrary(),
                    chain => {
                        if (chain.IsRefusedForAnotherReason()) { return true.ToProperty(); }

                        IReadOnlyList<(string Element, string Message)> reports = chain.ReportsOf(analyzer, Jd029);
                        string? disagreement = MiscountedValue(chain, reports) ?? MisplacedBlame(chain, reports);

                        return disagreement is null ? true.ToProperty() : false.Label(disagreement);
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

    /// <summary>
    ///     The first pooled value whose report count is wrong. Counted, not merely detected: a refused value owes
    ///     exactly one report, so a second one on the same value is as much a disagreement as a missing one.
    /// </summary>
    private static string? MiscountedValue(PoolChain chain, IReadOnlyList<(string Element, string Message)> reports) {
        foreach (PoolElement element in chain.Pool) {
            int expected = chain.RuntimeRefuses(element) ? 1 : 0;
            int reported = reports.Count(report => report.Element == element.Source);
            if (reported == expected) { continue; }

            string outcome = expected == 1 ? "the run time never draws it" : "the run time draws it";

            return $"{chain}: {element}: {outcome} and JD029 reports it {reported} time(s).";
        }

        return null;
    }

    /// <summary>The first report that sits off the pool, or blames a constraint that does not refuse its value alone.</summary>
    private static string? MisplacedBlame(PoolChain chain, IReadOnlyList<(string Element, string Message)> reports) {
        foreach ((string elementSource, string message) in reports) {
            PoolElement? element = chain.Pool.FirstOrDefault(candidate => candidate.Source == elementSource);
            if (element is null) { return $"{chain}: JD029 reports {elementSource}, which is not a pooled value."; }

            string           blamed = message.Substring(Prefix.Length, message.Length - Prefix.Length - Suffix.Length);
            DeclarationCall? call   = chain.Constraints.FirstOrDefault(constraint => constraint.Source == blamed);

            if (call is null) { return $"{chain}: {element}: JD029 blames {blamed}, which the chain does not declare."; }
            if (!chain.RuntimeRefusesUnder(element, call)) { return $"{chain}: {element}: JD029 blames {blamed}, which lets it through."; }
        }

        return null;
    }

}
