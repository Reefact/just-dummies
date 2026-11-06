using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd039GeneratorPassedForItsValueTests {

    // The shape `any` scaffolds: a pin taking the value, a refinement taking a lambda, and no overload taking a
    // generator, on purpose (ADR-0109).
    private const string Domain = """
        public sealed record Route(string Origin, bool Domestic);

        public sealed class Shipment {
            public Shipment(Route route, int parcels) { }
        }

        public sealed class AnyRoute : IAny<Route> {
            public AnyRoute WithDomestic(bool value) { return this; }
            public Route Generate() { return new Route("Lyon", true); }
        }

        public sealed class AnyShipment : IAny<Shipment> {
            public AnyShipment WithRoute(Route value) { return this; }
            public AnyShipment WithRoute(Func<AnyRoute, AnyRoute> configure) { return this; }
            public Shipment Generate() { return new Shipment(new Route("Lyon", true), 1); }
        }

        public static class Sink {
            public static void Take(int count) { }
            public static void Name(string name, int count) { }
            public static void Routes(params Route[] routes) { }
            public static void Keep(Route value) { }
            public static void Keep(Func<AnyRoute, bool> predicate) { }
        }
        """;

    [Fact]
    public async Task Names_the_lambda_overload_when_a_scaffolded_With_is_given_a_prepared_generator() {
        // Issue #486: the four sessions that hit this were told only CS1503.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRefusedAsync("AnyRoute domestic = new AnyRoute().WithDomestic(true); new AnyShipment().WithRoute(domestic);");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD039");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("domestic is a generator of Route, and this parameter takes a Route: refine the parameter through the lambda overload instead, x => x.…, or pass a drawn value with Generate()");
    }

    [Theory]
    [InlineData("new Shipment(new AnyRoute(), 2);",            "new AnyRoute()", "Route")]
    [InlineData("Sink.Take(Any.Int32().Positive());",          "Any.Int32().Positive()", "int")]
    [InlineData("IAny<string> names = Any.String(); Sink.Name(names, 1);", "names", "string")]
    [InlineData("Sink.Name(count: Any.Int32(), name: \"x\");", "count: Any.Int32()", "int")]
    public async Task Names_Generate_where_the_method_offers_no_lambda(string statements, string argument, string drawn) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRefusedAsync(statements);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD039");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"{argument.Split(": ")[^1]} is a generator of {drawn}, and this parameter takes a {drawn}: pass a drawn value with Generate()");
        Check.That(diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length)).IsEqualTo(argument);
    }

    [Theory]
    [InlineData("Sink.Routes(new AnyRoute());",                          "new AnyRoute()")]
    [InlineData("Sink.Routes(new Route(\"Lyon\", true), new AnyRoute());", "new AnyRoute()")]
    public async Task Reads_an_expanded_params_argument_against_its_element_type(string statements, string argument) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRefusedAsync(statements);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD039");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"{argument} is a generator of Route, and this parameter takes a Route: pass a drawn value with Generate()");
    }

    [Fact]
    public async Task Names_Generate_only_when_the_lambda_overload_returns_something_else() {
        // A Func<AnyRoute, bool> takes the generator but yields no refined one: x => x.WithDomestic(true) cannot satisfy it.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRefusedAsync("Sink.Keep(new AnyRoute());");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD039");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("new AnyRoute() is a generator of Route, and this parameter takes a Route: pass a drawn value with Generate()");
    }

    [Theory]
    // A generator of another type: CS1503 for a reason this rule does not explain.
    [InlineData("Sink.Name(Any.Int32(), 1);")]
    // A named argument binds to the params array itself, never to an element: Generate() would not compile either.
    [InlineData("Sink.Routes(routes: new AnyRoute());")]
    // The call fails on its arity, and no argument is a generator.
    [InlineData("Sink.Take(1, 2);")]
    public async Task Stays_silent_when_the_call_fails_for_another_reason(string statements) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeRefusedAsync(statements, statements.Contains("1, 2") ? "CS1501" : "CS1503");

        Check.That(diagnostics).IsEmpty();
    }

    [Fact]
    public async Task Stays_silent_on_a_call_that_compiles() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPassedForItsValueAnalyzer(), Source("new AnyShipment().WithRoute(r => r.WithDomestic(true)); Sink.Take(Any.Int32().Generate());"));

        Check.That(diagnostics).IsEmpty();
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeRefusedAsync(string statements, string expectedError = "CS1503") {
        return AnalyzerTestHarness.GetDiagnosticsOfARefusedSnippetAsync(new GeneratorPassedForItsValueAnalyzer(), Source(statements), expectedError);
    }

    private static string Source(string statements) {
        return $$"""
            using System;
            using JustDummies;

            {{Domain}}

            public static class Sample {
                public static void M() {
                    {{statements}}
                }
            }
            """;
    }

}
