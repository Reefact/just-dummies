using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd029PooledValueNeverDrawsTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new PooledValueNeverDrawsAnalyzer(), source);
    }

    [Theory]
    [InlineData("_ = Any.String().OneOf(\"abc\", \"de\").WithLength(3);", "WithLength(3)")]
    [InlineData("_ = Any.String().OneOf(\"abc\", \"x\").WithMinLength(2);", "WithMinLength(2)")]
    [InlineData("_ = Any.String().OneOf(\"ab\", \"abcdef\").WithMaxLength(3);", "WithMaxLength(3)")]
    [InlineData("_ = Any.String().OneOf(\"12\", \"ab\").Numeric();", "Numeric()")]
    [InlineData("_ = Any.String().OneOf(\"ab\", \"a-b\").Alpha();", "Alpha()")]
    [InlineData("_ = Any.String().OneOf(\"-:-\", \"abc\").Punctuation();", "Punctuation()")]
    [InlineData("_ = Any.String().OneOf(\"deadBEEF\", \"xyz\").Hexadecimal();", "Hexadecimal()")]
    [InlineData("_ = Any.String().OneOf(\" \", \"ab\").Whitespaces();", "Whitespaces()")]
    [InlineData("_ = Any.String().OneOf(\"-:-\", \"abc\").WithoutAlpha();", "WithoutAlpha()")]
    [InlineData("_ = Any.String().OneOf(\"-:-\", \"a1c\").WithoutNumeric();", "WithoutNumeric()")]
    [InlineData("_ = Any.String().OneOf(\"AB-1\", \"caf\\u00E9\").Printable();", "Printable()")]
    [InlineData("_ = Any.String().OneOf(\"ABC\", \"abc\").UpperCase();", "UpperCase()")]
    [InlineData("_ = Any.String().OneOf(\"ORD-1\", \"INV-1\").StartingWith(\"ORD-\");", "StartingWith(\"ORD-\")")]
    [InlineData("_ = Any.String().OneOf(\"a-FR\", \"a-BE\").EndingWith(\"-FR\");", "EndingWith(\"-FR\")")]
    [InlineData("_ = Any.String().OneOf(\"xKEYx\", \"nope\").Containing(\"KEY\");", "Containing(\"KEY\")")]
    [InlineData("_ = Any.String().OneOf(\"a\", \"b\").DifferentFrom(\"b\");", "DifferentFrom(\"b\")")]
    [InlineData("_ = Any.Int32().OneOf(1, 5, 42).Between(1, 10);", "Between(1, 10)")]
    [InlineData("_ = Any.Int32().OneOf(1, -3).Positive();", "Positive()")]
    [InlineData("_ = Any.Int32().OneOf(-1, 3).Negative();", "Negative()")]
    [InlineData("_ = Any.Int32().OneOf(0, 7).NonZero();", "NonZero()")]
    [InlineData("_ = Any.Int32().OneOf(6, 7).MultipleOf(3);", "MultipleOf(3)")]
    [InlineData("_ = Any.Int32().OneOf(4, 12).GreaterThan(5);", "GreaterThan(5)")]
    [InlineData("_ = Any.Int64().OneOf(1L, 99L).LessThanOrEqualTo(50L);", "LessThanOrEqualTo(50)")]
    [InlineData("_ = Any.Byte().OneOf((byte)1, (byte)9).LessThan(5);", "LessThan(5)")]
    [InlineData("_ = Any.Decimal().OneOf(1.5m, 2.25m).WithScale(1);", "WithScale(1)")]
    [InlineData("_ = Any.Decimal().OneOf(1.5m, -2m).Positive();", "Positive()")]
    [InlineData("_ = Any.Int32().OneOf(1, 2).DifferentFrom(2);", "DifferentFrom(2)")]
    public async Task Reports_the_value_a_declared_constraint_refuses(string body, string expected) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD029");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"This value never draws: {expected} refuses it");
    }

    [Fact]
    public async Task Does_not_report_a_binary_floating_point_pool() {
        // Double and Single are out of scope on purpose: their constants have no exact decimal, and judging one
        // through decimal could refuse a value the run time admits. Under-reporting is the safe direction.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.Double().OneOf(0.1, 99.0).LessThan(1.0);");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Names_a_two_bound_call_under_its_own_name() {
        // WithLengthBetween sets two bounds under one name, and the caller can only loosen the call. Reporting a
        // half would point at something they cannot edit on its own.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().OneOf(\"ab\", \"a\").WithLengthBetween(2, 3);");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This value never draws: WithLengthBetween(2, 3) refuses it");
    }

    [Fact]
    public async Task Reports_every_offending_value_not_only_the_first() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().OneOf(\"abc\", \"de\", \"fg\").WithLength(3);");

        Check.That(diagnostics.Length).IsEqualTo(2);
    }

    [Fact]
    public async Task Reports_on_the_offending_value_rather_than_on_the_chain() {
        // The squiggle belongs under the value a reader must decide about, not under the whole chain.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    string code = Any.String().OneOf("abc", "de").WithLength(3).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PooledValueNeverDrawsAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(source.Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length)).IsEqualTo("\"de\"");
    }

    [Theory]
    [InlineData("_ = Any.String().OneOf(\"abc\", \"xyz\").WithLength(3);")]
    [InlineData("_ = Any.String().OneOf(\"12\", \"34\").Numeric();")]
    [InlineData("_ = Any.String().WithLength(3).Alpha();")]
    [InlineData("_ = Any.String().OneOf(\"abc\", \"de\");")]
    [InlineData("_ = Any.Int32().OneOf(2, 4).Between(1, 10);")]
    [InlineData("_ = Any.Int32().Between(1, 10);")]
    [InlineData("_ = Any.Decimal().OneOf(1.5m, 2.5m).WithScale(1);")]
    public async Task Does_not_report_a_pool_in_step_with_its_constraints(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_through_an_inline_seeded_chain() {
        // The chain walk used to name WithSeed as the factory here and fall through, silencing this rule on the
        // very form the library recommends for reproducibility.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.WithSeed(1).Int32().OneOf(1, -3).Positive();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This value never draws: Positive() refuses it");
    }

    [Fact]
    public async Task Does_not_report_a_pool_held_in_a_variable() {
        // The documented limit, and the reason ADR-0067 calls this a complement rather than an alternative: a
        // catalogue is a variable by nature, and IPoolInspection<T> is what answers for it at run time.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    string[] names = ["abc", "de"];
                    _ = Any.String().OneOf(names).WithLength(3);
            """);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_the_constraint_argument_does_not_fold() {
        // Under-reporting is the safe direction: a constraint the walk cannot evaluate is absent rather than
        // guessed at, so the rule never accuses a value it has not actually tested.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    int length = System.Environment.ProcessorCount;
                    _ = Any.String().OneOf("abc", "de").WithLength(length);
            """);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        // The guard covers the shape a test uses to assert a failure: the chain is the WHOLE body of a lambda
        // handed to another call. A pool written to be refused there is the subject, not a mistake.
        const string source = """
            using System;

            using JustDummies;

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.String().OneOf("abc", "de").WithLength(3));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PooledValueNeverDrawsAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_value_only_a_later_constraint_refuses() {
        // The value set may be declared first or last; the chain is judged whole, from its outermost call.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().WithLength(3).OneOf(\"abc\", \"de\");");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This value never draws: WithLength(3) refuses it");
    }

    [Fact]
    public async Task Does_not_report_when_no_pooled_value_survives_at_all() {
        // JD015 owns that chain: it throws, and saying so once about the chain beats listing every value in a
        // register that reads as "this still works".
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().AlphaNumeric().OneOf("ORD-1", "ORD-2").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PooledValueNeverDrawsAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
