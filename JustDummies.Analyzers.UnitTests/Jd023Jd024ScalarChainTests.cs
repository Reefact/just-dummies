using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd023ScalarChainAdmitsNoValueTests {

    [Theory]
    [InlineData("Any.Int32().Between(1, 10).MultipleOf(20)")]
    [InlineData("Any.Int32().Between(4, 4).Odd()")]
    [InlineData("Any.Int32().Zero().Odd()")]
    [InlineData("Any.Int64().Odd().Between(-2, -2)")]
    [InlineData("Any.Byte().Between(3, 3).Even()")]
    [InlineData("Any.Int32().Odd().Between(1, 3).Except(1, 3)")]
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

    // Each chain beside the expression the analyzer reads, so the message is checked against the exception the
    // chain really throws rather than against a copy of its wording (issue #322).
    public static TheoryData<string, Action> BoundConflicts => new() {
        { "Any.Int32().Between(1, 6).GreaterThan(10)", () => Any.Int32().Between(1, 6).GreaterThan(10) },
        { "Any.Int32().GreaterThan(10).LessThan(3)", () => Any.Int32().GreaterThan(10).LessThan(3) },
        { "Any.Int32().Positive().Negative()", () => Any.Int32().Positive().Negative() },
        { "Any.Int32().Positive().Zero()", () => Any.Int32().Positive().Zero() },
        { "Any.Int32().Between(1, 10).Between(20, 30)", () => Any.Int32().Between(1, 10).Between(20, 30) },
        { "Any.Int64().GreaterThanOrEqualTo(10).LessThanOrEqualTo(9)", () => Any.Int64().GreaterThanOrEqualTo(10).LessThanOrEqualTo(9) },
        { "Any.UInt32().LessThan(3u).GreaterThan(5u)", () => Any.UInt32().LessThan(3u).GreaterThan(5u) },
        { "Any.Int32().GreaterThan(int.MaxValue)", () => Any.Int32().GreaterThan(int.MaxValue) }
    };

    [Theory]
    [MemberData(nameof(BoundConflicts))]
    public async Task Words_a_bound_conflict_as_the_run_time_does(string expression, Action declare) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");
        string                     thrown      = Check.ThatCode(declare).Throws<ConflictingAnyConstraintException>().Value.Message;

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No value satisfies this chain: c{thrown.Substring(1, thrown.Length - 2)}");
    }

    [Fact]
    public async Task Names_the_earlier_constraints_when_no_bound_crosses() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int32().Between(1, 10).MultipleOf(20);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("No value satisfies this chain: cannot apply MultipleOf(20) because no value admitted by Between(1, 10) survives it");
    }

    [Theory]
    [InlineData("Any.Int32().Between(1, 10).MultipleOf(5)")]
    [InlineData("Any.Int32().Positive().LessThan(100)")]
    [InlineData("Any.Int32().Between(1, 10).Except(5)")]
    [InlineData("Any.Int32().OneOf(1, 2, 3).Except(2)")]
    [InlineData("Any.Int32().GreaterThan(-100).LessThan(100).MultipleOf(7)")]
    [InlineData("Any.Int32().Between(1, 99).Odd()")]
    [InlineData("Any.Int32().Between(-1, -1).Odd()")]
    [InlineData("Any.Int32().Between(4, 4).Even()")]
    [InlineData("Any.Int32().Odd().Between(1, 3).Except(1)")]
    [InlineData("Any.Int32().Even().Between(-4, -4)")]
    [InlineData("Any.UInt32().GreaterThan(5u).LessThan(100u)")]
    // The exact types the rule reads since issue #356: decimal, whose bounds are strict or inclusive, and the temporal
    // generators, through their ticks or day numbers.
    [InlineData("Any.Decimal().GreaterThan(1m).LessThan(2m)")]
    [InlineData("Any.Decimal().Between(1m, 2m).MultipleOf(0.25m)")]
    [InlineData("Any.Decimal().Between(1.001m, 1.002m).WithScale(3)")]
    [InlineData("Any.Decimal().OneOf(1m, 2m).Except(2m)")]
    [InlineData("Any.Decimal().Positive().LessThan(0.001m)")]
    [InlineData("Any.DateOnly().After(new DateOnly(2026, 1, 1)).Before(new DateOnly(2026, 1, 3))")]
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)).WithGranularity(TimeSpan.FromDays(1))")]
    [InlineData("Any.TimeSpan().Positive().LessThan(TimeSpan.FromTicks(2))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).After(new TimeOnly(23, 0))")]
    [InlineData("Any.TimeOnly().After(new TimeOnly(23, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Between(new TimeOnly(23, 0), new TimeOnly(1, 0))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).WithGranularity(TimeSpan.FromMinutes(30))")]
    public async Task Does_not_report_a_satisfiable_chain(string expression) {
        // A satisfiable chain may still be JD038's, when it leaves exactly one value: what this pins is that neither
        // JD023 nor JD024 speaks.
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).Not.Contains("JD023", "JD024");
    }

    [Theory]
    [InlineData("Any.Decimal().GreaterThan(100m).LessThan(10m)")]
    [InlineData("Any.Decimal().GreaterThan(1m).LessThanOrEqualTo(1m)")]
    [InlineData("Any.Decimal().Positive().Negative()")]
    [InlineData("Any.Decimal().GreaterThan(decimal.MaxValue)")]
    [InlineData("Any.Decimal().Between(1m, 2m).MultipleOf(5m)")]
    [InlineData("Any.Decimal().Between(1.001m, 1.002m).WithScale(2)")]
    [InlineData("Any.Decimal().Between(1.001m, 1.002m).WithScale(2).WithScale(2)")]
    [InlineData("Any.Decimal().MultipleOf(5m).MultipleOf(5m).Between(1m, 2m)")]
    [InlineData("Any.Decimal().Between(1m, 1m).NonZero().Except(1m)")]
    [InlineData("Any.Decimal().OneOf(1m, 2m).Except(1m, 2m)")]
    [InlineData("Any.Decimal().OneOf(1.5m).WithScale(0)")]
    [InlineData("Any.DateOnly().After(new DateOnly(2027, 1, 1)).Before(new DateOnly(2026, 1, 1))")]
    [InlineData("Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1)).DifferentFrom(new DateOnly(2026, 1, 1))")]
    [InlineData("Any.DateTime().After(DateTime.MaxValue)")]
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 1, 12, 0, 0), new DateTime(2026, 1, 1, 12, 30, 0)).WithGranularity(TimeSpan.FromDays(1))")]
    [InlineData("Any.DateTimeOffset().AfterOrEqualTo(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)).BeforeOrEqualTo(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))")]
    [InlineData("Any.TimeSpan().Positive().Negative()")]
    [InlineData("Any.TimeSpan().Zero().NonZero()")]
    [InlineData("Any.TimeSpan().Between(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(59)).WithGranularity(TimeSpan.FromHours(1))")]
    [InlineData("Any.TimeOnly().After(new TimeOnly(10, 0)).Before(new TimeOnly(9, 0))")]
    // A time outside a window across midnight admits no value, whichever side of the window it is written on, and a
    // window inside it whose end comes first along it leaves two pieces (ADR-0125).
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).After(new TimeOnly(12, 0))")]
    [InlineData("Any.TimeOnly().Before(new TimeOnly(12, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(9, 0), new TimeOnly(17, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Between(new TimeOnly(1, 0), new TimeOnly(23, 0))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(23, 0), new TimeOnly(1, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).OneOf(new TimeOnly(12, 0))")]
    public async Task Reports_an_exact_type_chain_that_admits_no_value(string expression) {
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD023");
    }

    // The exact types, each beside the chain the run time throws on, so the wording is checked against the exception
    // rather than against a copy of it: a strict decimal bound, a temporal bound rendered as the run time renders it,
    // and the type's own edge when no declared bound is the other side.
    public static TheoryData<string, Action> ExactTypeBoundConflicts => new() {
        { "Any.Decimal().GreaterThan(100m).LessThan(10m)", () => Any.Decimal().GreaterThan(100m).LessThan(10m) },
        { "Any.Decimal().GreaterThan(1m).LessThanOrEqualTo(1m)", () => Any.Decimal().GreaterThan(1m).LessThanOrEqualTo(1m) },
        { "Any.Decimal().Between(1.50m, 5m).GreaterThan(10m)", () => Any.Decimal().Between(1.50m, 5m).GreaterThan(10m) },
        { "Any.Decimal().Positive().Negative()", () => Any.Decimal().Positive().Negative() },
        { "Any.Decimal().Positive().GreaterThan(decimal.MaxValue)", () => Any.Decimal().Positive().GreaterThan(decimal.MaxValue) },
        { "Any.Int32().Positive().GreaterThan(int.MaxValue)", () => Any.Int32().Positive().GreaterThan(int.MaxValue) },
        { "Any.DateOnly().After(new DateOnly(2027, 1, 1)).Before(new DateOnly(2026, 1, 1))", () => Any.DateOnly().After(new DateOnly(2027, 1, 1)).Before(new DateOnly(2026, 1, 1)) },
        { "Any.DateTime().After(new DateTime(2026, 1, 2)).BeforeOrEqualTo(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))", () => Any.DateTime().After(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)).BeforeOrEqualTo(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)) },
        { "Any.DateTimeOffset().Before(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2))).After(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))", () => Any.DateTimeOffset().Before(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2))).After(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)) },
        { "Any.TimeSpan().LessThan(TimeSpan.FromHours(1)).GreaterThanOrEqualTo(TimeSpan.FromHours(2))", () => Any.TimeSpan().LessThan(TimeSpan.FromHours(1)).GreaterThanOrEqualTo(TimeSpan.FromHours(2)) },
        { "Any.TimeSpan().GreaterThan(TimeSpan.MaxValue)", () => Any.TimeSpan().GreaterThan(TimeSpan.MaxValue) },
        { "Any.TimeOnly().After(new TimeOnly(10, 0)).Before(new TimeOnly(9, 30))", () => Any.TimeOnly().After(new TimeOnly(10, 0)).Before(new TimeOnly(9, 30)) },
        { "Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).After(new TimeOnly(12, 0))", () => Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).After(new TimeOnly(12, 0)) },
        { "Any.TimeOnly().Before(new TimeOnly(12, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0))", () => Any.TimeOnly().Before(new TimeOnly(12, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0)) },
        { "Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Between(new TimeOnly(1, 0), new TimeOnly(23, 0))", () => Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Between(new TimeOnly(1, 0), new TimeOnly(23, 0)) },
        { "Any.TimeOnly().Between(new TimeOnly(23, 0), new TimeOnly(1, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0))", () => Any.TimeOnly().Between(new TimeOnly(23, 0), new TimeOnly(1, 0)).Between(new TimeOnly(22, 0), new TimeOnly(6, 0)) }
    };

    [Theory]
    [MemberData(nameof(ExactTypeBoundConflicts))]
    public async Task Words_an_exact_type_bound_conflict_as_the_run_time_does(string expression, Action declare) {
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");
        string                     thrown      = Check.ThatCode(declare).Throws<ConflictingAnyConstraintException>().Value.Message;

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD023");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No value satisfies this chain: c{thrown.Substring(1, thrown.Length - 2)}");
    }

    [Theory]
    [InlineData("Any.Double().Positive().LessThan(-5)")]
    [InlineData("Any.Single().Between(1f, 2f).GreaterThan(3f)")]
    [InlineData("Any.Half().Positive().Negative()")]
    public async Task Does_not_report_a_binary_floating_point_generator(string expression) {
        // The model folds exactly; a binary floating-point domain does not: neighbours and rounding do not fold like
        // integers, so Double, Single and Half stay out (issue #356).
        string source = $$"""
            using System;
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

    [Theory]
    [InlineData("Any.Decimal().MultipleOf(0.25m).WithScale(1).Between(1m, 1.1m)")]
    [InlineData("Any.Decimal().OneOf(1m).OneOf(2m).Except(2m)")]
    [InlineData("Any.Int32().MultipleOf(2).Odd().Between(1, 1)")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromMinutes(1)).Between(new DateTime(2026, 1, 1, 0, 0, 1), new DateTime(2026, 1, 1, 0, 0, 2))")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).WithGranularity(TimeSpan.FromMinutes(7)).OneOf(new TimeOnly(12, 0))")]
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 1)).After(DateTime.MaxValue)")]
    public async Task Leaves_a_chain_another_rule_or_the_run_time_refuses_first(string expression) {
        // A second lattice, a second value set or a step off the scale's grid is JD034's; a transposed range JD014's;
        // a granularity that does not divide a day on a window across midnight, and a Local bound, are the run time's
        // own refusals. The walk ends there rather than judging what comes after.
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).Not.Contains("JD023");
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
    public async Task Words_negative_bounds_as_the_run_time_does_whatever_the_current_culture() {
        // Some cultures spell the minus sign U+2212; the run time writes its message invariantly, and so must the rule.
        CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "\u2212";
        CultureInfo previous = CultureInfo.CurrentCulture;
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Int32().Between(-10, -5).GreaterThan(-3);
                }
            }
            """;

        CultureInfo.CurrentCulture = culture;
        try {
            ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");
            string                     thrown      = Check.ThatCode(() => Any.Int32().Between(-10, -5).GreaterThan(-3)).Throws<ConflictingAnyConstraintException>().Value.Message;

            Check.That(diagnostics.Length).IsEqualTo(1);
            Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No value satisfies this chain: c{thrown.Substring(1, thrown.Length - 2)}");
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}

public class Jd023ScalarChainLargeStepTests {

    // A step as wide as the type: the walk over the lattice adds one step at a time, and a sum that leaves the type
    // must end the walk rather than throw inside the analyzer, where Roslyn would report AD0001 in place of every
    // other diagnostic of the build.
    [Theory]
    [InlineData("Any.Decimal().MultipleOf(decimal.MaxValue)")]
    [InlineData("Any.Decimal().MultipleOf(decimal.MaxValue / 2 + 1)")]
    [InlineData("Any.Decimal().MultipleOf(decimal.MaxValue / 3)")]
    [InlineData("Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).MultipleOf(decimal.MaxValue / 2 + 1)")]
    [InlineData("Any.Decimal().GreaterThan(decimal.MinValue).MultipleOf(decimal.MaxValue / 2 + 1)")]
    [InlineData("Any.Decimal().Positive().MultipleOf(decimal.MaxValue / 2 + 1)")]
    [InlineData("Any.Decimal().Between(decimal.MaxValue - 1, decimal.MaxValue).MultipleOf(decimal.MaxValue)")]
    [InlineData("Any.Decimal().MultipleOf(decimal.MaxValue / 2 + 1).Except(0m)")]
    [InlineData("Any.Int64().MultipleOf(long.MaxValue)")]
    [InlineData("Any.Int64().MultipleOf(long.MaxValue / 2 + 1)")]
    [InlineData("Any.Int64().Between(long.MinValue, long.MaxValue).MultipleOf(long.MaxValue / 2 + 1).Except(0)")]
    public async Task Does_not_throw_on_a_step_as_wide_as_the_type(string expression) {
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).Not.Contains("AD0001");
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
        // unrepresentable and condemned a legal chain. Exactly that value is JD038's to report, not JD023's.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD038");
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

    [Theory]
    [InlineData("Any.UInt64().GreaterThan(long.MaxValue)")]
    [InlineData("Any.UInt64().GreaterThan(9223372036854775807UL)")]
    [InlineData("Any.UInt64().MultipleOf(4).GreaterThan(long.MaxValue)")]
    [InlineData("Any.UInt64().LessThanOrEqualTo(long.MaxValue)")]
    [InlineData("Any.UInt64().GreaterThanOrEqualTo(9223372036854775000UL).MultipleOf(500UL).Except(9223372036854775000UL, 9223372036854775500UL)")]
    public async Task Does_not_report_a_ulong_bound_at_the_models_top(string expression) {
        // Issue #224: every value in (long.MaxValue, ulong.MaxValue] satisfies these, and the first used to be
        // reported as admitting none. The model's top is not the type's here, so the chain is not judged. The last
        // excludes every multiple of 500 the model holds above its floor, and 9223372036854776000 is still one.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Any.Int32().GreaterThan(int.MaxValue)")]
    [InlineData("Any.Byte().GreaterThan(255)")]
    [InlineData("Any.UInt32().LessThan(0)")]
    [InlineData("Any.Int16().LessThan(short.MinValue)")]
    public async Task Reports_a_bound_beyond_the_types_own_range(string expression) {
        // Issue #224, the other direction: each of these throws at run time, and was silent because the model only
        // knew the long-wide edges.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD023");
    }

    [Fact]
    public async Task Leaves_a_transposed_range_to_JD014() {
        // Transposed bounds are refused as an argument before any constraint applies: JD014 says so, and an empty
        // domain would be the wrong diagnosis.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("Any.Int64().Between(10, 1)");

        Check.That(diagnostics).IsEmpty();
    }

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

}

public class Jd024NarrowedToItsOwnShapeTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string expression) {
        return await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), Source(expression), "JD023", "JD024");
    }

    private static async Task<ImmutableArray<Diagnostic>> RepetitionsAsync(string expression) {
        return await AnalyzerTestHarness.GetDiagnosticsAsync(new ConstraintRepeatedAnalyzer(), Source(expression));
    }

    private static string Source(string expression) {
        return $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;
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

    [Theory]
    [InlineData("Any.Int32().Except(5).Except(5)")]
    [InlineData("Any.Int32().Except(5, 6).Except(6, 5)")]
    [InlineData("Any.Int32().DifferentFrom(5).DifferentFrom(5)")]
    public async Task Stands_down_on_an_exclusion_the_chain_already_wrote(string expression) {
        // The same exclusion written twice is a repetition, which JD035 reports in every generator family (issue
        // #279, ADR-0107), so that one mistake draws one diagnostic, as with a bound written twice and JD032.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);
        ImmutableArray<Diagnostic> repetitions = await RepetitionsAsync(expression);

        Check.That(diagnostics).IsEmpty();
        Check.That(repetitions.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD035");
    }

    [Theory]
    [InlineData("Any.Int32().Except(5, 6).Except(5)")]
    [InlineData("Any.Int32().Except(5).DifferentFrom(5)")]
    public async Task Still_reports_an_exclusion_an_earlier_different_one_made_inert(string expression) {
        // Not the same call written again, so not a repetition: what JD024's message describes, and nothing JD035
        // reports.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);
        ImmutableArray<Diagnostic> repetitions = await RepetitionsAsync(expression);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD024");
        Check.That(repetitions).IsEmpty();
    }

    [Fact]
    public async Task Still_reports_a_range_declared_twice_to_no_effect() {
        // Between carries two bounds in one call, so JD032 leaves it alone and JD024 keeps it.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("Any.Int32().Between(1, 10).Between(1, 20)");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
    }

}
