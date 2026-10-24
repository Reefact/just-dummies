using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd034ConstraintAlreadyDefinedTests {

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

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new ConstraintAlreadyDefinedAnalyzer(), source);
    }

    private static async Task NothingReportedAsync(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).IsEmpty();
    }

    [Theory]
    [InlineData("Any.String().Alpha().Numeric()",                           "Numeric() conflicts with Alpha(), declared before it: a string takes one character family or pool")]
    [InlineData("Any.String().Alpha().AlphaNumeric()",                      "AlphaNumeric() conflicts with Alpha(), declared before it: a string takes one character family or pool")]
    [InlineData("Any.String().Alpha().WithChars(\"ab\")",                   "WithChars(\"ab\") conflicts with Alpha(), declared before it: a string takes one character family or pool")]
    [InlineData("Any.String().WithChars(\"ab\").WithChars(\"ba\")",         "WithChars(\"ba\") conflicts with WithChars(\"ab\"), declared before it: a string takes one character family or pool")]
    [InlineData("Any.String().StartingWith(\"a\").StartingWith(\"b\")",     "StartingWith(\"b\") conflicts with StartingWith(\"a\"), declared before it: a string takes one prefix")]
    [InlineData("Any.String().EndingWith(\"a\").EndingWith(\"b\")",         "EndingWith(\"b\") conflicts with EndingWith(\"a\"), declared before it: a string takes one suffix")]
    [InlineData("Any.String().WithLength(2).WithLength(3)",                 "WithLength(3) conflicts with WithLength(2), declared before it: a string takes one exact length")]
    [InlineData("Any.String().InUpperCase().InLowerCase()",                 "InLowerCase() conflicts with InUpperCase(), declared before it: a string takes one casing")]
    [InlineData("Any.String().OneOf(\"a\").OneOf(\"b\")",                   "OneOf(\"b\") conflicts with OneOf(\"a\"), declared before it: a string takes one value set")]
    [InlineData("Any.String().WithChars(\"ab\").InUpperCase()",             "InUpperCase() conflicts with WithChars(\"ab\"), declared before it: a WithChars pool is the whole character definition, so it takes no casing")]
    [InlineData("Any.String().InUpperCase().WithChars(\"AB\")",             "WithChars(\"AB\") conflicts with InUpperCase(), declared before it: a WithChars pool is the whole character definition, so it takes no casing")]
    // Calls between the two do not separate them: the slot is the generator's, not the neighbour's.
    [InlineData("Any.String().WithLength(2).Alpha().NotBlank().WithLength(3)", "WithLength(3) conflicts with WithLength(2), declared before it: a string takes one exact length")]
    public async Task Reports_a_once_per_generator_constraint_declared_again_differently(string chain, string message) {
        // Issue #274: the run time refuses each of these at the second call, on every seed, as already defined — and
        // no rule reported them.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        _ = {chain}.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD034");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo(message);
    }

    [Fact]
    public async Task Reports_on_the_call_the_run_time_refuses() {
        // The exception is thrown at the second declaration, so that is where the diagnostic sits — and only the first
        // conflict is reported, since nothing after it is ever applied.
        const string chain = "Any.String().Alpha().Numeric().WithLength(2).WithLength(3)";
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        _ = {chain}.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length))
             .IsEqualTo("Any.String().Alpha().Numeric()");
    }

    [Theory]
    // The same call repeated is a no-op the run time ignores.
    [InlineData("Any.String().Alpha().Alpha()")]
    [InlineData("Any.String().WithChars(\"ab\").WithChars(\"ab\")")]
    [InlineData("Any.String().StartingWith(\"a\").StartingWith(\"a\")")]
    [InlineData("Any.String().WithLength(2).WithLength(2)")]
    [InlineData("Any.String().InUpperCase().Alpha().InUpperCase()")]
    // A value set is the same when it lists the same values, in any order and with any repetition (ADR-0100).
    [InlineData("Any.String().OneOf(\"a\", \"b\").OneOf(\"b\", \"a\", \"a\")")]
    // A casing beside a named family is legal; only a WithChars pool refuses one.
    [InlineData("Any.String().Alpha().InUpperCase()")]
    // Bounds fold and fragments accumulate: neither is declared once per generator, and JD032 owns a bound written twice.
    [InlineData("Any.String().WithMinLength(2).WithMinLength(3)")]
    [InlineData("Any.String().Containing(\"a\").Containing(\"b\")")]
    [InlineData("Any.String().WithoutAlpha().WithoutNumeric()")]
    public async Task Does_not_report_a_repetition_or_a_constraint_that_accumulates(string chain) {
        await NothingReportedAsync($"_ = {chain}.Generate();");
    }

    [Theory]
    [InlineData("Any.String().StartingWith(anchor).StartingWith(\"a\")")]
    [InlineData("Any.String().WithLength(length).WithLength(2)")]
    [InlineData("Any.String().WithChars(anchor).WithChars(\"ab\")")]
    [InlineData("Any.String().OneOf(values).OneOf(\"a\")")]
    [InlineData("Any.String().OneOf(\"a\", anchor).OneOf(\"a\")")]
    public async Task Does_not_guess_about_an_argument_it_cannot_read(string chain) {
        // Equal at run time, these are a no-op; different, a conflict. The compiler cannot tell which, so neither can
        // the rule.
        await NothingReportedAsync($"_ = {chain}.Generate();");
    }

    [Fact]
    public async Task Reports_a_rival_call_whatever_its_argument() {
        // A different method in the slot is a conflict whatever its argument says: Alpha() and WithChars(anything)
        // never declare the same thing, and a casing refuses any pool.
        ImmutableArray<Diagnostic> family = await AnalyzeAsync("        _ = Any.String().Alpha().WithChars(anchor).Generate();");
        ImmutableArray<Diagnostic> casing = await AnalyzeAsync("        _ = Any.String().WithChars(anchor).InLowerCase().Generate();");

        Check.That(family.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD034");
        Check.That(casing.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD034");
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        await NothingReportedAsync("static void ThatCode(Func<object> code) { } ThatCode(() => Any.String().Alpha().Numeric());");
    }

    [Fact]
    public async Task Does_not_report_another_generator() {
        // Strings first (issue #274): the other generators' once-per-generator constraints are not read yet.
        await NothingReportedAsync("_ = Any.Char().Alpha().Numeric().Generate();");
    }

}
