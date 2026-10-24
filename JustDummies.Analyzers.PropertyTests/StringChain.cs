using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A generated <c>Any.String()</c> chain, written once as C# for the analyzer and executed once by the run time,
///     which is the oracle: the chain admits no value exactly when the run time refuses it.
/// </summary>
internal sealed class StringChain {

    public StringChain(IReadOnlyList<StringChainCall> calls) {
        Calls = calls;
    }

    public IReadOnlyList<StringChainCall> Calls { get; }

    /// <summary>The chain as a whole expression, the shape the rules read: one expression, every constraint in it.</summary>
    public string Expression => "Any.String()" + string.Concat(Calls.Select(call => "." + call.Source)) + ".Generate()";

    /// <summary>
    ///     Whether the run time refuses the chain — at a call, or at the draw, where a <c>NotBlank()</c> with nowhere to
    ///     put its character is answered. The draw is deterministic here: the chains carry no exclusion a shaped string
    ///     would meet by a redraw.
    /// </summary>
    public bool RuntimeRefuses() {
        try {
            AnyString generator = Calls.Aggregate(Any.WithSeed(0).String(), (current, call) => call.ApplyTo(current));
            _ = generator.Generate();

            return false;
        } catch (ConflictingAnyConstraintException) {
            return true;
        }
    }

    /// <summary>Whether <paramref name="analyzer" /> reports <paramref name="id" /> anywhere on the chain.</summary>
    public bool IsReportedBy(DiagnosticAnalyzer analyzer, string id) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{Expression}};
                }
            }
            """;

        return AnalyzerHarness.Diagnose(analyzer, source).Any(diagnostic => diagnostic.Id == id);
    }

    /// <summary>
    ///     The chains one call shorter, so FsCheck reports the smallest chain that still disagrees. Dropping the value
    ///     set drops the exclusions with it: an exclusion is generated only beside a value set.
    /// </summary>
    public IEnumerable<StringChain> Shrink() {
        for (int call = 0; call < Calls.Count; call++) {
            int removed = call;

            yield return Calls[removed].Slot == StringChainSlot.ValueSet
                             ? new StringChain(Calls.Where(kept => kept.Slot is not (StringChainSlot.ValueSet or StringChainSlot.Exclusion)).ToList())
                             : new StringChain(Calls.Where((_, index) => index != removed).ToList());
        }
    }

    /// <inheritdoc />
    public override string ToString() {
        return Expression;
    }

}
