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
        // A casing is the third filter building the filler alphabet, so it follows the family: InLowerCase() holds the
        // characters it draws to lower case and keeps "ABC" as written.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().InLowerCase().StartingWith("ABC").Generate();
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
    public async Task Reports_a_value_set_of_nothing_but_blanks_under_NotBlank() {
        // Every value is non-empty, so NonEmpty would admit the whole pool; NotBlank empties it. The tab is the
        // half of the difference the Whitespaces family does not name, which is why the test carries one.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().NotBlank().OneOf("  ", "\t").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD015");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostics[0].GetMessage()).Contains("NotBlank()");
        Check.That(diagnostics[0].GetMessage()).Contains("allows none of the values");
    }

    [Fact]
    public async Task Reports_a_value_set_no_value_survives_two_constraints_at_once() {
        // Neither constraint is at fault on its own -- WithoutNumeric() admits "A", InLowerCase() admits "1" -- so
        // testing them one by one finds nothing. Measured: the library refuses this chain at declaration.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithoutNumeric().InLowerCase().OneOf("1", "A").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithoutNumeric()");
        Check.That(diagnostics[0].GetMessage()).Contains("InLowerCase()");
        Check.That(diagnostics[0].GetMessage()).Contains("together");
    }

    [Fact]
    public async Task Reports_a_value_set_no_value_survives_three_constraints_at_once() {
        // One value falls to each: "1" to WithoutNumeric(), "A" to InLowerCase(), " " to NotBlank().
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithoutNumeric().InLowerCase().NotBlank().OneOf("1", "A", " ").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithoutNumeric(), InLowerCase() and NotBlank()");
    }

    [Fact]
    public async Task Does_not_report_a_value_set_one_value_survives_every_constraint() {
        // "a" passes both, so the pair is a narrowing rather than a contradiction -- the library draws it.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithoutNumeric().InLowerCase().OneOf("1", "A", "a").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_name_a_constraint_that_refuses_no_value() {
        // NotBlank() admits both values, so it takes no part in emptying the pool: naming it would send the reader
        // to a constraint whose removal changes nothing. Only the two that actually refuse a value are named.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().NotBlank().WithoutNumeric().InLowerCase().OneOf("1", "A").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithoutNumeric() and InLowerCase()");
        Check.That(diagnostics[0].GetMessage()).Not.Contains("NotBlank()");
    }

    [Fact]
    public async Task Names_the_single_culprit_rather_than_the_conjunction_where_there_is_one() {
        // The pass that finds one constraint answering for the whole pool runs first, and its sentence is the more
        // actionable of the two: it names what to remove.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().InLowerCase().NotBlank().OneOf("A", "B").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("InLowerCase() allows none of the values it offers");
        Check.That(diagnostics[0].GetMessage()).Not.Contains("together");
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
                    return Any.String().OneOf("abc", "def").InUpperCase().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("InUpperCase()");
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
    public async Task Reports_a_blank_anchor_that_leaves_NotBlank_no_room() {
        // The anchor fills the declared length and carries no non-blank character, so the one NotBlank() requires has
        // nowhere to come from. Measured: the library refuses this chain at declaration.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith(" ").WithLength(1).NotBlank().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("NotBlank()");
        Check.That(diagnostics[0].GetMessage()).Contains("WithLength(1)");
    }

    [Fact]
    public async Task Does_not_report_NotBlank_beside_an_anchor_that_already_carries_one() {
        // The position is owed only where no anchor supplies the character. Adding it unconditionally would refuse
        // this chain, which the library honours and draws as "A".
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith("A").WithLength(1).NotBlank().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_blank_anchor_the_declared_length_still_fits() {
        // One character of filler is left, which is all NotBlank() asks for -- the library draws " ".
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith(" ").WithLength(2).NotBlank().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_fixed_length_that_leaves_NotBlank_no_room_at_all() {
        // No anchor, so the old budget bailed before it could look: the floor is owed by the constraint itself.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithLength(0).NotBlank().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("NotBlank() needs at least 1 character");
        Check.That(diagnostics[0].GetMessage()).Contains("WithLength(0)");
    }

    [Fact]
    public async Task Reports_a_cap_that_leaves_NonEmpty_no_room_at_all() {
        // The same floor from the other member, and the same silence before: NonEmpty() sets a minimum of one that
        // WithMaxLength(0) cannot hold, and the library refuses it at declaration.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithMaxLength(0).NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("NonEmpty() needs at least 1 character");
        Check.That(diagnostics[0].GetMessage()).Contains("WithMaxLength(0)");
    }

    [Fact]
    public async Task Does_not_report_a_repeated_affix_the_library_folds_into_one() {
        // A prefix and a suffix each own a single slot, so re-declaring the same literal is a no-op: this chain draws
        // "ORD-". Summing both declarations reported a chain the run time honours.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith("ORD-").StartingWith("ORD-").WithLength(4).Generate();
                }

                public static string N() {
                    return Any.String().EndingWith("-EUR").EndingWith("-EUR").WithLength(4).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_identical_fragment_the_library_folds_into_one() {
        // A Containing fragment redeclared identically is the same no-op every other redeclaration check applies:
        // this chain draws "XY" at a length of two, not "XYXY" -- summing both declarations reported a chain the
        // run time honours (issue #181).
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Containing("XY").Containing("XY").WithLength(2).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_two_different_fragments_that_genuinely_lengthen_the_value() {
        // The counterpart the identical-fragment fix must not flatten: Containing accumulates DISTINCT fragments, so
        // two different ones really do need four characters -- the library refuses this (issue #137, unaffected by
        // #181's fix).
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Containing("XY").Containing("YX").WithLength(2).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("at least 4 characters");
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

    [Theory]
    [InlineData("Any.String().WithMinLength(10).WithMaxLength(5)",                       "WithMinLength(10) needs at least 10 characters, which WithMaxLength(5) cannot hold")]
    [InlineData("Any.String().WithMaxLength(5).WithMinLength(10)",                       "WithMinLength(10) needs at least 10 characters, which WithMaxLength(5) cannot hold")]
    [InlineData("Any.String().WithMinLength(10).WithLength(5)",                          "WithMinLength(10) needs at least 10 characters, which WithLength(5) cannot hold")]
    [InlineData("Any.String().WithLength(5).WithMinLength(10)",                          "WithMinLength(10) needs at least 10 characters, which WithLength(5) cannot hold")]
    [InlineData("Any.String().WithLengthBetween(10, 20).WithLength(5)",                  "WithLengthBetween(10, 20) needs at least 10 characters, which WithLength(5) cannot hold")]
    [InlineData("Any.String().WithLengthBetween(1, 3).WithLength(5)",                    "WithLength(5) needs exactly 5 characters, which WithLengthBetween(1, 3) cannot hold")]
    [InlineData("Any.String().WithMaxLength(3).WithLength(5)",                           "WithLength(5) needs exactly 5 characters, which WithMaxLength(3) cannot hold")]
    [InlineData("Any.String().WithLength(5).WithMaxLength(3)",                           "WithLength(5) needs exactly 5 characters, which WithMaxLength(3) cannot hold")]
    [InlineData("Any.String().WithLengthBetween(10, 20).WithLengthBetween(1, 3)",        "WithLengthBetween(10, 20) needs at least 10 characters, which WithLengthBetween(1, 3) cannot hold")]
    [InlineData("Any.String().WithMinLength(4).WithMinLength(10).WithMaxLength(5)",      "WithMinLength(10) needs at least 10 characters, which WithMaxLength(5) cannot hold")]
    // A value set does not exempt the bounds: "abcdefghij" survives the minimum, and the maximum then contradicts it.
    [InlineData("Any.String().OneOf(\"abcdefghij\").WithMinLength(10).WithMaxLength(5)", "WithMinLength(10) needs at least 10 characters, which WithMaxLength(5) cannot hold")]
    public async Task Reports_two_declared_lengths_no_length_satisfies(string chain, string reason) {
        // Issue #225: the rule read a maximum and an exact length but never a minimum, never the range form, and never
        // compared the bounds with one another. The run time weighs them first, value set or not, whichever spelling
        // declared them, and refuses each of these at declaration.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{chain}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD015");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No string satisfies this chain: {reason}");
    }

    [Theory]
    [InlineData("Any.String().StartingWith(\"ORD-\").WithLengthBetween(1, 3)",         "the anchored fragments need at least 4 characters, which WithLengthBetween(1, 3) cannot hold")]
    [InlineData("Any.String().WithLengthBetween(0, 3).StartingWith(\"ORD-\")",         "the anchored fragments need at least 4 characters, which WithLengthBetween(0, 3) cannot hold")]
    [InlineData("Any.String().Containing(\"abcdef\").WithLengthBetween(0, 3)",         "the anchored fragments need at least 6 characters, which WithLengthBetween(0, 3) cannot hold")]
    [InlineData("Any.String().NotBlank().WithLengthBetween(0, 0)",                     "NotBlank() needs at least 1 character, which WithLengthBetween(0, 0) cannot hold")]
    [InlineData("Any.String().WithLengthBetween(0, 0).NotBlank()",                     "NotBlank() needs at least 1 character, which WithLengthBetween(0, 0) cannot hold")]
    [InlineData("Any.String().NonEmpty().WithLengthBetween(0, 0)",                     "NonEmpty() needs at least 1 character, which WithLengthBetween(0, 0) cannot hold")]
    // Under both, the exact length is the cap the fragments are measured against: it is the tighter of the two.
    [InlineData("Any.String().WithLength(3).WithMaxLength(10).StartingWith(\"ORD-\")", "the anchored fragments need at least 4 characters, which WithLength(3) cannot hold")]
    // Declared before a value set, the calls are laid out as a shaped string and refused as they are declared: the
    // value set that follows cannot rescue them, though "aba" carries both fragments in three characters.
    [InlineData("Any.String().WithLengthBetween(0, 3).Containing(\"ab\").Containing(\"ba\").OneOf(\"aba\")", "the anchored fragments need at least 4 characters, which WithLengthBetween(0, 3) cannot hold")]
    [InlineData("Any.String().WithLengthBetween(0, 0).NotBlank().OneOf(\"\")",                                  "NotBlank() needs at least 1 character, which WithLengthBetween(0, 0) cannot hold")]
    public async Task Reports_a_budget_the_range_form_cannot_hold(string chain, string reason) {
        // Issue #225: the same budget the single forms are held to, spelled as the range JD031 recommends.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{chain}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No string satisfies this chain: {reason}");
    }

    [Theory]
    [InlineData("Any.String().OneOf(\"EUR\", \"USD\").WithLength(2)",            "WithLength(2)")]
    [InlineData("Any.String().WithLength(2).OneOf(\"EUR\", \"USD\")",            "WithLength(2)")]
    [InlineData("Any.String().OneOf(\"EUR\").WithMinLength(5)",                  "WithMinLength(5)")]
    [InlineData("Any.String().OneOf(\"EUR\").WithMaxLength(2)",                  "WithMaxLength(2)")]
    [InlineData("Any.String().OneOf(\"EUR\").WithLengthBetween(4, 6)",           "WithLengthBetween(4, 6)")]
    [InlineData("Any.String().OneOf(\"\").NonEmpty()",                           "NonEmpty()")]
    [InlineData("Any.String().OneOf(\"EUR\").StartingWith(\"X\")",               "StartingWith(\"X\")")]
    [InlineData("Any.String().StartingWith(\"X\").OneOf(\"EUR\")",               "StartingWith(\"X\")")]
    [InlineData("Any.String().OneOf(\"EUR\").EndingWith(\"X\")",                 "EndingWith(\"X\")")]
    [InlineData("Any.String().OneOf(\"EUR\").Containing(\"Z\")",                 "Containing(\"Z\")")]
    [InlineData("Any.String().OneOf(\"EUR\").DifferentFrom(\"EUR\")",            "DifferentFrom(\"EUR\")")]
    [InlineData("Any.String().OneOf(\"EUR\", \"USD\").Except(\"EUR\", \"USD\")", "Except(\"EUR\", \"USD\")")]
    [InlineData("Any.String().Except(\"EUR\").OneOf(\"EUR\")",                   "Except(\"EUR\")")]
    [InlineData("Any.String().Alpha().OneOf(\"EUR\", \"US1\").WithLength(2)",    "WithLength(2)")]
    // A value set written as a collection expression is read like one written as arguments, by every rule alike.
    [InlineData("Any.String().OneOf([\"EUR\", \"USD\"]).WithLength(2)",          "WithLength(2)")]
    public async Task Reports_a_value_set_a_length_an_anchor_or_an_exclusion_empties(string chain, string culprit) {
        // Issue #225: the value-set branch tested character constraints only, while JD029 stood down on the premise
        // that this rule reports a pool nothing survives — so each rule left these to the other, and both were silent.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{chain}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No string satisfies this chain: {culprit} allows none of the values it offers");
    }

    [Theory]
    [InlineData("Any.String().WithMinLength(5).WithMaxLength(5)")]
    [InlineData("Any.String().WithLength(5).WithLengthBetween(5, 10)")]
    [InlineData("Any.String().WithLengthBetween(4, 6).StartingWith(\"ORD-\")")]
    [InlineData("Any.String().WithMinLength(10).StartingWith(\"ORD-\")")]
    [InlineData("Any.String().OneOf(\"EUR\", \"US\").WithLength(2)")]
    [InlineData("Any.String().OneOf(\"EUR\").WithLengthBetween(3, 3).Except(\"USD\")")]
    // A transposed range or a negative length throws an argument error of its own before any bound is set, and
    // JD014 reports it; read as bounds, they would make this rule claim a conflict the run time never reaches.
    [InlineData("Any.String().WithLengthBetween(10, 5)")]
    [InlineData("Any.String().WithMaxLength(-1)")]
    // A shaped string with a character left to draw: an anchor that carries a non-whitespace character settles
    // NotBlank() whatever the filler draws, and the whole of ASCII is never emptied by the two subtractions.
    [InlineData("Any.String().AlphaNumeric().WithoutAlpha()")]
    [InlineData("Any.String().Whitespaces().NotBlank().StartingWith(\"A\")")]
    [InlineData("Any.String().WithChars(\" a\").NotBlank()")]
    [InlineData("Any.String().WithoutAlpha().WithoutNumeric().NotBlank()")]
    // Before a value set, NotBlank() is not owed a position of its own — only the draw asks for one, and the draw sees
    // the value set — and after one, the calls are matched against the pooled values rather than laid out.
    [InlineData("Any.String().StartingWith(\" \").Containing(\" \").WithLength(2).NotBlank().OneOf(\" a\")")]
    [InlineData("Any.String().OneOf(\"\").Alpha().WithoutAlpha()")]
    public async Task Does_not_report_lengths_and_a_value_set_some_value_satisfies(string chain) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{chain}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_two_contradictory_lengths_on_the_later_of_the_two() {
        // The run time refuses the chain as it applies the later constraint, and names that one first.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithMinLength(10).WithMaxLength(5).NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(source.Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length)).IsEqualTo("Any.String().WithMinLength(10).WithMaxLength(5)");
    }

    [Theory]
    [InlineData("Any.String().Numeric().WithoutNumeric()",                    "WithoutNumeric() removes every character Numeric() offers")]
    [InlineData("Any.String().WithoutAlpha().Alpha()",                        "WithoutAlpha() removes every character Alpha() offers")]
    [InlineData("Any.String().Hexadecimal().WithoutNumeric().WithoutAlpha()", "WithoutNumeric() and WithoutAlpha() remove every character Hexadecimal() offers")]
    [InlineData("Any.String().WithChars(\"ab\").WithoutAlpha()",              "WithoutAlpha() removes every character WithChars(\"ab\") offers")]
    [InlineData("Any.String().Whitespaces().NotBlank()",                      "NotBlank() needs a character that is not whitespace, and Whitespaces() offers none")]
    [InlineData("Any.String().NotBlank().WithChars(\"\\t \")",                "NotBlank() needs a character that is not whitespace, and WithChars(\"\t \") offers none")]
    [InlineData("Any.String().WithChars(\" 1\").WithoutNumeric().NotBlank()",  "NotBlank() needs a character that is not whitespace, and WithoutNumeric() removes every one WithChars(\" 1\") offers")]
    // The same family declared twice is one family: the run time ignores the repetition, so the alphabet is as empty.
    [InlineData("Any.String().Numeric().Numeric().WithoutNumeric()",          "WithoutNumeric() removes every character Numeric() offers")]
    [InlineData("Any.String().Whitespaces().Whitespaces().NotBlank()",        "NotBlank() needs a character that is not whitespace, and Whitespaces() offers none")]
    // Emptied before a value set arrives: the run time refuses the subtraction there, and the value set never applies.
    [InlineData("Any.String().Alpha().WithoutAlpha().OneOf(\"\", \"a\")",     "WithoutAlpha() removes every character Alpha() offers")]
    public async Task Reports_a_shaped_string_left_with_no_character_to_draw(string chain, string reason) {
        // Found by the differential property of issue #232: the run time refuses each of these — at the call that
        // empties the alphabet, or at the draw for NotBlank() — and the rule read no character constraint on a shaped
        // string at all.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{chain}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"No string satisfies this chain: {reason}");
        // No single call is the one to remove — the family, a subtraction or NotBlank() would each do — so the report
        // sits on the chain as a whole.
        Check.That(source.Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length)).IsEqualTo($"{chain}.Generate()");
    }

    [Theory]
    [InlineData("Any.String().WithChars(\"abc\").InUpperCase()")]
    [InlineData("Any.String().InLowerCase().WithChars(\"AB\").NotBlank()")]
    [InlineData("Any.String().WithChars(\"\").WithoutAlpha()")]
    public async Task Stands_down_without_failing_on_a_pool_it_does_not_judge(string chain) {
        // A casing beside a WithChars pool is a conflict of declaration (issue #274), and an empty pool an argument
        // the call itself rejects: neither leaves an alphabet to judge. Read as one, the casing or the empty pool left
        // no character and no subtraction to blame, and the rule threw — which Roslyn surfaces as AD0001.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{chain}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).IsEmpty();
    }

    [Theory]
    [InlineData("StartingWith(anchor)")]
    [InlineData("EndingWith(anchor)")]
    [InlineData("Containing(anchor)")]
    public async Task Does_not_infer_a_blank_only_shape_from_an_anchor_it_cannot_read(string anchoring) {
        // Found in review: an anchor the compiler cannot resolve may carry the character NotBlank() asks for — this
        // chain draws whenever anchor is "A" — so the filler is not the only place that character can come from.
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M(string anchor) {
                    return Any.String().Whitespaces().NotBlank().{{anchoring}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).IsEmpty();
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
