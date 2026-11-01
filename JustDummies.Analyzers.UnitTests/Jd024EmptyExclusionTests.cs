using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     An exclusion written as a constant empty list removes nothing: the run time accepts it as a no-op, so JD014 no
///     longer reports it and JD024 does, on every generator family (ADR-0122).
/// </summary>
public class Jd024EmptyExclusionTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(DiagnosticAnalyzer analyzer, string expression) {
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static void M(string[] taken) {
                    _ = {{expression}};
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(analyzer, source);
    }

    [Theory]
    [InlineData("Any.Int32().Except()")]
    [InlineData("Any.Int32().Between(1, 9).Except()")]
    [InlineData("Any.String().Except()")]
    [InlineData("Any.StringMatching(\"[a-z]{3}\").Except([])")]
    [InlineData("Any.Guid().Except(new Guid[0])")]
    [InlineData("Any.OneOf(1, 2).Except(Array.Empty<int>())")]
    [InlineData("Any.Double().Except()")]
    public async Task JD024_reports_a_constant_empty_exclusion(string expression) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(new ScalarChainAdmitsNoValueAnalyzer(), expression);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD024");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This constraint changes nothing: an empty exclusion removes no value");
    }

    [Theory]
    [InlineData("Any.StringMatching(\"[a-z]{3}\").Except(taken)")]
    [InlineData("Any.Int32().Except(new int[taken.Length])")]
    public async Task JD024_does_not_report_a_computed_exclusion(string expression) {
        // The fixture case: a set of values already used that starts empty. Only what is written empty is reported.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(new ScalarChainAdmitsNoValueAnalyzer(), expression);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Any.Int32().Except()")]
    [InlineData("Any.String().Except([])")]
    public async Task JD014_no_longer_reports_an_empty_exclusion(string expression) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(new RejectedConstantArgumentAnalyzer(), expression);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task JD014_still_reports_an_empty_value_set() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(new RejectedConstantArgumentAnalyzer(), "Any.Int32().OneOf()");

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

}
