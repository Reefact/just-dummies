using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     The library and <c>StringShapeFacts</c> each work out how few characters a chain can draw, and this is what
///     stops the two from drifting apart.
/// </summary>
/// <remarks>
///     <para>
///         <c>JD015</c> and <c>JD030</c> read that floor from one place, so those two cannot disagree with each
///         other. What they can disagree with is the generator: the rules reason over syntax at build time and
///         <c>StringSpec</c> lays a value out at run time, so the arithmetic is written twice. Drift refuses at
///         build time a chain the run time honours, or names an interval carrying values it can never produce —
///         both of which this repository has already shipped once.
///     </para>
///     <para>
///         Neither side names the number. The library's floor is found by asking what the tightest ceiling it will
///         still draw under is; the rule's by reading the interval it reports. The assertion is only that the two
///         are the same floor.
///     </para>
/// </remarks>
public sealed class StringFloorAgreementTests {

    /// <summary>Above every floor the shapes below reach, so a search that ran away is a failure rather than a pass.</summary>
    private const int PastEveryShapesFloor = 16;

    /// <summary>
    ///     Each chain twice: once as the source the rule reads, once as the calls the library runs. The pairs are
    ///     chosen to exercise the arithmetic's own distinctions — an affix that folds, a fragment that accumulates,
    ///     and the filler position <c>NotBlank</c> is owed only where no anchor already carries a non-blank
    ///     character.
    /// </summary>
    private static readonly (string Text, Func<int, string> DrawUnder)[] Chains = [
        ("Dummy.String()", cap => Dummy.String().WithMaxLength(cap).Generate()),
        ("Dummy.String().NonEmpty()", cap => Dummy.String().NonEmpty().WithMaxLength(cap).Generate()),
        ("Dummy.String().NotBlank()", cap => Dummy.String().NotBlank().WithMaxLength(cap).Generate()),
        ("Dummy.String().StartingWith(\"hello\")", cap => Dummy.String().StartingWith("hello").WithMaxLength(cap).Generate()),
        ("Dummy.String().StartingWith(\"A\").StartingWith(\"A\")", cap => Dummy.String().StartingWith("A").StartingWith("A").WithMaxLength(cap).Generate()),
        ("Dummy.String().EndingWith(\"Z\").EndingWith(\"Z\")", cap => Dummy.String().EndingWith("Z").EndingWith("Z").WithMaxLength(cap).Generate()),
        ("Dummy.String().Containing(\"XY\").Containing(\"XY\")", cap => Dummy.String().Containing("XY").Containing("XY").WithMaxLength(cap).Generate()),
        ("Dummy.String().StartingWith(\"ab\").EndingWith(\"cd\")", cap => Dummy.String().StartingWith("ab").EndingWith("cd").WithMaxLength(cap).Generate()),
        ("Dummy.String().StartingWith(\" \").NotBlank()", cap => Dummy.String().StartingWith(" ").NotBlank().WithMaxLength(cap).Generate()),
        ("Dummy.String().StartingWith(\"A\").NotBlank()", cap => Dummy.String().StartingWith("A").NotBlank().WithMaxLength(cap).Generate())
    ];

    [Fact(DisplayName = "JD030 reports the floor the library actually enforces, for every shape of the arithmetic.")]
    public async Task TheRuleReportsTheFloorTheLibraryEnforces() {
        foreach ((string text, Func<int, string> drawUnder) in Chains) {
            int libraryFloor = TightestCeilingTheLibraryDrawsUnder(text, drawUnder);
            int ruleFloor    = await FloorTheRuleReports(text);

            Check.WithCustomMessage($"{text}: the library draws under a ceiling of {libraryFloor} and no lower, while JD030 reports a floor of {ruleFloor}.")
                 .That(ruleFloor).IsEqualTo(libraryFloor);
        }
    }

    /// <summary>
    ///     The tightest <c>WithMaxLength</c> the chain still draws under — which is the fewest characters it can
    ///     produce, found by asking rather than by naming it.
    /// </summary>
    private static int TightestCeilingTheLibraryDrawsUnder(string text, Func<int, string> drawUnder) {
        for (int cap = 0; cap <= PastEveryShapesFloor; cap++) {
            try {
                drawUnder(cap);

                return cap;
            } catch (ConflictingDummyConstraintException) {
                // Too tight for this shape; the next ceiling up is the question.
            }
        }

        throw new InvalidOperationException($"{text} drew under no ceiling up to {PastEveryShapesFloor}, so there is no floor to compare.");
    }

    /// <summary>The floor JD030 names, read out of the interval it reports rather than out of the rule's code.</summary>
    private static async Task<int> FloorTheRuleReports(string text) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return {{text}}.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UndeclaredStringLengthAnalyzer(), source);

        Check.WithCustomMessage($"{text}: JD030 reported nothing, so there is no floor to compare.").That(diagnostics.Length).IsEqualTo(1);

        Match interval = Regex.Match(diagnostics[0].GetMessage(), @"it draws (\d+) to \d+ characters");

        Check.WithCustomMessage($"{text}: JD030's message no longer states an interval — \"{diagnostics[0].GetMessage()}\".").That(interval.Success).IsTrue();

        return int.Parse(interval.Groups[1].Value, CultureInfo.InvariantCulture);
    }

}
