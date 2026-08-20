using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd015StringConstraintsAdmitNoValueTests {

    [Fact]
    public async Task Does_not_report_a_family_that_excludes_a_fragments_character() {
        // The '-' is not a digit, but it is not drawn either: the prefix is a literal the caller wrote, and a family
        // governs only what the generator draws (ADR-0077). Reporting here would refuse at build time a chain the
        // run time honours — which is what this rule did until #94.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Numeric().StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_ascii_fragment_under_a_family() {
        // An accented letter is outside ASCII, so no named family can draw it — and it still passes, because a
        // contained value is not drawn. The filler stays printable ASCII whatever the literal holds.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Printable().Containing("café").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_character_a_subtraction_removes() {
        // A subtraction narrows the filler alphabet exactly as a family does, so it exempts a literal for the same
        // reason: WithoutNumeric() removes the digits it draws, not the ones the caller wrote.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithoutNumeric().StartingWith("ORD-1").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_fragment_whose_case_the_chain_forbids() {
        // A casing is the third filter building the filler alphabet, so it follows the family: LowerCase() holds the
        // characters it draws to lower case and keeps "ABC" as written.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().LowerCase().StartingWith("ABC").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_length_budget_once_a_value_set_is_declared() {
        // With OneOf declared, the fragments are matched against the pooled values rather than laid out side by side,
        // so the budget no longer applies. Live in JustDummies.UnitTests/AnyStringValueSetTests.cs.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().OneOf("aba").WithMaxLength(3).Containing("ab").Containing("ba").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_family_declared_after_the_fragment() {
        // StringSpec re-validates the whole spec on every mutation, so the order does not change the verdict — the
        // exemption holds whether the family is declared before or after the fragment.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith("ORD-").Numeric().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_family_that_admits_none_of_a_value_sets_values() {
        // The counterpart of the exemption: a literal claims its own region of a shaped string, but a value set
        // claims the whole string, so the family's region IS that supplied value and the two must agree. This one
        // throws at declaration, and saying so at Info would read as "it still works".
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().AlphaNumeric().OneOf("ORD-1", "ORD-2").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD015");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostics[0].GetMessage()).Contains("AlphaNumeric()");
        Check.That(diagnostics[0].GetMessage()).Contains("allows none of the values");
    }

    [Fact]
    public async Task Does_not_report_a_value_set_one_value_survives() {
        // A narrowing rather than a contradiction: the chain draws "12345" happily, and JD029 names what went.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().AlphaNumeric().OneOf("ORD-1", "12345").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_casing_that_admits_none_of_a_value_sets_values() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().OneOf("abc", "def").UpperCase().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("UpperCase()");
    }

    [Fact]
    public async Task Does_not_report_a_value_set_whose_values_are_not_constant() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M(string code) {
                    return Any.String().AlphaNumeric().OneOf("ORD-1", code).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_fragments_that_cannot_fit_a_fixed_length() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithLength(3).StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("at least 4 characters");
    }

    [Fact]
    public async Task Reports_a_prefix_and_suffix_that_exceed_the_cap() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithMaxLength(6).StartingWith("SKU-").EndingWith("-EUR").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithMaxLength(6)");
    }

    [Fact]
    public async Task Does_not_report_a_budget_that_fits_exactly() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithLength(8).StartingWith("SKU-").EndingWith("-EUR").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_fragment() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M(string prefix) {
                    return Any.String().WithLength(3).StartingWith(prefix).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_chain_split_across_statements() {
        // The chain must be one expression: following a generator through a local would need dataflow, and a rule
        // claiming a chain is unsatisfiable must see every constraint it carries.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    AnyString generator = Any.String().WithLength(3);

                    return generator.StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

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
                    Check2.ThatCode(() => Any.String().WithLength(3).StartingWith("ORD-"));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
