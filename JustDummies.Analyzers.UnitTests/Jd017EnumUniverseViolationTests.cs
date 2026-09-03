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
        """;

    [Fact]
    public async Task Does_not_report_a_flag_combination_OneOf_accepts() {
        // The natural thing to write on a [Flags] enum, and the generator draws it -- with the opt-in, without it,
        // and in either order. Reporting here refused at build time a chain the run time honours.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.Enum<Perm>().OneOf(Perm.Read | Perm.Write);
                    _ = Dummy.Enum<Perm>().OneOf(Perm.Read | Perm.Write).AllowingCombinations();
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
                    _ = Dummy.Enum<Perm>().OneOf((Perm)4);
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
                    _ = Dummy.Enum<Day>().OneOf((Day)3);
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
                    _ = Dummy.Enum<Perm>().OneOf(Perm.Read | Perm.Write).Except(Perm.None, Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_an_exclusion_that_removes_the_whole_allow_list() {
        // The other side of that narrowing: an allow-list every exclusion does carry leaves nothing to draw, and the
        // generator refuses it -- so standing down for the allow-list must not become standing down for all of them.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.Enum<Perm>().OneOf(Perm.Read).Except(Perm.None, Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("no declared Perm member remains");
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
                    _ = Dummy.Enum<Perm>().OneOf(value).Except(Perm.None, Perm.Read, Perm.Write);
                    _ = Dummy.Enum<Perm>().OneOf(Perm.Read, value).Except(Perm.None, Perm.Read, Perm.Write);
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
                    _ = Dummy.Enum<Perm>().Except(Perm.None, Perm.Read, Perm.Write);
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
                    _ = Dummy.Enum<Day>().OneOf((Day)99);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("not a declared member of Day");
    }

    [Fact]
    public async Task Reports_an_exclusion_that_removes_every_member() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.Enum<Day>().Except(Day.Mon, Day.Tue);
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
                    _ = Dummy.Enum<Perm>().AllowingCombinations().Except(Perm.None, Perm.Read, Perm.Write);
                    _ = Dummy.Enum<Perm>().Except(Perm.None, Perm.Read, Perm.Write).AllowingCombinations();
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
                    _ = Dummy.Enum<Perm>().AllowingCombinations().OneOf(Perm.Read | Perm.Write);
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
                    _ = Dummy.Enum<Perm>().OneOf(Perm.Read, Perm.Write);
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
                    _ = Dummy.Enum<Day>().Except(Day.Mon);
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
                public static IDummy<T> AnyOf<T>() where T : struct, Enum => Dummy.Enum<T>();
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
                    Check2.ThatCode(() => Dummy.Enum<Day>().Except(Day.Mon, Day.Tue));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
