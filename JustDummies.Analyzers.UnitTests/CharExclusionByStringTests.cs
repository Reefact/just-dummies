using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     <c>Any.Char().Except("abc")</c>: a <see cref="string" /> passed to the sequence overload is the characters to
///     exclude (issue #393). The rules that read <c>Except</c>'s constants were written for <see cref="char" />
///     literals, so this pins that none of them mistakes the string for one constant, or for an empty exclusion: a
///     sequence held as a string is opaque to them, as one held in a variable is.
/// </summary>
public class CharExclusionByStringTests {

    private static readonly DiagnosticAnalyzer[] Analyzers = typeof(DrawInsideCompositionLambdaAnalyzer).Assembly
                                                                                                          .GetTypes()
                                                                                                          .Where(type => typeof(DiagnosticAnalyzer).IsAssignableFrom(type) && !type.IsAbstract && type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
                                                                                                          .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
                                                                                                          .ToArray();

    [Theory]
    [InlineData("_ = Any.Char().Alpha().Except(\"abc\");")]
    [InlineData("_ = Any.Char().Except(\"ab\").Except(\"cd\");")]
    [InlineData("_ = Any.Char().OneOf('a', 'b', 'c', 'd').Except(\"ab\");")]
    public async Task No_rule_misreads_a_string_of_excluded_characters(string statement) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    {{statement}}
                }
            }
            """;

        List<string> reported = [];
        foreach (DiagnosticAnalyzer analyzer in Analyzers) {
            ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(analyzer, source);
            reported.AddRange(diagnostics.Where(diagnostic => diagnostic.Id.StartsWith("JD", StringComparison.Ordinal)).Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage()}"));
        }

        Check.That(Analyzers.Length).IsStrictlyGreaterThan(30);
        Check.That(reported).IsEmpty();
    }

}
