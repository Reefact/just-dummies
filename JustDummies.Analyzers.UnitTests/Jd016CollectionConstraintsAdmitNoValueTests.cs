using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd016CollectionConstraintsAdmitNoValueTests {

    private const string EnumDeclarations = """
        public enum Day { Mon, Tue }
        """;

    [Theory]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithCount(0).NonEmpty()")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).NonEmpty().WithCount(0)")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).Empty().NonEmpty()")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithMinCount(5).WithMaxCount(2)")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithCount(2).WithMinCount(5)")]
    public async Task Reports_counts_that_cannot_all_hold(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD016");
    }

    [Fact]
    public async Task Reports_more_contained_elements_than_the_cap_allows() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Int32()).WithMaxCount(2).Containing(1).Containing(2).Containing(3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("cannot fit");
    }

    [Fact]
    public async Task Reports_a_set_asking_for_more_than_boolean_can_give() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.SetOf(Dummy.Boolean()).WithCount(5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 2");
    }

    [Fact]
    public async Task Reports_a_distinct_list_exceeding_the_enum_member_count() {
        string source = $$"""
            using JustDummies;

            {{EnumDeclarations}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Enum<Day>()).Distinct().WithCount(10);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 2");
    }

    [Fact]
    public async Task Reports_a_distinct_list_exceeding_an_explicit_pool() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.SetOf(Dummy.OneOf("a", "b")).WithCount(3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    /// <summary>
    ///     The small primitive rows are provable too, and were not read at all before: every element type but
    ///     bool and enum answered "unbounded", so a floor of 200 over a set of char went unreported.
    /// </summary>
    [Theory]
    [InlineData("Dummy.SetOf(Dummy.Char()).WithCount(200)", "only 128")]
    [InlineData("Dummy.SetOf(Dummy.Byte()).WithCount(300)", "only 256")]
    [InlineData("Dummy.SetOf(Dummy.SByte()).WithCount(300)", "only 256")]
    [InlineData("Dummy.SetOf(Dummy.Int16()).WithCount(70000)", "only 65536")]
    [InlineData("Dummy.SetOf(Dummy.UInt16()).WithCount(70000)", "only 65536")]
    // The one floating-point row narrow enough to prove: the two zeros compare equal, so a set keeps one.
    [InlineData("Dummy.SetOf(Dummy.Half()).WithCount(70000)", "only 63487")]
    // DummyChar.OneOf(...) reaches past ASCII on purpose — the pool is the caller's own — so walking through it
    // to the Char() factory beneath and answering 128 would be too wide, not too narrow.
    [InlineData("Dummy.SetOf(Dummy.Char().OneOf('a', 'b')).WithCount(5)", "only 2")]
    [InlineData("Dummy.SetOf(Dummy.Char().OneOf('a', 'b', 'c')).WithCount(4)", "only 3")]
    public async Task Reports_a_set_asking_for_more_than_a_small_primitive_row_can_give(string expression, string expected) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains(expected);
    }

    /// <summary>
    ///     An aliased enum declares more names than it has values, and the count that matters is the values.
    /// </summary>
    [Fact]
    public async Task Reports_a_set_counting_an_aliased_enums_values_rather_than_its_names() {
        const string source = """
            using JustDummies;

            public enum Grade { Low = 1, Medium = 2, High = 3, Min = 1, Max = 3 }

            public static class Sample {
                public static void M() {
                    _ = Dummy.SetOf(Dummy.Enum<Grade>()).WithCount(4);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 3");
    }

    [Theory]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithCountBetween(2, 10)")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).NonEmpty().WithMaxCount(5)")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithCount(3).Containing(1).Containing(2)")]
    [InlineData("Dummy.SetOf(Dummy.Boolean()).WithCount(2)")]
    [InlineData("Dummy.SetOf(Dummy.Int32()).WithCount(500)")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithCount(500)")]
    // A narrowed char pool that still fits, and a different generator's own OneOf(...) wrapped by Containing(...)
    // — which extends the effective domain past what OneOf alone draws (issue #188) — both stay silent. Scoping
    // the fix to DummyChar specifically (not every OneOf(...) overload) is what this pins.
    [InlineData("Dummy.SetOf(Dummy.Char().OneOf('a', 'b')).WithCount(2)")]
    [InlineData("Dummy.SetOf(Dummy.Char().OneOf('a', 'b', 'c')).WithCount(3)")]
    [InlineData("Dummy.SetOf(Dummy.Half()).WithCount(100)")]
    [InlineData("Dummy.SetOf(Dummy.Int32().OneOf(1, 2)).Containing(3).WithCount(3)")]
    public async Task Does_not_report_a_satisfiable_chain(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_distinct_list_over_a_small_domain() {
        // Without Distinct, repeats are fine: ten booleans in a list is ordinary.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Boolean()).WithCount(10);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

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
                    Check2.ThatCode(() => Dummy.SetOf(Dummy.Boolean()).WithCount(5));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_widened_flag_enum_domain() {
        // AllowingCombinations widens the universe to the OR-closure of the declared members — eight values for four
        // flags — so counting declared members would condemn a legal chain. Live in
        // JustDummies.UnitTests/DummyEnumCombinationTests.cs, which asserts WithCount(8) succeeds.
        const string source = """
            using System;
            using JustDummies;

            [Flags]
            public enum Perm { None = 0, Read = 1, Write = 2, Execute = 4 }

            public static class Sample {
                public static void M() {
                    _ = Dummy.SetOf(Dummy.Enum<Perm>().AllowingCombinations()).WithCount(8);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Still_reports_a_flag_enum_without_the_widening() {
        const string source = """
            using System;
            using JustDummies;

            [Flags]
            public enum Perm { None = 0, Read = 1, Write = 2, Execute = 4 }

            public static class Sample {
                public static void M() {
                    _ = Dummy.SetOf(Dummy.Enum<Perm>()).WithCount(5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 4");
    }

    [Fact]
    public async Task Does_not_report_an_element_generator_whose_domain_is_unprovable() {
        // An unprovable domain must never be treated as a small one.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(IDummy<int> elements) {
                    _ = Dummy.SetOf(elements).WithCount(1000);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
