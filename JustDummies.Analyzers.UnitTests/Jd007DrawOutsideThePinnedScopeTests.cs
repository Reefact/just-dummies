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

    [Fact]
    public async Task Does_not_report_a_draw_in_a_body_handed_on_as_a_value() {
        // Issue #220: each of these bodies draws when it is invoked — in the test, inside the scope — not when the
        // member declaring it runs. Roslyn attributes them to the enclosing member, which is what the rule judged.
        const string source = """
            using System;
            using System.Collections.Generic;
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            public sealed class Holder {
                public Func<int> Next { get; set; }
            }

            [Reproducible]
            public class Sample {
                private readonly Func<string> _reference = () => Any.String().NonEmpty().Generate();
                private readonly Lazy<string> _lazy = new Lazy<string>(() => Any.String().NonEmpty().Generate());
                private readonly Func<int> _next;
                private readonly Holder _holder;
                private readonly List<Func<int>> _factories = new() { () => Any.Int32().Generate() };

                public Sample() {
                    _next = Draw;
                    _holder = new Holder { Next = () => Any.Int32().Positive().Generate() };

                    int Draw() => Any.Int32().Positive().Generate();
                }

                [Fact]
                public void T() {
                    int value = _next() + _reference().Length;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_draw_in_a_body_the_member_runs_itself() {
        // The other side of issue #220: a body counts as deferred only when nothing can run it before the constructor
        // returns. An ordinary method, a constructor that calls its argument, an invocation on the spot, a local the
        // constructor then calls, a setter that calls its value, an array read at once, a field the constructor reads
        // back, a lambda returned by one run on the spot and an object read back all may — so each draw here stays
        // reported.
        const string source = """
            using System;
            using System.Linq;
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            public sealed class Runner {
                public Runner(Func<int> next) { Value = next(); }
                public int Value { get; }
            }

            public sealed class Box {
                public Func<int> Next { get; set; }
            }

            [Reproducible]
            public class Sample {
                private readonly int[] _values;
                private int _value;
                private readonly Runner _runner;
                private readonly Runner _runner2;
                private readonly Func<int> _readBack;
                private readonly Func<int> _initialized = () => Any.Int32().Generate();
                private readonly Box _box;

                public Func<int> Eager { set { _value = value(); } }

                public Sample() {
                    _values = Enumerable.Range(0, 3).Select(_ => Any.Int32().Generate()).ToArray();
                    _value = Draw();
                    _runner = new Runner(() => Any.Int32().Generate());
                    _runner2 = new Runner(DrawForTheRunner);
                    _value = ((Func<int>)(() => Any.Int32().Generate()))();
                    Func<int> next = () => Any.Int32().Generate();
                    _value = next();
                    Eager = () => Any.Int32().Generate();
                    _value = new Func<int>[] { () => Any.Int32().Generate() }[0]();
                    _readBack = () => Any.Int32().Generate();
                    _value = _readBack();
                    _value = _initialized();
                    _value = ((Func<Func<int>>)(() => () => Any.Int32().Generate()))()();
                    _box = new Box { Next = () => Any.Int32().Generate() };
                    _value = _box.Next();

                    int Draw() => Any.Int32().Positive().Generate();
                    int DrawForTheRunner() => Any.Int32().Positive().Generate();
                }

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(12);
    }

}
