using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd033AnchoredLiteralOutsideCharacterFamilyTests {

    [Fact]
    public async Task Reports_a_family_that_cannot_draw_a_prefixs_character() {
        // The chain is legal and stays legal (ADR-0079): what the rule says is what follows from it, that the
        // separator lands in the prefix and nowhere else.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().AlphaNumeric().StartingWith("ORD-").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD033");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Info);
        Check.That(diagnostics[0].GetMessage()).Contains("AlphaNumeric()");
        Check.That(diagnostics[0].GetMessage()).Contains("'-'");
        Check.That(diagnostics[0].GetMessage()).Contains("only where you wrote it");
    }

    [Fact]
    public async Task Does_not_report_a_prefix_the_family_can_draw() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Numeric().StartingWith("123").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_chain_that_declares_no_family() {
        // The unconstrained draw is the whole of ASCII (ADR-0075), so there is no alphabet to fall outside of.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith("ORD-").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_character_a_subtraction_removes() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithoutNumeric().StartingWith("ORD-1").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithoutNumeric()");
        Check.That(diagnostics[0].GetMessage()).Contains("'1'");
    }

    [Fact]
    public async Task Reports_a_letter_whose_case_the_chain_cannot_draw() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().UpperCase().StartingWith("ord-").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("UpperCase()");
        Check.That(diagnostics[0].GetMessage()).Contains("lowercase 'o'");
    }

    [Fact]
    public async Task Does_not_report_a_non_letter_under_a_casing() {
        // A casing constrains the case of a letter and says nothing about any other character, so the '-' is not
        // its business -- the same distinction JD015 carried before this rule inherited it.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().UpperCase().StartingWith("ORD-").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_suffix_and_a_contained_value_too() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Alpha().EndingWith("-42").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("Alpha()");
    }

    [Fact]
    public async Task Reports_a_character_outside_an_explicit_pool() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithChars("0123456789").Containing("-OK-").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithChars(\"0123456789\")");
    }

    [Fact]
    public async Task Does_not_report_once_a_value_set_is_declared() {
        // There is no filler beside a pooled value, so "appears only where you wrote it" has no subject; a pooled
        // value a constraint refuses is JD029's, and it reports the removal rather than a layout fact.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().AlphaNumeric().OneOf("ORD-1", "ORD-2").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_literal() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M(string prefix) {
                    return Any.String().AlphaNumeric().StartingWith(prefix).WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_chain_split_across_statements() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    AnyString generator = Any.String().AlphaNumeric();

                    return generator.StartingWith("ORD-").WithLength(12).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AnchoredLiteralOutsideCharacterFamilyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
