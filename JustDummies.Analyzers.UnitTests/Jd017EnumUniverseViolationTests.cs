using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd017EnumUniverseViolationTests {

    private const string Declarations = """
        using System;

        [Flags]
        public enum Perm { None = 0, Read = 1, Write = 2 }

        public enum Day { Mon, Tue }

        public enum Status { Active = 1, Inactive = 2 }
        """;

    // The shapes the trial found reported twice, once by each rule (issue #321). JD017 owns the membership, so the
    // pair gives exactly one diagnostic between them, and it is this rule's.
    [Theory]
    [InlineData("Any.Enum<Status>().OneOf((Status)42)",               "42 is not a declared member of Status")]
    [InlineData("Any.Enum<Perm>().OneOf((Perm)4)",                    "4 is neither a declared member of Perm nor a combination of its declared members")]
    [InlineData("Any.Enum<Status>().OneOf(Status.Active, (Status)0)", "0 is not a declared member of Status")]
    [InlineData("Any.Enum<Status>().OneOf([Status.Active, (Status)0])", "0 is not a declared member of Status")]
    [InlineData("Any.Enum<Perm>().AllowingCombinations().OneOf((Perm)4)", "4 is neither a declared member of Perm nor a combination of its declared members")]
    public async Task Reports_a_value_the_type_does_not_define_once_between_JD014_and_JD017(string expression, string expectedFragment) {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> jd017 = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);
        ImmutableArray<Diagnostic> jd014 = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(jd014.Length).IsEqualTo(0);
        Check.That(jd017.Length).IsEqualTo(1);
        Check.That(jd017[0].GetMessage()).Contains(expectedFragment);
    }

    [Fact]
    public async Task Reports_a_value_the_type_does_not_define_on_a_generator_held_in_a_variable() {
        // The membership is judged on the OneOf call itself, as JD014 judged it before handing it over, so a receiver
        // the chain walk cannot trace back to Any.Enum<T>() loses nothing.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    AnyEnum<Day> days = Any.Enum<Day>();
                    _ = days.OneOf((Day)99);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("99 is not a declared member of Day");
    }

    [Fact]
    public async Task Does_not_report_a_same_named_generator_declared_outside_JustDummies() {
        // The membership is judged per OneOf call, so the call must be JustDummies' own: a project's AnyEnum may accept
        // any value it likes.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public sealed class AnyEnum {
                public AnyEnum OneOf(params Status[] values) => this;
            }

            public static class Sample {
                public static void M() {
                    _ = new AnyEnum().OneOf((Status)99);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_flag_combination_OneOf_accepts() {
        // The natural thing to write on a [Flags] enum, and the generator draws it -- with the opt-in, without it,
        // and in either order. Reporting here refused at build time a chain the run time honours.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().OneOf(Perm.Read | Perm.Write);
                    _ = Any.Enum<Perm>().OneOf(Perm.Read | Perm.Write).AllowingCombinations();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_value_no_combination_of_declared_members_produces() {
        // Bit 2 is declared nowhere, so OR-ing declared members never produces 4 -- the case the acceptance above
        // must not reach, since no constraint the caller could add would make it drawable.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().OneOf((Perm)4);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD017");
        Check.That(diagnostics[0].GetMessage()).Contains("neither a declared member of Perm nor a combination of its declared members");
    }

    [Fact]
    public async Task Reports_a_composite_on_an_enum_that_is_not_Flags() {
        // The exemption is gated on the attribute, exactly as the generator gates it: 3 would be Mon | Tue on a
        // [Flags] enum, and Day never said its members combine.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().OneOf((Day)3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("not a declared member of Day");
    }

    [Fact]
    public async Task Does_not_report_an_allow_listed_combination_no_exclusion_touches() {
        // Every DECLARED member is excluded, yet the allow-list names a value none of the exclusions carries -- and
        // the generator draws it. What decides is whether anything the caller allowed survives.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().OneOf(Perm.Read | Perm.Write).Except(Perm.None, Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Any.Enum<Perm>().OneOf(Perm.Read).Except(Perm.None, Perm.Read, Perm.Write)", "Perm")]
    [InlineData("Any.Enum<Day>().OneOf(Day.Mon).Except(Day.Mon)", "Day")]
    [InlineData("Any.Enum<Day>().Except(Day.Mon).OneOf(Day.Mon)", "Day")]
    [InlineData("Any.Enum<Perm>().OneOf(Perm.Read | Perm.Write).Except(Perm.Read | Perm.Write)", "Perm")]
    [InlineData("Any.Enum<Perm>().OneOf(Perm.Read, Perm.Write).Except(Perm.Read).DifferentFrom(Perm.Write)", "Perm")]
    public async Task Reports_an_exclusion_that_removes_the_whole_allow_list(string expression, string type) {
        // The other side of that narrowing: an allow-list every exclusion does carry leaves nothing to draw, and the
        // generator refuses it -- so standing down for the allow-list must not become standing down for all of them.
        // Issue #227: the pool is what is weighed, however many declared members it leaves out, in either order.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains($"no {type} value the allow-list names remains");
    }

    [Fact]
    public async Task Does_not_report_a_total_exclusion_beside_an_allow_list_it_cannot_read() {
        // An allow-list IS the pool, so an entry the rule cannot read leaves the surviving pool unknown -- and on a
        // [Flags] enum that entry may hold a combination no exclusion names, which the generator draws.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M(Perm value) {
                    _ = Any.Enum<Perm>().OneOf(value).Except(Perm.None, Perm.Read, Perm.Write);
                    _ = Any.Enum<Perm>().OneOf(Perm.Read, value).Except(Perm.None, Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_an_exclusion_that_removes_every_member_of_a_Flags_enum() {
        // A [Flags] enum with neither the opt-in nor a combination written anywhere still draws declared members
        // only, so emptying them empties the draw -- and the generator says so.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().Except(Perm.None, Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("no declared Perm member remains");
    }

    [Fact]
    public async Task Reports_an_undeclared_numeric_value() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().OneOf((Day)99);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("not a declared member of Day");
    }

    [Theory]
    [InlineData("Any.Enum<Day>().Except((Day)99)")]
    [InlineData("Any.Enum<Day>().DifferentFrom((Day)99)")]
    [InlineData("Any.Enum<Status>().DifferentFrom(default)")]
    [InlineData("Any.Enum<Perm>().Except((Perm)16)")]
    [InlineData("Any.Enum<Day>().OneOf(Day.Tue).Except((Day)99)")]
    public async Task Does_not_report_an_exclusion_of_a_value_the_type_does_not_define(string expression) {
        // Issue #228, ADR-0105: excluding a value the generator never draws removes nothing, and the library accepts
        // it as a no-op -- refusing it here failed a build the run time honours. Only OneOf is judged.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_an_exclusion_that_removes_every_member() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().Except(Day.Mon, Day.Tue);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("no declared Day member remains");
    }

    [Fact]
    public async Task Does_not_report_excluding_every_declared_member_once_AllowingCombinations_is_declared() {
        // The universe is the OR-closure once combinations are allowed, so removing every declared member still
        // leaves Read | Write to draw -- and the library does draw it. Reporting here refused at build time a chain
        // the run time honours, whichever order the two constraints were written in.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().AllowingCombinations().Except(Perm.None, Perm.Read, Perm.Write);
                    _ = Any.Enum<Perm>().Except(Perm.None, Perm.Read, Perm.Write).AllowingCombinations();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_combination_once_AllowingCombinations_is_declared() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().AllowingCombinations().OneOf(Perm.Read | Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_declared_members() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().OneOf(Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_partial_exclusion() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().Except(Day.Mon);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_unsubstituted_type_parameter() {
        // A generic helper gives no enum to reason about; the rule must bail rather than guess.
        string source = $$"""
            using System;
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static IAny<T> AnyOf<T>() where T : struct, Enum => Any.Enum<T>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        string source = $$"""
            using System;
            using JustDummies;
            {{Declarations}}

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.Enum<Day>().Except(Day.Mon, Day.Tue));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
