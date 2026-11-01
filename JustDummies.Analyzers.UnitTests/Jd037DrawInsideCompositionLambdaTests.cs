using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd037DrawInsideCompositionLambdaTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using System.Linq;
            using JustDummies;

            public sealed class Booking {
                public Booking(System.DateOnly start, System.DateOnly end) { }
            }

            public static class Sample {
                public static void M() {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInsideCompositionLambdaAnalyzer(), source);
    }

    [Fact]
    public async Task Reports_a_draw_inside_an_As_lambda() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "        IAny<Booking> anyBooking = Any.DateOnly().As(start => new Booking(start, Any.DateOnly().After(start).Generate()));");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD037");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostics[0].GetMessage())
             .IsEqualTo("This draw runs inside the lambda passed to As, apart from the recipe: compose an offset with Any.Combine, or derive the dependent draw with Then");
    }

    [Fact]
    public async Task Reports_a_draw_inside_a_Combine_lambda() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "        IAny<int> anySum = Any.Combine(Any.Int32(), Any.Int32(), (a, b) => a + b + Any.Int32().Between(0, 9).Generate());");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("lambda passed to Combine");
    }

    [Fact]
    public async Task Reports_a_draw_nested_deeper_inside_the_lambda() {
        // A draw inside a local query inside the lambda runs at generation time all the same.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "        IAny<int[]> anyRow = Any.Int32().Between(1, 5).As(n => Enumerable.Range(0, n).Select(_ => Any.Int32().Generate()).ToArray());");

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Fact]
    public async Task Reports_a_draw_from_an_explicit_context_too() {
        // The draw still runs apart from the recipe, and the rule reads the call, not the source: the remedy reads the
        // same whatever the inner generator draws from.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    AnyContext any = Any.WithSeed(1);
                    IAny<int> anyPair = any.Int32().As(n => n + any.Int32().Generate());
            """);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Theory]
    [InlineData("IAny<int> anyBounded = Any.Int32().Between(1, 100).Then(bound => Any.Int32().Between(0, bound));")]
    [InlineData("IAny<Booking> anyBooking = Any.Combine(Any.DateOnly(), Any.Int32().Between(1, 30), (start, nights) => new Booking(start, start.AddDays(nights)));")]
    [InlineData("int anyValue = Any.Int32().Generate();")]
    [InlineData("IAny<string> anyText = Any.Int32().As(n => n.ToString());")]
    [InlineData("System.Func<int> draw = () => Any.Int32().Generate();")]
    public async Task Does_not_report_a_draw_outside_a_composition_lambda(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_As_of_another_library() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    static int As(int value, System.Func<int, int> f) { return f(value); }
                    int anyValue = As(1, n => Any.Int32().Generate());
            """);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
