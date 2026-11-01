using System.Collections.Immutable;

using FsCheck;
using FsCheck.Fluent;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential property for <c>JD037</c>: a dependent draw written three ways over an explicit context —
///     inside an <c>As</c> lambda, inside a <c>Combine</c> lambda, and through <c>Then</c> — is compiled for the
///     analyzer and run under two ambient seeds by the run time.
/// </summary>
/// <remarks>
///     <para>
///         The run time is the oracle for the rule's reason: a chain whose value moves with the ambient seed while the
///         context's seed stays put does not replay from the seed it reports. The rule must report every draw written
///         inside a composition lambda, must leave the <c>Then</c> form silent, and that silent form must replay from
///         the context's seed alone — so the way out the message names is one the run time honours. A reported chain
///         whose inner generator comes from the same context replays; it is still reported, since the rule reads where
///         the draw sits, not where it draws from (ADR-0120).
///     </para>
/// </remarks>
public sealed class Jd037DifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 120;

    /// <summary>Draws per run: enough that a chain depending on the ambient seed shows it under any two seeds.</summary>
    private const int DrawsPerRun = 8;

    private enum Placement { As, Combine, Then }

    private enum InnerSource { StaticAny, SameContext }

    [Fact(DisplayName = "JD037 reports a draw written inside a composition lambda, and the Then form it points to replays from the context's seed alone.")]
    public void Jd037AgreesWithTheRunTime() {
        DrawInsideCompositionLambdaAnalyzer analyzer = new();

        Gen<(Placement Placement, InnerSource Inner, int Bound, int ContextSeed, int AmbientSeed)> cases =
            from placement in Gen.Elements(Placement.As, Placement.Combine, Placement.Then)
            from inner in Gen.Elements(InnerSource.StaticAny, InnerSource.SameContext)
            from bound in Gen.Choose(1_000, 1_000_000)
            from contextSeed in Gen.Choose(int.MinValue, int.MaxValue)
            from ambientSeed in Gen.Choose(int.MinValue, int.MaxValue - 1)
            select (placement, inner, bound, contextSeed, ambientSeed);

        Prop.ForAll(cases.ToArbitrary(),
                    @case => {
                        bool reported  = Reported(analyzer, @case.Placement, @case.Inner, @case.Bound);
                        bool dependent = !Run(@case.Placement, @case.Inner, @case.Bound, @case.ContextSeed, @case.AmbientSeed)
                                             .SequenceEqual(Run(@case.Placement, @case.Inner, @case.Bound, @case.ContextSeed, @case.AmbientSeed + 1));

                        bool expectedReport    = @case.Placement != Placement.Then;
                        bool expectedDependent = expectedReport && @case.Inner == InnerSource.StaticAny;
                        if (reported == expectedReport && dependent == expectedDependent) { return true.ToProperty(); }

                        return false.Label($"{@case}: JD037 reported={reported} (expected {expectedReport}); depends on the ambient seed={dependent} (expected {expectedDependent}).");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

    private static bool Reported(DiagnosticAnalyzer analyzer, Placement placement, InnerSource inner, int bound) {
        string innerFactory = inner == InnerSource.StaticAny ? "Any" : "any";
        string chain = placement switch {
            Placement.As      => $"any.Int32().Between(1, {bound}).As(n => n + {innerFactory}.Int32().Between(0, n).Generate())",
            Placement.Combine => $"Any.Combine(any.Int32().Between(1, {bound}), any.Int32(), (n, m) => n + {innerFactory}.Int32().Between(0, n).Generate())",
            _                 => $"any.Int32().Between(1, {bound}).Then(n => {innerFactory}.Int32().Between(0, n).As(k => n + k))"
        };
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static IAny<int> M() {
                    AnyContext any = Any.WithSeed(1);

                    return {{chain}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = AnalyzerHarness.Diagnose([analyzer], source);

        return diagnostics.Any(diagnostic => diagnostic.Id == "JD037");
    }

    /// <summary>The same chain the snippet spells, run: <see cref="DrawsPerRun" /> values under one ambient seed.</summary>
    private static List<int> Run(Placement placement, InnerSource inner, int bound, int contextSeed, int ambientSeed) {
        using (Any.UseSeed(ambientSeed)) {
            AnyContext any = Any.WithSeed(contextSeed);

            AnyInt32 Inner(int n) {
                return (inner == InnerSource.StaticAny ? Any.Int32() : any.Int32()).Between(0, n);
            }

            IAny<int> chain = placement switch {
                Placement.As      => any.Int32().Between(1, bound).As(n => n + Inner(n).Generate()),
                Placement.Combine => Any.Combine(any.Int32().Between(1, bound), any.Int32(), (n, _) => n + Inner(n).Generate()),
                _                 => any.Int32().Between(1, bound).Then(n => Inner(n).As(k => n + k))
            };

            return Enumerable.Range(0, DrawsPerRun).Select(_ => chain.Generate()).ToList();
        }
    }

}
