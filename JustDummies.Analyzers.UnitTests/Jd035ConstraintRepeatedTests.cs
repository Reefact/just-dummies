using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd035ConstraintRepeatedTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using System;
            using System.Collections.Generic;

            using JustDummies;

            public static class Sample {
                public static void M(string anchor, int length, string[] values) {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new ConstraintRepeatedAnalyzer(), source);
    }

    private static async Task NothingReportedAsync(string chain) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        _ = {chain}.Generate();");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).IsEmpty();
    }

    private static string Message(string repeated, string first) {
        return $"{repeated} repeats {first}, declared before it, and changes nothing";
    }

    [Theory]
    // A constraint the generator takes once, declared again identically: the run time keeps the one it holds.
    [InlineData("Any.String().Alpha().Alpha()",                                "Alpha()", "Alpha()")]
    [InlineData("Any.String().WithLength(4).WithLength(4)",                    "WithLength(4)", "WithLength(4)")]
    [InlineData("Any.String().StartingWith(\"a\").StartingWith(\"a\")",       "StartingWith(\"a\")", "StartingWith(\"a\")")]
    [InlineData("Any.String().EndingWith(\"a\").EndingWith(\"a\")",           "EndingWith(\"a\")", "EndingWith(\"a\")")]
    [InlineData("Any.String().InUpperCase().InUpperCase()",                    "InUpperCase()", "InUpperCase()")]
    [InlineData("Any.String().WithChars(\"ab\").WithChars(\"ab\")",            "WithChars(\"ab\")", "WithChars(\"ab\")")]
    [InlineData("Any.Char().Alpha().Alpha()",                                  "Alpha()", "Alpha()")]
    [InlineData("Any.Char().InLowerCase().InLowerCase()",                      "InLowerCase()", "InLowerCase()")]
    [InlineData("Any.Int32().MultipleOf(5).MultipleOf(5)",                     "MultipleOf(5)", "MultipleOf(5)")]
    [InlineData("Any.Byte().MultipleOf(2).MultipleOf(2)",                      "MultipleOf(2)", "MultipleOf(2)")]
    [InlineData("Any.Decimal().WithScale(2).WithScale(2)",                     "WithScale(2)", "WithScale(2)")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(2).WithCount(2)",           "WithCount(2)", "WithCount(2)")]
    [InlineData("Any.ListOf(Any.Int32()).Empty().Empty()",                     "Empty()", "Empty()")]
    [InlineData("Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCount(2).WithCount(2)", "WithCount(2)", "WithCount(2)")]
    [InlineData("Any.Uri().Web().UsingHttps().UsingHttps()",                   "UsingHttps()", "UsingHttps()")]
    [InlineData("Any.Uri().Web().WithHost(\"a.example\").WithHost(\"a.example\")", "WithHost(\"a.example\")", "WithHost(\"a.example\")")]
    [InlineData("Any.Uri().Web().WithPort(80).WithPort(80)",                   "WithPort(80)", "WithPort(80)")]
    [InlineData("Any.Uri().Web().WithPort().WithPort()",                       "WithPort()", "WithPort()")]
    [InlineData("Any.Uri().Web().WithPathSegments(1).WithPathSegments(1)",     "WithPathSegments(1)", "WithPathSegments(1)")]
    [InlineData("Any.Uri().WebSocket().UsingWss().UsingWss()",                 "UsingWss()", "UsingWss()")]
    [InlineData("Any.Uri().Ftp().WithPort(21).WithPort(21)",                   "WithPort(21)", "WithPort(21)")]
    [InlineData("Any.Uri().Mailto().WithDomain(\"a.example\").WithDomain(\"a.example\")", "WithDomain(\"a.example\")", "WithDomain(\"a.example\")")]
    [InlineData("Any.Uri().Mailto().WithLocalPart(\"a\").WithLocalPart(\"a\")", "WithLocalPart(\"a\")", "WithLocalPart(\"a\")")]
    // The same declaration, however it is spelled: a value set by its values (ADR-0100), a granularity by its length,
    // an offset by its range, named arguments by their parameters.
    [InlineData("Any.String().OneOf(\"a\", \"b\").OneOf(\"b\", \"a\", \"a\")", "OneOf(\"b\", \"a\", \"a\")", "OneOf(\"a\", \"b\")")]
    [InlineData("Any.String().OneOf([\"a\"]).OneOf(\"a\")",                    "OneOf(\"a\")", "OneOf([\"a\"])")]
    [InlineData("Any.Int32().OneOf(1, 2).OneOf(2, 1)",                         "OneOf(2, 1)", "OneOf(1, 2)")]
    [InlineData("Any.Double().OneOf(1).OneOf(1.0)",                            "OneOf(1.0)", "OneOf(1)")]
    [InlineData("Any.Decimal().OneOf(1.0m).OneOf(1.00m)",                      "OneOf(1.00m)", "OneOf(1.0m)")]
    [InlineData("Any.Enum<DayOfWeek>().OneOf(DayOfWeek.Monday).OneOf(DayOfWeek.Monday)", "OneOf(DayOfWeek.Monday)", "OneOf(DayOfWeek.Monday)")]
    [InlineData("Any.StringMatching(\"[a-c]\").OneOf(\"a\").OneOf(\"a\")",    "OneOf(\"a\")", "OneOf(\"a\")")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromMinutes(60)).WithGranularity(TimeSpan.FromHours(1))", "WithGranularity(TimeSpan.FromHours(1))", "WithGranularity(TimeSpan.FromMinutes(60))")]
    [InlineData("Any.TimeSpan().WithGranularity(TimeSpan.FromSeconds(1)).WithGranularity(new TimeSpan(0, 0, 1))", "WithGranularity(new TimeSpan(0, 0, 1))", "WithGranularity(TimeSpan.FromSeconds(1))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(1)).WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))", "WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))", "WithOffset(TimeSpan.FromHours(1))")]
    [InlineData("Any.Uri().Web().WithUserInfo(password: \"p\", user: \"u\").WithUserInfo(\"u\", \"p\")", "WithUserInfo(\"u\", \"p\")", "WithUserInfo(password: \"p\", user: \"u\")")]
    // A call whose repetition changes nothing: an exclusion, a subtraction, a string's fragment, a toggle.
    [InlineData("Any.String().Containing(\"a\").Containing(\"a\")",           "Containing(\"a\")", "Containing(\"a\")")]
    [InlineData("Any.String().WithoutAlpha().WithoutAlpha()",                  "WithoutAlpha()", "WithoutAlpha()")]
    [InlineData("Any.String().WithoutNumeric().WithoutNumeric()",              "WithoutNumeric()", "WithoutNumeric()")]
    [InlineData("Any.String().NotBlank().NotBlank()",                          "NotBlank()", "NotBlank()")]
    [InlineData("Any.String().Except(\"a\", \"b\").Except(\"b\", \"a\")",     "Except(\"b\", \"a\")", "Except(\"a\", \"b\")")]
    [InlineData("Any.String().DifferentFrom(\"a\").DifferentFrom(\"a\")",     "DifferentFrom(\"a\")", "DifferentFrom(\"a\")")]
    [InlineData("Any.Char().WithoutAlpha().WithoutAlpha()",                    "WithoutAlpha()", "WithoutAlpha()")]
    [InlineData("Any.Char().Except('a').Except('a')",                          "Except('a')", "Except('a')")]
    [InlineData("Any.Int32().Except(5).Except(5)",                             "Except(5)", "Except(5)")]
    [InlineData("Any.Int32().Except(5, 6).Except(6, 5)",                       "Except(6, 5)", "Except(5, 6)")]
    [InlineData("Any.Int32().DifferentFrom(5).DifferentFrom(5)",               "DifferentFrom(5)", "DifferentFrom(5)")]
    [InlineData("Any.Int32().NonZero().NonZero()",                             "NonZero()", "NonZero()")]
    [InlineData("Any.Double().Except(5.0).Except(5.0)",                        "Except(5.0)", "Except(5.0)")]
    [InlineData("Any.Double().NonZero().NonZero()",                            "NonZero()", "NonZero()")]
    [InlineData("Any.Enum<DayOfWeek>().Except(DayOfWeek.Monday).Except(DayOfWeek.Monday)", "Except(DayOfWeek.Monday)", "Except(DayOfWeek.Monday)")]
    [InlineData("Any.Enum<AttributeTargets>().AllowingCombinations().AllowingCombinations()", "AllowingCombinations()", "AllowingCombinations()")]
    [InlineData("Any.StringMatching(\"[a-c]\").Except(\"a\").Except(\"a\")",  "Except(\"a\")", "Except(\"a\")")]
    [InlineData("Any.OneOf(1, 2, 3).Except(1).Except(1)",                      "Except(1)", "Except(1)")]
    [InlineData("Any.Guid().NonEmpty().NonEmpty()",                            "NonEmpty()", "NonEmpty()")]
    [InlineData("Any.Guid().Empty().Empty()",                                  "Empty()", "Empty()")]
    [InlineData("Any.Boolean().True().True()",                                 "True()", "True()")]
    [InlineData("Any.Boolean().DifferentFrom(true).DifferentFrom(true)",       "DifferentFrom(true)", "DifferentFrom(true)")]
    [InlineData("Any.ListOf(Any.Int32()).Distinct().Distinct()",               "Distinct()", "Distinct()")]
    [InlineData("Any.Uri().Web().WithQuery().WithQuery()",                     "WithQuery()", "WithQuery()")]
    [InlineData("Any.Uri().Web().WithFragment().WithFragment()",               "WithFragment()", "WithFragment()")]
    [InlineData("Any.Uri().Relative().Rooted().Rooted()",                      "Rooted()", "Rooted()")]
    [InlineData("Any.Uri().Mailto().WithHeaders().WithHeaders()",              "WithHeaders()", "WithHeaders()")]
    // Calls between the two do not separate them.
    [InlineData("Any.String().WithLength(4).Alpha().NotBlank().WithLength(4)", "WithLength(4)", "WithLength(4)")]
    // A value one exclusion list already holds is the same exclusion declared twice, in one call (issue #280).
    [InlineData("Any.String().Except(\"a\", \"a\")",                       "\"a\"", "\"a\"")]
    [InlineData("Any.String().Except(\"a\", \"b\", \"a\")",              "\"a\"", "\"a\"")]
    [InlineData("Any.String().Except([\"a\", \"a\"])",                     "\"a\"", "\"a\"")]
    [InlineData("Any.Char().Except('a', 'a')",                                 "'a'", "'a'")]
    [InlineData("Any.Int32().Except(5, 5)",                                    "5", "5")]
    [InlineData("Any.Double().Except(1, 1.0)",                                 "1.0", "1")]
    [InlineData("Any.Enum<DayOfWeek>().Except(DayOfWeek.Monday, DayOfWeek.Monday)", "DayOfWeek.Monday", "DayOfWeek.Monday")]
    [InlineData("Any.OneOf(1, 2, 3).Except(1, 1)",                             "1", "1")]
    public async Task Reports_a_constraint_declared_again_identically(string chain, string repeated, string first) {
        // Issue #279: the run time ignores each second call, so the chain draws exactly what it would without it, and
        // no rule reported it.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        _ = {chain}.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD035");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo(Message(repeated, first));
    }

    [Fact]
    public async Task Reports_on_the_repeated_call_alone() {
        // The diagnostic covers the call that changes nothing, which is the one to delete.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().Alpha().WithLength(4).Alpha().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length))
             .IsEqualTo("Alpha()");
        Check.That(diagnostics[0].Location.SourceSpan.Start).IsStrictlyGreaterThan(diagnostics[0].Location.SourceTree!.ToString().IndexOf("WithLength", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reports_a_value_listed_twice_on_the_value_and_a_repeated_list_on_the_call() {
        // Each is a repetition of its own: the second "a" in the first list — the one followed by the closing
        // parenthesis — then the whole second call.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().Except(\"a\", \"a\").Except(\"a\").Generate();");

        Check.That(diagnostics.Select(diagnostic => diagnostic.GetMessage())).ContainsExactly(
            Message("\"a\"", "\"a\""), Message("Except(\"a\")", "Except(\"a\", \"a\")"));
        Check.That(diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length + 1))
             .IsEqualTo("\"a\")");
    }

    [Fact]
    public async Task Reports_every_repetition() {
        // Unlike a conflict, a repetition stops nothing: the calls after it are applied, and each one written again is
        // a call to delete.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().Alpha().Alpha().WithLength(4).WithLength(4).Alpha().Generate();");

        Check.That(diagnostics.Select(diagnostic => diagnostic.GetMessage())).ContainsExactly(
            Message("Alpha()", "Alpha()"), Message("WithLength(4)", "WithLength(4)"), Message("Alpha()", "Alpha()"));
    }

    [Theory]
    // A different declaration is JD034's; the run time rejects it rather than ignoring it.
    [InlineData("Any.String().Alpha().Numeric()")]
    [InlineData("Any.String().WithLength(2).WithLength(3)")]
    // Bounds and their aliases fold rather than repeat: JD032 and JD024 own them (ADR-0078).
    [InlineData("Any.String().WithMinLength(4).WithMinLength(4)")]
    [InlineData("Any.String().WithLengthBetween(2, 5).WithLengthBetween(2, 5)")]
    [InlineData("Any.String().NonEmpty().NonEmpty()")]
    [InlineData("Any.Int32().Positive().Positive()")]
    [InlineData("Any.Int32().Zero().Zero()")]
    [InlineData("Any.Int32().Between(1, 9).Between(1, 9)")]
    [InlineData("Any.Double().Positive().Positive()")]
    [InlineData("Any.ListOf(Any.Int32()).NonEmpty().NonEmpty()")]
    // Not the same call, even where it reaches the same values.
    [InlineData("Any.String().Except(\"a\", \"b\").Except(\"a\")")]
    [InlineData("Any.String().Containing(\"a\").Containing(\"b\")")]
    [InlineData("Any.Int32().Except(5).DifferentFrom(5)")]
    [InlineData("Any.Int32().NonZero().Except(0)")]
    // A collection's contained element is required again, not repeated: a list draws it twice.
    [InlineData("Any.ListOf(Any.Int32()).WithCount(3).Containing(1).Containing(1)")]
    // A call that narrows nothing fills no slot, so there is nothing to repeat.
    [InlineData("Any.Int32().MultipleOf(1).MultipleOf(1)")]
    // A value listed twice in a value set reads as a weighting, which is JD025's to refuse.
    [InlineData("Any.String().OneOf(\"a\", \"a\")")]
    public async Task Does_not_report_what_is_not_a_repetition(string chain) {
        await NothingReportedAsync(chain);
    }

    [Theory]
    [InlineData("Any.String().StartingWith(anchor).StartingWith(anchor)")]
    [InlineData("Any.String().OneOf(values).OneOf(values)")]
    [InlineData("Any.String().Except(values).Except(values)")]
    [InlineData("Any.String().DifferentFrom(anchor).DifferentFrom(anchor)")]
    [InlineData("Any.Guid().DifferentFrom(Guid.Empty).DifferentFrom(Guid.Empty)")]
    [InlineData("Any.DateTime().Except(DateTime.MinValue).Except(DateTime.MinValue)")]
    [InlineData("Any.ListOf(Any.String()).Distinct(StringComparer.Ordinal).Distinct(StringComparer.Ordinal)")]
    [InlineData("Any.String().Except(\"a\", anchor, \"a\")")]
    public async Task Does_not_guess_about_an_argument_it_cannot_read(string chain) {
        // An argument the compiler cannot resolve to a constant may hold another value on the second call, and a
        // comparer is compared by reference: the rule stays silent rather than guess.
        await NothingReportedAsync(chain);
    }

    [Theory]
    // Nothing after a conflict or a rejected argument is applied, so nothing there repeats anything.
    [InlineData("Any.String().Alpha().Numeric().WithLength(2).WithLength(2)")]
    [InlineData("Any.String().WithMinLength(-1).Alpha().Alpha()")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(-1).Distinct().Distinct()")]
    public async Task Does_not_report_past_a_call_the_run_time_refuses(string chain) {
        await NothingReportedAsync(chain);
    }

}
