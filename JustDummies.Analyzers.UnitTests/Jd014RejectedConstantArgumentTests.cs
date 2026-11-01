using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd014RejectedConstantArgumentTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeWithDeclarationsAsync(string expression) {
        string source = $$"""
            using System;
            using JustDummies;

            public enum Status { Active, Pending, Closed }

            [Flags]
            public enum Access { None = 0, Read = 1, Write = 2, Exec = 4 }

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);
    }

    [Theory]
    [InlineData("Any.String().WithLengthBetween(10, 5)",        "transposed")]
    [InlineData("Any.Int32().Between(10, 5)",                   "transposed")]
    [InlineData("Any.String().WithLength(-1)",                  "negative")]
    [InlineData("Any.String().WithMaxLength(-1)",               "negative")]
    [InlineData("Any.String().WithLength(2000000)",             "1,000,000")]
    // A ceiling steers the draw as a floor does, so ADR-0076 caps it too — one rule for every size argument.
    // This row said otherwise until the scaffolder emitted WithMaxLength(1048576) for an ordinary 1 MiB limit,
    // compiled clean, drew no diagnostic, and threw inside the emitted parameterless constructor.
    [InlineData("Any.String().WithMaxLength(2000000)",          "1,000,000")]
    [InlineData("Any.ListOf(Any.Int32()).WithMaxCount(2000000)", "1,000,000")]
    [InlineData("Any.Int32().MultipleOf(0)",                    "strictly positive")]
    // Issue #224: every numeric overload carries the guard its Int32 twin does, and only the Int32 one was read.
    [InlineData("Any.Decimal().Between(100m, 1m)",              "transposed")]
    [InlineData("Any.Double().Between(10.0, 1.0)",              "transposed")]
    [InlineData("Any.Single().Between(10f, 1f)",                "transposed")]
    [InlineData("Any.Int64().Between(10, 1)",                   "transposed")]
    [InlineData("Any.UInt64().Between(10UL, 1UL)",              "transposed")]
    [InlineData("Any.Int64().MultipleOf(0)",                    "strictly positive")]
    [InlineData("Any.Int16().MultipleOf(-3)",                   "strictly positive")]
    [InlineData("Any.Byte().MultipleOf(0)",                     "strictly positive")]
    [InlineData("Any.Decimal().MultipleOf(0m)",                 "strictly positive")]
    [InlineData("Any.Decimal().MultipleOf(-0.25m)",             "strictly positive")]
    [InlineData("Any.Decimal().WithScale(29)",                  "[0, 28]")]
    [InlineData("Any.String().StartingWith(\"\")",              "must not be empty")]
    [InlineData("Any.String().WithChars(\"\")",                 "must not be empty")]
    public async Task Reports_an_argument_the_guard_rejects(string expression, string expectedFragment) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD014");
        Check.That(diagnostics[0].GetMessage()).Contains(expectedFragment);
    }

    [Fact]
    public async Task Reports_a_collection_count_range_that_is_transposed() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.ListOf(Any.Int32()).WithCountBetween(10, 2);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD014");
    }

    [Fact]
    public async Task Reports_an_empty_choice_pool() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.String().OneOf();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("at least one value");
    }

    [Fact]
    public async Task Reports_a_non_positive_granularity() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.DateTime().WithGranularity(TimeSpan.Zero);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("strictly positive");
    }

    [Theory]
    // A URI component the guard refuses wherever it runs: a host that is empty, not ASCII, or digits and dots that are
    // not a canonical dotted-quad — all checked before System.Uri reads it — and a user-info part outside the
    // unreserved characters.
    [InlineData("Any.Uri().Web().WithHost(\"\")",                   "must not be empty")]
    [InlineData("Any.Uri().Web().WithHost(\"münchen.de\")",         "punycode")]
    [InlineData("Any.Uri().Ftp().WithHost(\"1.2.3\")",              "canonical dotted-quad")]
    [InlineData("Any.Uri().Mailto().WithDomain(\"01.2.3.4\")",      "canonical dotted-quad")]
    [InlineData("Any.Uri().Web().WithUserInfo(\"user\", \"p@ss\")", "'@' is not one")]
    [InlineData("Any.Uri().Web().WithUserInfo(\"josé\")",           "'é' is not one")]
    [InlineData("Any.Uri().Mailto().WithLocalPart(\"first+last\")", "'+' is not one")]
    // An offset outside whole minutes or ±14:00, or a range whose ends are transposed.
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromSeconds(30))",                                              "whole number of minutes")]
    [InlineData("Any.DateTimeOffset().WithOffset(new TimeSpan(-15, 0, 0))",                                               "within ±14:00")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-1), TimeSpan.FromHours(15))",                 "within ±14:00")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(1))",                   "transposed")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(maximum: TimeSpan.FromHours(1), minimum: TimeSpan.FromHours(2))", "transposed")]
    // A granularity is read through every spelling of a TimeSpan, not only TimeSpan.Zero and the FromXxx factories.
    [InlineData("Any.DateTime().WithGranularity(new TimeSpan(0, 0, 0))",  "strictly positive")]
    [InlineData("Any.TimeSpan().WithGranularity(new TimeSpan(-1))",       "strictly positive")]
    // A value set element a floating-point generator refuses -- Half's NaN and infinities being members, not constants.
    [InlineData("Any.Double().OneOf(1.0, double.NaN)",                 "finite")]
    [InlineData("Any.Single().OneOf([float.PositiveInfinity])",        "finite")]
    [InlineData("Any.Half().OneOf((Half)1, Half.NaN)",                 "finite")]
    [InlineData("Any.Half().OneOf((Half)double.NegativeInfinity)",     "finite")]
    // A floating-point bound the guard refuses for not being finite (issue #327).
    [InlineData("Any.Double().Between(double.NaN, 90.0)",              "not accepted as a bound")]
    [InlineData("Any.Double().Between(1.0, double.PositiveInfinity)",  "not accepted as a bound")]
    // Refused by the finiteness guard, which runs first, so never reported as a transposed pair.
    [InlineData("Any.Double().Between(1.0, double.NegativeInfinity)",  "not accepted as a bound")]
    [InlineData("Any.Double().LessThan(double.PositiveInfinity)",      "not accepted as a bound")]
    [InlineData("Any.Double().LessThanOrEqualTo(double.NaN)",          "not accepted as a bound")]
    [InlineData("Any.Single().GreaterThan(float.NegativeInfinity)",    "not accepted as a bound")]
    [InlineData("Any.Double().GreaterThanOrEqualTo(float.NaN)",        "not accepted as a bound")]
    [InlineData("Any.Half().GreaterThanOrEqualTo(Half.PositiveInfinity)", "not accepted as a bound")]
    [InlineData("Any.Half().Between((Half)1, Half.NaN)",               "not accepted as a bound")]
    // A finite constant too large for a Half becomes an infinity on the way: 65520 is the midpoint above the largest
    // finite half, and rounds to the even step, which is the infinity.
    [InlineData("Any.Half().LessThan((Half)70000.0)",                  "not accepted as a bound")]
    [InlineData("Any.Half().GreaterThan((Half)(-65520.0))",            "not accepted as a bound")]
    [InlineData("Any.Half().Between((Half)1, (Half)100000)",           "not accepted as a bound")]
    [InlineData("Any.Half().OneOf((Half)1, (Half)65520f)",             "finite")]
    public async Task Reports_a_uri_component_an_offset_or_a_value_the_guard_rejects(string expression, string expectedFragment) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeWithDeclarationsAsync(expression);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD014");
        Check.That(diagnostics[0].GetMessage()).Contains(expectedFragment);
    }

    [Theory]
    [InlineData("Any.Uri().Web().WithHost(\"1.2.3.4\")")]
    [InlineData("Any.Uri().Web().WithHost(\"[::1]\")")]
    [InlineData("Any.Uri().Mailto().WithDomain(\"example.com\")")]
    // An empty user-info part passes its guard.
    [InlineData("Any.Uri().Web().WithUserInfo(\"\")")]
    [InlineData("Any.Uri().Web().WithUserInfo(\"A-b.c_d~9\")")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(-14))")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(maximum: TimeSpan.FromHours(2), minimum: TimeSpan.FromHours(1))")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromTicks(1))")]
    // The generic entry points carry no finiteness rule and no membership rule, by construction.
    [InlineData("Any.OneOf(double.NaN, 1.0)")]
    [InlineData("Any.OneOf((Status)99)")]
    // On a [Flags] enumeration a combination of declared members is accepted, and the zero value when it is declared.
    [InlineData("Any.Enum<Access>().OneOf(Access.Read | Access.Exec)")]
    [InlineData("Any.Enum<Access>().OneOf((Access)0)")]
    // Finite floating-point bounds pass their guard, a Half just below the overflow midpoint included; and a
    // non-finite exclusion is a no-op (ADR-0101).
    [InlineData("Any.Double().Between(-1e300, 1e300)")]
    [InlineData("Any.Half().LessThan(Half.MaxValue)")]
    [InlineData("Any.Half().LessThan((Half)65519.99)")]
    [InlineData("Any.Double().Except(double.NaN)")]
    // An exclusion is not checked against the members.
    [InlineData("Any.Enum<Status>().Except((Status)99)")]
    // A value the enumeration does not define is refused, but JD017 reports it: one mistake, one diagnostic (#321).
    [InlineData("Any.Enum<Status>().OneOf(Status.Active, (Status)99)")]
    [InlineData("Any.Enum<Access>().OneOf((Access)8)")]
    [InlineData("Any.Enum<Status>().OneOf(Status.Active).Except(Status.Pending).OneOf((Status)99)")]
    // Refused here, but not wherever the test may run. System.Uri judges every other host, and its parsing differs
    // across target frameworks; the invariant lower case of İ is i under some runtimes and globalization modes, and İ
    // under others.
    [InlineData("Any.Uri().Web().WithHost(\"::1\")")]
    [InlineData("Any.Uri().Web().WithHost(\"a b\")")]
    [InlineData("Any.Uri().Web().WithUserInfo(\"\\u0130\")")]
    // Not covered, by scope: the rule reads the numeric and Half forms of a transposed Between. The run-time guard
    // refuses these; widening the rule is a decision for ADR-0106, not a missing case.
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1))")]
    [InlineData("Any.TimeSpan().Between(TimeSpan.FromHours(2), TimeSpan.FromHours(1))")]
    [InlineData("Any.DateTimeOffset().Between(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))")]
    public async Task Does_not_report_a_uri_component_an_offset_or_a_value_the_guard_may_accept(string expression) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeWithDeclarationsAsync(expression);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Any.String().WithLengthBetween(5, 10)")]
    [InlineData("Any.Int32().Between(5, 10)")]
    [InlineData("Any.Int32().Between(5, 5)")]
    [InlineData("Any.String().WithLength(0)")]
    [InlineData("Any.String().WithMaxLength(0)")]
    // Verified rather than assumed: UriSpec.RequireSegmentCount refuses a negative count and nothing else, so
    // the one member of this family that really is uncapped keeps its silence.
    [InlineData("Any.Uri().Relative().WithPathSegments(2000000)")]
    [InlineData("Any.Int32().MultipleOf(7)")]
    [InlineData("Any.Decimal().MultipleOf(0.25m)")]
    // A start after the end on a time of day is the window across midnight, not a transposition (ADR-0125).
    [InlineData("Any.TimeOnly().Between(new System.TimeOnly(22, 0), new System.TimeOnly(6, 0))")]
    [InlineData("Any.Decimal().WithScale(28)")]
    [InlineData("Any.String().StartingWith(\"ORD-\")")]
    [InlineData("Any.String().OneOf(\"EUR\")")]
    [InlineData("Any.ListOf(Any.Int32()).WithCountBetween(2, 10)")]
    [InlineData("Any.Decimal().Between(1m, 100m)")]
    [InlineData("Any.Double().Between(1.0, 1.0)")]
    [InlineData("Any.UInt64().MultipleOf(4UL)")]
    public async Task Does_not_report_a_legal_argument(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_argument() {
        // Only a constant is decidable; a variable is the run-time guard's business and must stay unreported.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(int minimum, int maximum) {
                    _ = Any.Int32().Between(minimum, maximum);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_rejected_argument_inside_a_throws_assertion() {
        // No rule guesses which calls a test writes on purpose (ADR-0112): a test asserting the refusal reports like
        // any other code, and silences the rule with a justified [SuppressMessage].
        const string source = """
            using System;
            using JustDummies;

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.Int32().Between(10, 5).Generate());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Fact]
    public async Task Does_not_report_a_same_named_method_on_another_type() {
        const string source = """
            public sealed class Other {
                public Other Between(int minimum, int maximum) => this;
            }

            public static class Sample {
                public static void M() {
                    _ = new Other().Between(10, 5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_Containing_on_a_collection() {
        // Containing(TItem) is not the string overload; the non-empty-text check must key on the parameter type.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.ListOf(Any.Int32()).Containing(0);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
