using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A generated numeric chain holding a value set: constraints declared around one <c>OneOf</c>, written once as C#
///     for the analyzer and executed by the run time, which is the oracle.
/// </summary>
/// <remarks>
///     The oracle asks the question JD029 answers, one value at a time: the same chain with that value as its whole pool
///     either draws or is refused. That is what "this value never draws" means, read off the run time rather than off
///     a restatement of its predicates.
/// </remarks>
internal sealed class PoolChain {

    /// <summary>What the run time says when a constraint the generator takes once is declared again differently.</summary>
    private const string AlreadyDefined = "is already defined";

    public PoolChain(PoolFamily family, IReadOnlyList<PoolElement> pool, IReadOnlyList<DeclarationCall> constraints, int poolPosition) {
        Family       = family;
        Pool         = pool;
        Constraints  = constraints;
        PoolPosition = Math.Min(poolPosition, constraints.Count);
    }

    public PoolFamily Family { get; }

    public IReadOnlyList<PoolElement> Pool { get; }

    public IReadOnlyList<DeclarationCall> Constraints { get; }

    /// <summary>How many constraints are declared before the value set.</summary>
    public int PoolPosition { get; }

    public string Expression => Family.Factory
                              + string.Concat(Constraints.Take(PoolPosition).Select(call => "." + call.Source))
                              + ".OneOf(" + string.Join(", ", Pool.Select(element => element.Source)) + ")"
                              + string.Concat(Constraints.Skip(PoolPosition).Select(call => "." + call.Source))
                              + ".Generate()";

    /// <summary>
    ///     Whether the run time refuses the chain for a reason that is not about any pooled value: a constraint declared
    ///     twice, which JD034 claims, or an argument rejected on its own, which JD014 does. Such a chain says nothing
    ///     about which values draw.
    /// </summary>
    public bool IsRefusedForAnotherReason() {
        try {
            Run(Pool);

            return false;
        } catch (ConflictingAnyConstraintException conflict) {
            return conflict.Message.Contains(AlreadyDefined, StringComparison.Ordinal);
        } catch (ArgumentException) {
            return true;
        }
    }

    /// <summary>Whether <paramref name="element" /> never draws: the chain with it as its only pooled value is refused.</summary>
    public bool RuntimeRefuses(PoolElement element) {
        try {
            Run([element]);

            return false;
        } catch (ConflictingAnyConstraintException) {
            return true;
        }
    }

    /// <summary>
    ///     Whether the one constraint the diagnostic blames refuses <paramref name="element" /> on its own: a value
    ///     set holding only that value, beside only that constraint, is refused.
    /// </summary>
    public bool RuntimeRefusesUnder(PoolElement element, DeclarationCall constraint) {
        try {
            constraint.ApplyTo(Family.OneOf(Family.Create(Any.WithSeed(0)), [element]));

            return false;
        } catch (ConflictingAnyConstraintException) {
            return true;
        }
    }

    /// <summary>The JD029 reports on the chain: the pooled element each one sits on, and the message it carries.</summary>
    public IReadOnlyList<(string Element, string Message)> ReportsOf(DiagnosticAnalyzer analyzer, string id) {
        string source = $$"""
            using System;

            using JustDummies;

            public static class Sample {
                public static object M() {
                    return {{Expression}};
                }
            }
            """;

        return AnalyzerHarness.Diagnose([analyzer], source)
                              .Where(diagnostic => diagnostic.Id == id)
                              .Select(diagnostic => (source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length), diagnostic.GetMessage()))
                              .ToList();
    }

    /// <summary>The chains one call or one pooled value shorter, so FsCheck reports the smallest that still disagrees.</summary>
    public IEnumerable<PoolChain> Shrink() {
        for (int call = 0; call < Constraints.Count; call++) {
            int removed = call;

            yield return new PoolChain(Family, Pool, Constraints.Where((_, index) => index != removed).ToList(), removed < PoolPosition ? PoolPosition - 1 : PoolPosition);
        }

        if (Pool.Count < 2) { yield break; }

        for (int element = 0; element < Pool.Count; element++) {
            int removed = element;

            yield return new PoolChain(Family, Pool.Where((_, index) => index != removed).ToList(), Constraints, PoolPosition);
        }
    }

    /// <inheritdoc />
    public override string ToString() {
        return Expression;
    }

    private void Run(IReadOnlyList<PoolElement> pool) {
        object generator = Family.Create(Any.WithSeed(0));
        for (int call = 0; call <= Constraints.Count; call++) {
            if (call == PoolPosition) { generator = Family.OneOf(generator, pool); }
            if (call < Constraints.Count) { generator = Constraints[call].ApplyTo(generator); }
        }

        Family.Generate(generator);
    }

}
