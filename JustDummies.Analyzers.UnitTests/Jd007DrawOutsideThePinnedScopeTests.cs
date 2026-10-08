using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd007DrawOutsideThePinnedScopeTests {

    [Fact]
    public async Task Reports_a_draw_in_the_constructor_of_a_reproducible_class() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference;

                public Sample() {
                    _reference = Any.String().NonEmpty().Generate();
                }

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
        Check.That(diagnostics[0].GetMessage()).Contains("constructor");
    }

    [Fact]
    public async Task Reports_a_draw_in_a_field_initializer() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
        Check.That(diagnostics[0].GetMessage()).Contains("field initializer");
    }

    [Fact]
    public async Task Reports_a_draw_in_InitializeAsync() {
        const string source = """
            using System.Threading.Tasks;
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample : IAsyncLifetime {
                private string _reference = "";

                public ValueTask InitializeAsync() {
                    _reference = Any.String().NonEmpty().Generate();

                    return default;
                }

                public ValueTask DisposeAsync() => default;

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
        Check.That(diagnostics[0].GetMessage()).Contains("InitializeAsync");
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_the_test_body() {
        // The body IS inside the pinned scope — verified against xunit.v3 by probe before this rule was written.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                [Fact]
                public void T() {
                    string reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_class_without_the_attribute() {
        const string source = """
            using JustDummies;
            using Xunit;

            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_from_an_isolated_context() {
        // Any.WithSeed is isolated by design: the ambient scope does not govern it, so the diagnostic would be wrong.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference;

                public Sample() {
                    AnyContext context = Any.WithSeed(1234);
                    _reference = context.String().NonEmpty().Generate();
                }

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_under_an_assembly_level_attribute() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [assembly: Reproducible]

            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
    }

    [Fact]
    public async Task Reports_a_draw_derived_through_a_library_extension() {
        // Issue #219: As(...) and OrNull() draw from the generator they are called on, so these are drawn before the
        // scope opens exactly as their underived twins are.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            public sealed class Reference {
                public Reference(string value) { }
            }

            [Reproducible]
            public class Sample {
                private readonly Reference _reference = Any.String().NonEmpty().As(value => new Reference(value)).Generate();
                private readonly string? _note;

                public Sample() {
                    _note = Any.String().NonEmpty().OrNull().Generate();
                }

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(2);
        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsOnlyElementsThatMatch(id => id == "JD007");
    }

    [Fact]
    public async Task Does_not_report_an_inline_isolated_context() {
        // Issue #219: the same isolated context as above, written inline rather than held in a local.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference = Any.WithSeed(1234).String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_type_xunit_does_not_instantiate() {
        // Issue #221: under an assembly-level attribute every type is covered, and the rule judged the member kind
        // alone. A test-data builder is constructed by the test body, inside the scope, never by xUnit before it —
        // and a private [Fact] on a base type is not inherited, so it does not make its heirs test classes.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [assembly: Reproducible]

            public sealed class OrderBuilder {
                private string _reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
                private string _customer;

                public OrderBuilder() {
                    _customer = Any.String().Alpha().WithLengthBetween(1, 50).Generate();
                }

                public string Build() => _reference + "/" + _customer;
            }

            public class SelfCheckingBase {
                [Fact]
                private void SelfCheck() { }
            }

            public sealed class CustomerBuilder : SelfCheckingBase {
                private string _name = Any.String().Alpha().WithLengthBetween(1, 50).Generate();
            }

            public class OrderTests {
                [Fact]
                public void T() {
                    string order = new OrderBuilder().Build();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_test_class_whose_tests_are_inherited() {
        // A test class may declare no test of its own and inherit them: xUnit still constructs it before the hooks.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [assembly: Reproducible]

            public abstract class OrderContract {
                [Fact]
                public void T() { }
            }

            public class OrderTests : OrderContract {
                private readonly string _reference = Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
    }

}
