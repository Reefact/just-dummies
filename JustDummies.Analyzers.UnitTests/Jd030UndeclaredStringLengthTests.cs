using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd030UndeclaredStringLengthTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new UndeclaredStringLengthAnalyzer(), source);
    }

    [Fact]
    public async Task Reports_a_chain_that_declares_no_length() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD030");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Info);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 0 to 1024 characters");
    }

    [Theory]
    [InlineData("_ = Dummy.String().WithLength(10).Generate();")]
    [InlineData("_ = Dummy.String().WithMinLength(3).Generate();")]
    [InlineData("_ = Dummy.String().WithMaxLength(50).Generate();")]
    [InlineData("_ = Dummy.String().WithLengthBetween(1, 5).Generate();")]
    [InlineData("_ = Dummy.String().Alpha().WithMaxLength(8).Generate();")]
    public async Task Does_not_report_a_chain_that_settles_its_length(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_value_set_which_supplies_its_own_lengths() {
        // OneOf replaces the layout: the caller supplied the values, so their lengths are theirs and no spread
        // applies. Nothing here is left unsaid.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().OneOf(\"EUR\", \"USD\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_pattern_whose_shape_is_the_whole_specification() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.StringMatching(@\"[A-Z]{3}\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_chain_carrying_only_NonEmpty() {
        // NonEmpty raises the floor to one and leaves the ceiling where it was, so the draw still spans the whole
        // spread. Reporting it is the point: this is the chain the scaffolder emits, and the one most likely to be
        // mistaken for a bounded string. The interval shifts with the floor rather than staying at the constant
        // the unconstrained chain reports -- measured against the library, NonEmpty draws 1 to 1025.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().NonEmpty().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Reports_a_chain_carrying_only_NotBlank() {
        // NotBlank carries the same floor of one character as NonEmpty and leaves the ceiling alone, so it shifts
        // the reported interval identically. A rule that knew only NonEmpty would report 0 to 1024 here, one short
        // at both ends of what the library actually draws.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().NotBlank().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Counts_an_anchored_literal_in_the_reported_interval() {
        // An anchor occupies characters the draw cannot go below, so the whole interval shifts with it -- measured
        // against the library, StartingWith("hello") draws 5 to 1029. Reporting 0 to 1024 here would name an
        // interval containing lengths the chain can never produce.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().StartingWith(\"hello\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 5 to 1029 characters");
    }

    [Fact]
    public async Task Counts_every_anchor_and_keeps_the_higher_of_the_two_floors() {
        // Three anchors sum to eight characters, which outranks the floor of one NonEmpty sets -- measured against
        // the library, this draws 8 to 1032.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzeAsync("        _ = Dummy.String().StartingWith(\"ORD-\").Containing(\"XY\").EndingWith(\"99\").NonEmpty().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 8 to 1032 characters");
    }

    [Fact]
    public async Task Adds_the_NotBlank_position_beside_an_anchor_that_is_entirely_blank() {
        // The prefix carries no non-blank character, so NotBlank has to reserve a filler position of its own beside
        // it -- measured against the library, this draws 2 to 1026.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().StartingWith(\" \").NotBlank().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 2 to 1026 characters");
    }

    [Fact]
    public async Task Adds_no_NotBlank_position_beside_an_anchor_that_already_carries_one() {
        // The prefix satisfies the guarantee on its own, so nothing is reserved beyond the character it occupies --
        // measured against the library, this draws 1 to 1025. The same interval as a bare NotBlank, reached for a
        // different reason.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().StartingWith(\"A\").NotBlank().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Counts_a_repeated_prefix_once_because_it_occupies_a_single_slot() {
        // Re-declaring the same prefix is a no-op in the specification, so the second call adds no character --
        // measured against the library, this draws 1 to 1025. Adding both would name an interval one too high at
        // each end, which is the failure this rule exists to avoid.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().StartingWith(\"A\").StartingWith(\"A\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Counts_a_repeated_suffix_once_as_well() {
        // The suffix owns a single slot on the same terms as the prefix -- measured against the library, this draws
        // 1 to 1025.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().EndingWith(\"Z\").EndingWith(\"Z\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Counts_a_repeated_fragment_every_time_because_Containing_accumulates() {
        // Containing does not own a slot: a second identical fragment is a second fragment the value must carry --
        // measured against the library, this draws 4 to 1028. The counterpart to the two cases above, and the
        // reason they cannot share one rule.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Dummy.String().Containing(\"XY\").Containing(\"XY\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 4 to 1028 characters");
    }

    [Fact]
    public async Task Leaves_an_anchor_it_cannot_resolve_out_of_the_sum() {
        // A prefix the compiler cannot fold to a constant is invisible to the rule. Understating the floor is the
        // safe direction and is what this rule did for every anchor before it counted any: the blindness that hides
        // the length also keeps NotBlank's position from being added on top of it.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "        string prefix = System.Environment.MachineName;\n        _ = Dummy.String().StartingWith(prefix).NotBlank().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Does_not_report_a_chain_written_as_a_negative_test() {
        // The guard the other rules share: a chain that is the sole body of a lambda ARGUMENT is being asserted
        // about, not written as arrange code.
        const string source = """
            using System;
            using JustDummies;

            public static class Expect {
                public static bool Throws<T>(Func<string> code) where T : Exception => true;
            }

            public static class Sample {
                public static void M() {
                    Expect.Throws<ArgumentException>(() => Dummy.String().Generate());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UndeclaredStringLengthAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_the_factory_call_even_when_a_later_statement_narrows_the_reassigned_variable() {
        // The rule is syntactic (its documented Scope): it reasons about the Dummy.String() expression as written, not
        // about what the variable it is assigned to ends up holding. The factory call is reported the moment it is
        // written with no length, whatever a later statement does to the same variable.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "        DummyString s = Dummy.String();\n            s = s.WithMaxLength(5);\n            _ = s.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 0 to 1024 characters");
    }

}
