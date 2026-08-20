using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd023ScalarChainAdmitsNoValueTests {

    [Theory]
    [InlineData("Any.Int32().Between(1, 10).MultipleOf(20)")]
    [InlineData("Any.Int32().GreaterThan(10).LessThan(3)")]
    [InlineData("Any.Int32().Positive().LessThan(-5)")]
    [InlineData("Any.Int32().Positive().Negative()")]
    [InlineData("Any.Int32().Zero().NonZero()")]
    [InlineData("Any.Int32().OneOf(5).Except(5)")]
    [InlineData("Any.Int64().GreaterThanOrEqualTo(10).LessThanOrEqualTo(9)")]
    // The unsigned families, written the way their own type spells a literal. Without a suffix these read as int
    // constants and were judged; with one they were abandoned unread, so whether the rule spoke turned on how the
    // caller typed the number rather than on any boundary the page documents.
    [InlineData("Any.UInt32().GreaterThan(5u).LessThan(3u)")]
    [InlineData("Any.UInt16().GreaterThan((ushort)5).LessThan((ushort)3)")]
    [InlineData("Any.UInt64().GreaterThanOrEqualTo(10UL).LessThanOrEqualTo(9UL)")]
    public async Task Reports_a_chain_that_admits_no_value(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD023");
    }

    [Theory]
    [InlineData("Any.Int32().Between(1, 10).MultipleOf(5)")]
    [InlineData("Any.Int32().Positive().LessThan(100)")]
    [InlineData("Any.Int32().Between(1, 10).Except(5)")]
    [InlineData("Any.Int32().OneOf(1, 2, 3).Except(2)")]
    [InlineData("Any.Int32().GreaterThan(-100).LessThan(100).MultipleOf(7)")]
    [InlineData("Any.UInt32().GreaterThan(5u).LessThan(100u)")]
    public async Task Does_not_report_a_satisfiable_chain(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_argument() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(int bound) {
                    _ = Any.Int32().GreaterThan(bound).LessThan(3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_integer_generator() {
        // The model is integer arithmetic; a floating-point domain does not behave like one.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Double().Positive().LessThan(-5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        const string source = """
            using System;
            using JustDummies;

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.Int32().Positive().Negative());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd024ConstraintWithNoEffectTests {

    [Fact]
    public async Task Reports_an_exclusion_the_domain_could_never_produce() {
        // The dangerous case: the author excluded a sentinel the generator was never going to draw. It silently
        // misses, and starts mattering the day someone widens the range.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int32().Between(1, 10).Except(20);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
        Check.That(diagnostics[0].GetMessage()).Contains("removes no value");
    }

    [Fact]
    public async Task Reports_a_bound_already_implied() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int32().Positive().GreaterThan(-5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
        Check.That(diagnostics[0].GetMessage()).Contains("already implied");
    }

    [Fact]
    public async Task Does_not_report_an_exclusion_that_removes_something() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int32().Between(1, 10).Except(5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_bound_that_narrows() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int32().Positive().GreaterThan(100);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd023ScalarChainRepresentableExtremesTests {

    [Theory]
    [InlineData("Any.Int64().LessThanOrEqualTo(long.MinValue)")]
    [InlineData("Any.Int64().GreaterThanOrEqualTo(long.MaxValue)")]
    [InlineData("Any.Int32().LessThanOrEqualTo(int.MinValue)")]
    public async Task Does_not_report_a_bound_at_a_representable_extreme(string expression) {
        // Live in JustDummies.UnitTests/AnySignedIntegerTests.cs, which asserts these generate exactly that value.
        // The first version used -long.MaxValue as its "unbounded" sentinel, which made long.MinValue
        // unrepresentable and condemned a legal chain.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_ulong_bound_the_model_cannot_hold() {
        // The interval this rule reasons in is long-wide, so a ulong past long.MaxValue is an argument it cannot
        // evaluate. It is abandoned like any other unreadable argument rather than truncated into a bound that
        // would mean something else: the chain below really does admit no value, and staying silent about it is
        // the direction this rule is allowed to err in. Accusing on a guess is not.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.UInt64().GreaterThan(ulong.MaxValue - 1).LessThan(3UL);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_bound_beyond_the_representable_range() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int64().GreaterThan(long.MaxValue);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD023");
    }

}

public class Jd024NarrowedToItsOwnShapeTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");
    }

    [Theory]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(10).GreaterThanOrEqualTo(8)")]
    [InlineData("Any.Int32().LessThanOrEqualTo(50).LessThanOrEqualTo(90)")]
    [InlineData("Any.Int32().GreaterThan(10).GreaterThan(5)")]
    public async Task Stands_down_on_a_bound_the_chain_already_named(string expression) {
        // JD032 owns the same NAME declared twice, in both writing orders and in every family (ADR-0078). Two
        // diagnostics on one expression for one mistake is noise that teaches a reader to disable both, so JD024
        // reports nothing here -- and the message it would have used, "already implied by the constraints declared
        // before it", is the one JD032 states properly.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);

        Check.That(diagnostics).IsEmpty();
    }

    [Theory]
    [InlineData("Any.Int32().Positive().GreaterThan(-5)")]
    [InlineData("Any.Int32().GreaterThan(5).Positive()")]
    public async Task Still_reports_a_bound_implied_by_a_different_one(string expression) {
        // What JD024's message actually describes, and the shape its page documents. The bound narrows nothing,
        // but it is not the same bound written twice, so nothing else covers it.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
    }

    [Fact]
    public async Task Still_reports_an_exclusion_that_removes_nothing() {
        // The case JD024 exists for, and the one its information severity is justified by.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("Any.Int32().Between(1, 10).Except(20)");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
    }

    [Fact]
    public async Task Still_reports_a_range_declared_twice_to_no_effect() {
        // Between carries two bounds in one call, so JD032 leaves it alone and JD024 keeps it.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("Any.Int32().Between(1, 10).Between(1, 20)");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
    }

}
