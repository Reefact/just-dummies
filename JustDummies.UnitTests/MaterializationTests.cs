#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

[TestSubject(typeof(Any))]
public sealed class MaterializationTests {

    #region Statics members declarations

    private static T Materialize<T>(IAny<T> generator) {
        return generator.Generate();
    }

    #endregion

    [Fact(DisplayName = "Generate materializes a valid string value.")]
    public void GenerateMaterializesAString() {
        string value = Any.String().NonEmpty().Generate();

        Check.That(value).IsNotEmpty();
    }

    [Fact(DisplayName = "Generate materializes a valid int value.")]
    public void GenerateMaterializesAnInt() {
        int value = Any.Int32().Positive().Generate();

        Check.That(value).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "A materialized value flows into a method expecting the generated type.")]
    public void GeneratedValueFlowsIntoACallSite() {
        static int Measure(string text) {
            return text.Length;
        }

        int length = Measure(Any.String().WithLength(9).Generate());

        Check.That(length).IsEqualTo(9);
    }

    [Fact(DisplayName = "Each Generate call draws a fresh value.")]
    public void EachGenerateDrawsAFreshValue() {
        AnyInt32 generator = Any.Int32().Between(0, int.MaxValue);

        HashSet<int> seen = [];
        for (int i = 0; i < 20; i++) {
            seen.Add(generator.Generate());
        }

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "Generic inference flows through IAny<T>, materializing without any implicit conversion.")]
    public void GenericInferenceMaterializesThroughIAny() {
        string text  = Materialize(Any.String().NonEmpty());
        int    value = Materialize(Any.Int32().Positive());

        Check.That(text).IsNotEmpty();
        Check.That(value).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "Building a generator draws nothing at all, which is why a chain left unmaterialized is silent.")]
    [SuppressMessage(JustDummiesRule.JD006.Category, JustDummiesRule.JD006.Id, Justification = "The discarded generator IS the subject. This pins the behaviour JD006 reports: the arrange line reads like it did something, and drew nothing.")]
    public void BuildingAGeneratorDrawsNothing() {
        int draws = 0;

        Any.Int32().As(value => {
            draws++;

            return value;
        });

        Check.That(draws).IsEqualTo(0);
    }

    [Fact(DisplayName = "A generator interpolated into text renders its type name, never a value it could draw.")]
    [SuppressMessage(JustDummiesRule.JD005.Category, JustDummiesRule.JD005.Id, Justification = SuppressionJustification.JD005.RenderedGeneratorIsTheSubject)]
    public void AGeneratorRendersAsItsTypeName() {
        string rendered = $"{Any.Int32()}";

        Check.That(rendered).IsEqualTo(typeof(AnyInt32).ToString());
        Check.That(int.TryParse(rendered, out int _)).IsFalse();
    }

}
