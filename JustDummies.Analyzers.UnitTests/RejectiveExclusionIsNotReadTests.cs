using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     The rejective exclusions on <c>IAny&lt;T&gt;</c>, <c>DifferentFrom</c> and <c>Except</c>, are read by no rule: they
///     redraw at generation time and declare nothing a builder's specification holds (ADR-0121, ADR-0133). Several
///     rules recognise an exclusion by the name <c>Except</c>, so every analyzer of the package runs here, the opt-in
///     ones included, over chains whose builder part is satisfiable and whose rejective part excludes every value.
/// </summary>
public sealed class RejectiveExclusionIsNotReadTests {

    private static IEnumerable<DiagnosticAnalyzer> AllAnalyzers() {
        return typeof(ScalarChainAdmitsNoValueAnalyzer).Assembly.GetTypes()
                                                       .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type) && type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
                                                       .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!);
    }

    [Theory]
    [InlineData("int anyValue = ((IAny<int>)Any.Int32().OneOf(5, 6)).Except(5, 6).Generate();")]
    [InlineData("int anyValue = ((IAny<int>)Any.Int32().Between(1, 2)).Except(new List<int> { 1, 2 }).Generate();")]
    [InlineData("decimal anyValue = ((IAny<decimal>)Any.Decimal().OneOf(1m, 2m)).Except(1m, 2m).Generate();")]
    [InlineData("string anyValue = ((IAny<string>)Any.String().OneOf(\"a\", \"b\")).Except(\"a\", \"b\").Generate();")]
    [InlineData("Day anyValue = ((IAny<Day>)Any.Enum<Day>().OneOf(Day.Mon, Day.Tue)).Except(Day.Mon, Day.Tue).Generate();")]
    [InlineData("HashSet<int> anyValues = Any.SetOf(((IAny<int>)Any.Int32().Between(1, 2)).Except(1, 2)).WithCount(2).Generate();")]
    [InlineData("int anyValue = ((IAny<int>)Any.Int32().OneOf(5, 6)).DifferentFrom(5).DifferentFrom(6).Generate();")]
    public async Task No_rule_reads_a_rejective_exclusion(string statement) {
        string source = $$"""
            using System.Collections.Generic;
            using JustDummies;

            public enum Day { Mon, Tue, Wed }

            public static class Sample {
                public static void M() {
                    {{statement}}
                }
            }
            """;

        DiagnosticAnalyzer[] analyzers = AllAnalyzers().ToArray();
        // Every rule of the package, so a pass is never the empty loop's.
        Check.That(analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).Select(descriptor => descriptor.Id).Distinct().Count(id => id.StartsWith("JD", StringComparison.Ordinal))).IsEqualTo(38);

        foreach (DiagnosticAnalyzer analyzer in analyzers) {
            string[] ids = analyzer.SupportedDiagnostics.Select(descriptor => descriptor.Id).ToArray();

            ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(analyzer, source, ids);

            Check.WithCustomMessage($"{analyzer.GetType().Name} reported {string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Id))}")
                 .That(diagnostics.Length).IsEqualTo(0);
        }
    }

}
