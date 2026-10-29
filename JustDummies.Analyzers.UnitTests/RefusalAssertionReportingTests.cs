using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     Every rule that once stood down on a test asserting a refusal reports it like any other code (ADR-0112), in each
///     shape the removed exemption read or was argued about: the chain as the whole body of a parameterless lambda, the
///     same in a lambda that takes a parameter, and the chain going on past the offending call.
/// </summary>
/// <remarks>
///     Each chain is the one the rule's own negative test used to pin as silent, so a rule regaining an exemption fails
///     here by name. A chain the rule does not follow past <c>Generate()</c> anywhere — not only in a test — has no
///     third shape: that is the rule's scope, not an exemption.
/// </remarks>
public class RefusalAssertionReportingTests {

    private sealed record Case(Func<DiagnosticAnalyzer> Analyzer, string Chain, bool Discarded, string? Continuation);

    private static readonly IReadOnlyDictionary<string, Case> Cases = new Dictionary<string, Case> {
        ["JD004"] = new(() => new DiscardedSeedingResultAnalyzer(), "Any.UseSeed(seed, null!)", Discarded: true, Continuation: null),
        ["JD006"] = new(() => new DiscardedGeneratorResultAnalyzer(), "Any.String().WithLength(3).StartingWith(\"ORD-\")", Discarded: true, Continuation: null),
        ["JD011"] = new(() => new GeneratorWhereValueExpectedAnalyzer(), "Any.String().WithLength(3).StartingWith(\"ORD-\")", Discarded: false, Continuation: null),
        ["JD014"] = new(() => new RejectedConstantArgumentAnalyzer(), "Any.Int32().Between(10, 5)", Discarded: false, Continuation: ".Generate()"),
        ["JD015"] = new(() => new StringConstraintsAdmitNoValueAnalyzer(), "Any.String().WithLength(3).StartingWith(\"ORD-\")", Discarded: false, Continuation: ".Generate()"),
        ["JD016"] = new(() => new CollectionConstraintsAdmitNoValueAnalyzer(), "Any.SetOf(Any.Boolean()).WithCount(5)", Discarded: false, Continuation: ".Generate()"),
        ["JD017"] = new(() => new EnumUniverseViolationAnalyzer(), "Any.Enum<Day>().Except(Day.Mon, Day.Tue)", Discarded: false, Continuation: ".Generate()"),
        ["JD021"] = new(() => new BlankReplaySnippetAnalyzer(), "Any.UseSeed(seed, \" \")", Discarded: false, Continuation: null),
        ["JD023"] = new(() => new ScalarChainAdmitsNoValueAnalyzer(), "Any.Int32().Positive().Negative()", Discarded: false, Continuation: ".Generate()"),
        ["JD025"] = new(() => new DuplicatePoolValueAnalyzer(), "Any.OneOf(1, 1)", Discarded: false, Continuation: ".Generate()"),
        ["JD026"] = new(() => new EmptyRelativeUriAnalyzer(), "Any.Uri().Relative().WithPathSegments(0)", Discarded: false, Continuation: ".Generate()"),
        ["JD027"] = new(() => new UnusedCombineOperandAnalyzer(), "Any.Combine(Any.Int32(), Any.String(), (number, text) => number)", Discarded: false, Continuation: ".Generate()"),
        ["JD028"] = new(() => new InertDistinctnessAnalyzer(), "Any.SetOf(Any.Int32().As(v => new Box(v)))", Discarded: false, Continuation: ".Generate()"),
        ["JD029"] = new(() => new PooledValueNeverDrawsAnalyzer(), "Any.String().OneOf(\"abc\", \"de\").WithLength(3)", Discarded: false, Continuation: ".Generate()"),
        ["JD030"] = new(() => new UndeclaredStringLengthAnalyzer(), "Any.String().Generate()", Discarded: false, Continuation: null),
        ["JD031"] = new(() => new PairedBoundsHaveARangeFormAnalyzer(), "Any.String().WithMinLength(8).WithMaxLength(20)", Discarded: false, Continuation: ".Generate()"),
        ["JD032"] = new(() => new BoundDeclaredTwiceAnalyzer(), "Any.String().WithMinLength(8).WithMinLength(10)", Discarded: false, Continuation: ".Generate()"),
        ["JD033"] = new(() => new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), "Any.String().AlphaNumeric().StartingWith(\"ORD-\").WithLength(12)", Discarded: false, Continuation: ".Generate()"),
        ["JD034"] = new(() => new ConstraintAlreadyDefinedAnalyzer(), "Any.String().Alpha().Numeric()", Discarded: false, Continuation: ".Generate()"),
        ["JD035"] = new(() => new ConstraintRepeatedAnalyzer(), "Any.String().Alpha().Alpha()", Discarded: false, Continuation: ".Generate()")
    };

    public static TheoryData<string, string> Shapes() {
        TheoryData<string, string> shapes = new();
        foreach ((string rule, Case @case) in Cases) {
            shapes.Add(rule, "parameterless lambda");
            shapes.Add(rule, "lambda taking a parameter");
            if (@case.Continuation is not null) { shapes.Add(rule, "chain going on past the call"); }
        }

        return shapes;
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task Reports_a_chain_a_test_asserts_to_be_refused(string rule, string shape) {
        Case   @case     = Cases[rule];
        string assertion = @case.Discarded ? "Sink.Run" : "Sink.Get";
        string mapping   = @case.Discarded ? "Sink.RunEach" : "Sink.GetEach";
        string statement = shape switch {
            "parameterless lambda"         => $"{assertion}(() => {@case.Chain})",
            "lambda taking a parameter"    => $"{mapping}(_ => {@case.Chain})",
            "chain going on past the call" => $"{assertion}(() => {@case.Chain}{@case.Continuation})",
            _                              => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
        };
        string source = $$"""
            using System;
            using JustDummies;

            public enum Day { Mon, Tue }

            public sealed class Box {
                public Box(int value) { Value = value; }
                public int Value { get; }
            }

            public static class Sink {
                public static void Run(Action code) { }
                public static void Get(Func<object> code) { }
                public static void RunEach(Action<int> code) { }
                public static void GetEach(Func<int, object> code) { }
            }

            public static class Sample {
                public static void M(int seed) {
                    {{statement}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(@case.Analyzer(), source, rule);

        Check.That(diagnostics.Count(diagnostic => diagnostic.Id == rule)).IsEqualTo(1);
    }

}
