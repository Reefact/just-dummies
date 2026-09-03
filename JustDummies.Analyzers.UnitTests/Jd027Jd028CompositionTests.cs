using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd027UnusedCombineOperandTests {

    [Fact]
    public async Task Reports_an_operand_the_composer_never_reads() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.Combine(Dummy.Int32(), Dummy.String(), (number, text) => number);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD027");
        Check.That(diagnostics[0].GetMessage()).Contains("'text'");
    }

    [Fact]
    public async Task Reports_every_ignored_operand() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.Combine(Dummy.Int32(), Dummy.String(), Dummy.Boolean(), (number, text, flag) => number);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(2);
    }

    [Fact]
    public async Task Does_not_report_a_composer_that_reads_every_operand() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.Combine(Dummy.Int32(), Dummy.String(), (number, text) => text + number);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_discarded_parameter() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.Combine(Dummy.Int32(), Dummy.String(), (number, _) => number);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Sees_a_parameter_read_inside_a_nested_lambda() {
        const string source = """
            using System;
            using System.Linq;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.Combine(Dummy.Int32(), Dummy.String(), (number, text) => Enumerable.Range(0, 1).Select(_ => text).First() + number);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_composer_whose_whole_body_is_a_throw() {
        // Dogfooding found this on the library's own arity-8 test: a composer that throws reads no parameter by
        // construction, and is exercising the failure path Combine wraps rather than ignoring an operand.
        //
        // The type arguments are explicit because they have to be — a throw-only lambda gives inference nothing to
        // infer TResult from. The first version of this test omitted them, so the snippet did not compile, no lambda
        // was ever bound, and the rule stood down for the wrong reason: it passed while the real site still fired.
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IDummy<string> failing = Dummy.Combine<int, string, string>(Dummy.Int32(), Dummy.String(), (number, text) => throw new InvalidOperationException("rejected"));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_composer_passed_as_a_method_group() {
        // The method's body is not necessarily this compilation's to read, so which operands it uses is not knowable.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.Combine(Dummy.Int32(), Dummy.String(), Pair);
                }

                private static string Pair(int number, string text) { return text; }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedCombineOperandAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd028InertDistinctnessTests {

    private const string ReferenceEqualityType = """
        public sealed class Box {
            public Box(int value) { Value = value; }
            public int Value { get; }
        }
        """;

    [Theory]
    [InlineData("Dummy.ListOf(Dummy.Int32().As(v => new Box(v))).Distinct()")]
    [InlineData("Dummy.ArrayOf(Dummy.Int32().As(v => new Box(v))).Distinct()")]
    [InlineData("Dummy.SequenceOf(Dummy.Int32().As(v => new Box(v))).Distinct()")]
    [InlineData("Dummy.SetOf(Dummy.Int32().As(v => new Box(v)))")]
    public async Task Reports_distinctness_over_reference_equality(string expression) {
        string source = $$"""
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD028");
        Check.That(diagnostics[0].GetMessage()).Contains("'Box'");
    }

    [Theory]
    [InlineData("public sealed record Box(int Value);")]
    [InlineData("public sealed class Box : System.IEquatable<Box> { public bool Equals(Box other) => true; public override bool Equals(object o) => true; public override int GetHashCode() => 0; }")]
    [InlineData("public sealed class Box { public override bool Equals(object o) => true; public override int GetHashCode() => 0; }")]
    [InlineData("public class Box { }")]
    public async Task Does_not_report_a_type_whose_equality_can_bind(string declaration) {
        string source = $$"""
            using JustDummies;

            {{declaration}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Int32().As(v => (Box)null)).Distinct();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Dummy.ListOf(Dummy.Int32()).Distinct()")]
    [InlineData("Dummy.ListOf(Dummy.String()).Distinct()")]
    [InlineData("Dummy.SetOf(Dummy.Guid())")]
    public async Task Does_not_report_a_built_in_element_type(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Dummy.SetOf(Dummy.OneOf(first, second))")]
    [InlineData("Dummy.ListOf(Dummy.OneOf(first, second)).Distinct()")]
    [InlineData("Dummy.ListOf(held).Distinct()")]
    public async Task Does_not_report_a_generator_that_hands_back_existing_instances(string expression) {
        // The narrowing dogfooding forced. A pool returns the very references it was handed, so drawing the same
        // member twice yields the same reference and distinctness binds exactly as asked — the library's own suite
        // asserts precisely that.
        string source = $$"""
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M(Box first, Box second, IDummy<Box> held) {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_collection_that_never_asked_for_distinctness() {
        string source = $$"""
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Int32().As(v => new Box(v))).WithCount(3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_an_explicit_comparer_answers_the_question() {
        string source = $$"""
            using System.Collections.Generic;
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M(IEqualityComparer<Box> comparer) {
                    _ = Dummy.ListOf(Dummy.Int32().As(v => new Box(v))).Distinct(comparer);
                    _ = Dummy.SetOf(Dummy.Int32().As(v => new Box(v)), comparer);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_an_element_composed_by_Combine() {
        string source = $$"""
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.SetOf(Dummy.Combine(Dummy.Int32(), Dummy.Int32(), (left, right) => new Box(left + right)));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD028");
    }

    [Fact]
    public async Task Does_not_report_a_projection_that_may_return_a_shared_instance() {
        string source = $$"""
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                private static Box Lookup(int value) { return null; }

                public static void M() {
                    _ = Dummy.SetOf(Dummy.Int32().As(v => Lookup(v)));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_dictionary_on_its_key_type() {
        string source = $$"""
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M() {
                    _ = Dummy.DictionaryOf(Dummy.Int32().As(v => new Box(v)), Dummy.String());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD028");
    }

    [Fact]
    public async Task Does_not_report_a_negative_test() {
        string source = $$"""
            using System;
            using JustDummies;

            {{ReferenceEqualityType}}

            public static class Sample {
                public static void M() {
                    Run(() => Dummy.SetOf(Dummy.Int32().As(v => new Box(v))));
                }

                private static void Run(Func<object> body) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InertDistinctnessAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
