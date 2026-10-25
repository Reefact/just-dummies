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
    [InlineData("Any.String().Alpha().Numeric()",                           "Numeric() conflicts with Alpha(), declared before it: the generator accepts only one character family or pool")]
    [InlineData("Any.String().Alpha().AlphaNumeric()",                      "AlphaNumeric() conflicts with Alpha(), declared before it: the generator accepts only one character family or pool")]
    [InlineData("Any.String().Alpha().WithChars(\"ab\")",                   "WithChars(\"ab\") conflicts with Alpha(), declared before it: the generator accepts only one character family or pool")]
    [InlineData("Any.String().WithChars(\"ab\").WithChars(\"ba\")",         "WithChars(\"ba\") conflicts with WithChars(\"ab\"), declared before it: the generator accepts only one character family or pool")]
    [InlineData("Any.String().StartingWith(\"a\").StartingWith(\"b\")",     "StartingWith(\"b\") conflicts with StartingWith(\"a\"), declared before it: the generator accepts only one prefix")]
    [InlineData("Any.String().EndingWith(\"a\").EndingWith(\"b\")",         "EndingWith(\"b\") conflicts with EndingWith(\"a\"), declared before it: the generator accepts only one suffix")]
    [InlineData("Any.String().WithLength(2).WithLength(3)",                 "WithLength(3) conflicts with WithLength(2), declared before it: the generator accepts only one exact length")]
    [InlineData("Any.String().InUpperCase().InLowerCase()",                 "InLowerCase() conflicts with InUpperCase(), declared before it: the generator accepts only one casing")]
    [InlineData("Any.String().OneOf(\"a\").OneOf(\"b\")",                   "OneOf(\"b\") conflicts with OneOf(\"a\"), declared before it: the generator accepts only one value set")]
    // A value set written as a collection expression is read like one written as arguments.
    [InlineData("Any.String().OneOf([\"a\"]).OneOf([\"b\"])",               "OneOf([\"b\"]) conflicts with OneOf([\"a\"]), declared before it: the generator accepts only one value set")]
    [InlineData("Any.String().OneOf(\"a\", \"b\").OneOf([\"a\", \"c\"])",     "OneOf([\"a\", \"c\"]) conflicts with OneOf(\"a\", \"b\"), declared before it: the generator accepts only one value set")]
    [InlineData("Any.String().WithChars(\"ab\").InUpperCase()",             "InUpperCase() conflicts with WithChars(\"ab\"), declared before it: a WithChars pool completely defines the allowed characters and cannot be combined with a casing constraint")]
    [InlineData("Any.String().InUpperCase().WithChars(\"AB\")",             "WithChars(\"AB\") conflicts with InUpperCase(), declared before it: a WithChars pool completely defines the allowed characters and cannot be combined with a casing constraint")]
    // Calls between the two do not separate them: the slot is the generator's, not the neighbour's.
    [InlineData("Any.String().WithLength(2).Alpha().NotBlank().WithLength(3)", "WithLength(3) conflicts with WithLength(2), declared before it: the generator accepts only one exact length")]
    // Every other generator holds its own once-only constraints the same way (issue #278).
    [InlineData("Any.Char().Alpha().Numeric()",                             "Numeric() conflicts with Alpha(), declared before it: the generator accepts only one character family")]
    [InlineData("Any.Char().InUpperCase().InLowerCase()",                   "InLowerCase() conflicts with InUpperCase(), declared before it: the generator accepts only one casing")]
    [InlineData("Any.Char().OneOf('a').OneOf('b')",                         "OneOf('b') conflicts with OneOf('a'), declared before it: the generator accepts only one value set")]
    [InlineData("Any.Int32().OneOf(1).OneOf(2)",                            "OneOf(2) conflicts with OneOf(1), declared before it: the generator accepts only one value set")]
    [InlineData("Any.Double().OneOf(1.5).OneOf(2.5)",                       "OneOf(2.5) conflicts with OneOf(1.5), declared before it: the generator accepts only one value set")]
    [InlineData("Any.Enum<DayOfWeek>().OneOf(DayOfWeek.Monday).OneOf(DayOfWeek.Friday)", "OneOf(DayOfWeek.Friday) conflicts with OneOf(DayOfWeek.Monday), declared before it: the generator accepts only one value set")]
    [InlineData("Any.StringMatching(\"[a-z]\").OneOf(\"a\").OneOf(\"b\")", "OneOf(\"b\") conflicts with OneOf(\"a\"), declared before it: the generator accepts only one value set")]
    [InlineData("Any.Int32().MultipleOf(5).MultipleOf(3)",                  "MultipleOf(3) conflicts with MultipleOf(5), declared before it: the generator accepts only one MultipleOf constraint")]
    [InlineData("Any.Byte().MultipleOf(2).MultipleOf(4)",                   "MultipleOf(4) conflicts with MultipleOf(2), declared before it: the generator accepts only one MultipleOf constraint")]
    [InlineData("Any.Decimal().WithScale(2).WithScale(3)",                  "WithScale(3) conflicts with WithScale(2), declared before it: the generator accepts only one scale")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromMinutes(1))", "WithGranularity(TimeSpan.FromMinutes(1)) conflicts with WithGranularity(TimeSpan.FromHours(1)), declared before it: the generator accepts only one granularity")]
    [InlineData("Any.TimeSpan().WithGranularity(TimeSpan.FromSeconds(1)).WithGranularity(new TimeSpan(0, 0, 2))", "WithGranularity(new TimeSpan(0, 0, 2)) conflicts with WithGranularity(TimeSpan.FromSeconds(1)), declared before it: the generator accepts only one granularity")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(1)).WithOffset(TimeSpan.FromHours(2))", "WithOffset(TimeSpan.FromHours(2)) conflicts with WithOffset(TimeSpan.FromHours(1)), declared before it: the generator accepts only one offset")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(1)).WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2))", "WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2)) conflicts with WithOffset(TimeSpan.FromHours(1)), declared before it: the generator accepts only one offset")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(2).WithCount(3)",        "WithCount(3) conflicts with WithCount(2), declared before it: the generator accepts only one exact count")]
    // Empty() is an exact count of zero, yet the run time compares the calls: WithCount(0) after it is a second declaration.
    [InlineData("Any.ListOf(Any.Int32()).Empty().WithCount(0)",             "WithCount(0) conflicts with Empty(), declared before it: the generator accepts only one exact count")]
    [InlineData("Any.DictionaryOf(Any.Int32(), Any.Int32()).Empty().WithCount(0)", "WithCount(0) conflicts with Empty(), declared before it: the generator accepts only one exact count")]
    [InlineData("Any.Uri().Web().UsingHttp().UsingHttps()",                 "UsingHttps() conflicts with UsingHttp(), declared before it: the generator accepts only one scheme")]
    [InlineData("Any.Uri().Web().WithHost(\"a.example\").WithHost(\"b.example\")", "WithHost(\"b.example\") conflicts with WithHost(\"a.example\"), declared before it: the generator accepts only one host")]
    [InlineData("Any.Uri().Web().WithPort(80).WithPort(8080)",              "WithPort(8080) conflicts with WithPort(80), declared before it: the generator accepts only one port")]
    [InlineData("Any.Uri().Web().WithPort().WithPort(80)",                  "WithPort(80) conflicts with WithPort(), declared before it: the generator accepts only one port")]
    [InlineData("Any.Uri().Web().WithUserInfo().WithUserInfo(\"a\")",     "WithUserInfo(\"a\") conflicts with WithUserInfo(), declared before it: the generator accepts only one user info")]
    [InlineData("Any.Uri().Web().WithPathSegments(1).WithoutPath()",        "WithoutPath() conflicts with WithPathSegments(1), declared before it: the generator accepts only one path")]
    [InlineData("Any.Uri().WebSocket().UsingWs().UsingWss()",               "UsingWss() conflicts with UsingWs(), declared before it: the generator accepts only one scheme")]
    [InlineData("Any.Uri().Ftp().WithPort(21).WithPort(22)",                "WithPort(22) conflicts with WithPort(21), declared before it: the generator accepts only one port")]
    [InlineData("Any.Uri().Mailto().WithDomain(\"a.example\").WithDomain(\"b.example\")", "WithDomain(\"b.example\") conflicts with WithDomain(\"a.example\"), declared before it: the generator accepts only one domain")]
    [InlineData("Any.Uri().Mailto().WithLocalPart(\"a\").WithLocalPart(\"b\")", "WithLocalPart(\"b\") conflicts with WithLocalPart(\"a\"), declared before it: the generator accepts only one local part")]
    // An empty local part passes its guard, so it reaches the slot.
    [InlineData("Any.Uri().Mailto().WithLocalPart(\"a\").WithLocalPart(\"\")", "WithLocalPart(\"\") conflicts with WithLocalPart(\"a\"), declared before it: the generator accepts only one local part")]
    [InlineData("Any.Uri().Relative().WithPathSegments(1).WithPathSegments(2)", "WithPathSegments(2) conflicts with WithPathSegments(1), declared before it: the generator accepts only one path")]
    public async Task Reports_a_once_per_generator_constraint_declared_again_differently(string chain, string message) {
        // Issues #274 and #278: the run time refuses each of these at the second call, on every seed, as already
        // defined — and no rule reported them.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        _ = {chain}.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD034");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo(message);
    }

    [Fact]
    public async Task Reports_on_the_call_the_run_time_refuses() {
        // The exception is thrown at the second declaration, so the diagnostic covers that call alone — not the chain up
        // to it, which on a long chain would hide which declaration is wrong. Only the first conflict is reported, since
        // nothing after it is ever applied.
        const string chain = "Any.String().Alpha().Numeric().WithLength(2).WithLength(3)";
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        _ = {chain}.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length))
             .IsEqualTo("Numeric()");
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
    [InlineData("Any.String().OneOf([\"a\", \"b\"]).OneOf([\"b\", \"a\"])")]
    [InlineData("Any.String().OneOf(\"a\", \"b\").OneOf([\"b\", \"a\", \"a\"])")]
    // A casing beside a named family is legal; only a WithChars pool refuses one.
    [InlineData("Any.String().Alpha().InUpperCase()")]
    // Bounds fold and fragments accumulate: neither is declared once per generator, and JD032 owns a bound written twice.
    [InlineData("Any.String().WithMinLength(2).WithMinLength(3)")]
    [InlineData("Any.String().Containing(\"a\").Containing(\"b\")")]
    [InlineData("Any.String().WithoutAlpha().WithoutNumeric()")]
    [InlineData("Any.Uri().Web().UsingHttps().UsingHttps()")]
    [InlineData("Any.Uri().Web().WithPort(80).WithPort(80)")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(2).WithCount(2)")]
    [InlineData("Any.ListOf(Any.Int32()).Empty().Empty()")]
    [InlineData("Any.Char().OneOf('a', 'b').OneOf('b', 'a')")]
    [InlineData("Any.Char().Alpha().InUpperCase()")]
    // A value set holds values of the generator's own type, which is what the run time compares.
    [InlineData("Any.Double().OneOf(1).OneOf(1.0)")]
    [InlineData("Any.Double().OneOf(1.0).OneOf([1])")]
    [InlineData("Any.Double().OneOf(0.0).OneOf(-0.0)")]
    [InlineData("Any.Decimal().OneOf(1.0m).OneOf(1.00m)")]
    // A granularity is its length, and an offset the range it declares, whatever spelling wrote them.
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromMinutes(60)).WithGranularity(TimeSpan.FromHours(1))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromMinutes(60)).WithOffset(new TimeSpan(1, 0, 0))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(1)).WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(maximum: TimeSpan.FromHours(2), minimum: TimeSpan.FromHours(1)).WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2))")]
    [InlineData("Any.Uri().Web().WithUserInfo(password: \"p\", user: \"u\").WithUserInfo(\"u\", \"p\")")]
    // A multiple of one, or a granularity of one tick, narrows nothing: the run time ignores it and fills no slot.
    [InlineData("Any.Int32().MultipleOf(1).MultipleOf(5)")]
    [InlineData("Any.Int32().MultipleOf(5).MultipleOf(1)")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromTicks(1)).WithGranularity(TimeSpan.FromHours(1))")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromTicks(1))")]
    public async Task Does_not_report_a_repetition_or_a_constraint_that_accumulates(string chain) {
        await NothingReportedAsync($"_ = {chain}.Generate();");
    }

    [Theory]
    [InlineData("Any.String().StartingWith(anchor).StartingWith(\"a\")")]
    [InlineData("Any.String().WithLength(length).WithLength(2)")]
    [InlineData("Any.String().WithChars(anchor).WithChars(\"ab\")")]
    [InlineData("Any.String().OneOf(values).OneOf(\"a\")")]
    [InlineData("Any.String().OneOf([.. values]).OneOf([\"a\"])")]
    [InlineData("Any.String().OneOf(\"a\", anchor).OneOf(\"a\")")]
    [InlineData("Any.Int32().MultipleOf(length).MultipleOf(5)")]
    [InlineData("Any.Guid().OneOf(Guid.Empty).OneOf(Guid.Empty)")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromHours(length)).WithGranularity(TimeSpan.FromHours(1))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(length)).WithOffset(TimeSpan.FromHours(1))")]
    // A fractional TimeSpan is not read: its rounding has changed between .NET versions (ADR-0046).
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromHours(1.5)).WithGranularity(TimeSpan.FromMinutes(90))")]
    // A host other than a plain DNS name is checked by System.Uri at run time, which the rule does not replicate.
    [InlineData("Any.Uri().Web().WithHost(\"1.2.3.4\").WithHost(\"a.example\")")]
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

    [Theory]
    // The refused call is the second declaration: its guard throws before it reads the first.
    [InlineData("Any.ListOf(Any.Int32()).WithCount(2).WithCount(-1)")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(2).WithCount(1000001)")]
    [InlineData("Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCount(2).WithCount(-1)")]
    [InlineData("Any.DictionaryOf(Any.Int32(), Any.Int32()).Empty().WithCount(1000001)")]
    [InlineData("Any.String().WithLength(2).WithLength(-1)")]
    [InlineData("Any.String().StartingWith(\"a\").StartingWith(\"\")")]
    [InlineData("Any.String().StartingWith(\"a\").StartingWith(null)")]
    [InlineData("Any.String().Alpha().WithChars(\"\")")]
    [InlineData("Any.Int32().OneOf(1).OneOf()")]
    [InlineData("Any.Int32().MultipleOf(2).MultipleOf(0)")]
    [InlineData("Any.Double().OneOf(1.5).OneOf(double.NaN)")]
    [InlineData("Any.Enum<DayOfWeek>().OneOf(DayOfWeek.Monday).OneOf((DayOfWeek)42)")]
    [InlineData("Any.Decimal().WithScale(2).WithScale(29)")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.FromHours(1)).WithGranularity(new TimeSpan(0))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(1)).WithOffset(TimeSpan.FromSeconds(30))")]
    [InlineData("Any.Uri().Web().WithPort(80).WithPort(0)")]
    [InlineData("Any.Uri().Web().WithHost(\"a.example\").WithHost(\"bad host\")")]
    [InlineData("Any.Uri().Web().WithUserInfo(\"a\").WithUserInfo(\"b:c\")")]
    [InlineData("Any.Uri().Relative().WithPathSegments(1).WithPathSegments(-1)")]
    [InlineData("Any.Uri().Mailto().WithDomain(\"a.example\").WithDomain(\"\")")]
    [InlineData("Any.Uri().Mailto().WithLocalPart(\"a\").WithLocalPart(\"a b\")")]
    // The refused call comes first, so the two declarations after it are never applied.
    [InlineData("Any.Int32().MultipleOf(0).MultipleOf(3).MultipleOf(5)")]
    // So does a call that is not itself a once-per-generator constraint: its guard throws all the same.
    [InlineData("Any.String().Alpha().WithMinLength(-1).Numeric()")]
    [InlineData("Any.String().Alpha().Containing(\"\").Numeric()")]
    [InlineData("Any.Int32().MultipleOf(2).Between(10, 1).MultipleOf(3)")]
    [InlineData("Any.DateTime().WithGranularity(TimeSpan.Zero).WithGranularity(TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromMinutes(1))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromSeconds(30)).WithOffset(TimeSpan.FromHours(1)).WithOffset(TimeSpan.FromHours(2))")]
    [InlineData("Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(15)).WithOffset(TimeSpan.FromHours(1)).WithOffset(TimeSpan.FromHours(2))")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(1)).WithOffset(TimeSpan.FromHours(1)).WithOffset(TimeSpan.FromHours(2))")]
    public async Task Does_not_report_at_or_past_a_call_that_rejects_its_own_argument(string chain) {
        // Review of #282: the run time throws an argument exception there, not "already defined", and applies nothing
        // after it.
        await NothingReportedAsync($"_ = {chain}.Generate();");
    }

    [Fact]
    public async Task Does_not_report_a_second_Distinct_comparer() {
        // The run time compares comparers by reference, so no reading of the source tells a repetition from a conflict.
        await NothingReportedAsync("_ = Any.ListOf(Any.String()).Distinct(StringComparer.Ordinal).Distinct(StringComparer.OrdinalIgnoreCase).Generate();");
    }

}
