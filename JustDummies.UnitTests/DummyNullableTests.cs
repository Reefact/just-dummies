#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class DummyNullableTests {

    #region Statics members declarations

    private const int SampleCount = 200;

    // Note: chaining OrNull twice (a nullable of a nullable) is a compile-time error — a Nullable<T> is not a
    // struct and not a class, so neither OrNull overload applies. That guard needs no runtime test.

    private static string Render(int? value) {
        return value?.ToString() ?? "null";
    }

    #endregion

    [Fact(DisplayName = "OrNull on a value type yields both null and non-null; the non-null values honour the inner constraints.")]
    public void ValueTypeOrNullYieldsBothCases() {
        IDummy<int?> generator = Dummy.Int32().Between(1, 100).OrNull();

        int nulls    = 0;
        int nonNulls = 0;
        for (int i = 0; i < SampleCount; i++) {
            int? value = generator.Generate();
            if (value is null) {
                nulls++;
            } else {
                nonNulls++;
                Check.That(value.Value is >= 1 and <= 100).IsTrue();
            }
        }

        Check.That(nulls).IsStrictlyGreaterThan(0);
        Check.That(nonNulls).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "OrNull on a reference type yields both null and non-null values satisfying the inner constraints.")]
    public void ReferenceTypeOrNullYieldsBothCases() {
        IDummy<string?> generator = Dummy.String().NonEmpty().OrNull();

        bool sawNull    = false;
        bool sawNonNull = false;
        for (int i = 0; i < SampleCount; i++) {
            string? value = generator.Generate();
            if (value is null) {
                sawNull = true;
            } else {
                sawNonNull = true;
                Check.That(value).IsNotEmpty();
            }
        }

        Check.That(sawNull).IsTrue();
        Check.That(sawNonNull).IsTrue();
    }

    [Fact(DisplayName = "OrNull is reproducible: two same-seed contexts replay the same null/value sequence.")]
    public void OrNullIsReproducibleUnderASeed() {
        IDummy<int?> first  = Dummy.WithSeed(123).Int32().OrNull();
        IDummy<int?> second = Dummy.WithSeed(123).Int32().OrNull();

        string sequenceOne = string.Join("|", Enumerable.Range(0, 30).Select(_ => Render(first.Generate())));
        string sequenceTwo = string.Join("|", Enumerable.Range(0, 30).Select(_ => Render(second.Generate())));

        Check.That(sequenceTwo).IsEqualTo(sequenceOne);
        // The sequence exercises both branches — otherwise the reproducibility guarantee would be vacuous.
        Check.That(sequenceOne).Contains("null");
    }

    [Fact(DisplayName = "OrNull composes with As to produce an optional value object.")]
    public void OrNullComposesWithAs() {
        IDummy<OrderReference?> generator = Dummy.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).OrNull();

        bool sawNull    = false;
        bool sawNonNull = false;
        for (int i = 0; i < SampleCount; i++) {
            OrderReference? reference = generator.Generate();
            if (reference is null) {
                sawNull = true;
            } else {
                sawNonNull = true;
                Check.That(reference.Value).StartsWith("ORD-");
            }
        }

        Check.That(sawNull).IsTrue();
        Check.That(sawNonNull).IsTrue();
    }

    [Fact(DisplayName = "OrNull validates its argument on both the value-type and reference-type overloads.")]
    public void OrNullValidatesItsArgument() {
        Check.ThatCode(() => ((IDummy<int>)null!).OrNull()).Throws<ArgumentNullException>();
        Check.ThatCode(() => ((IDummy<string>)null!).OrNull()).Throws<ArgumentNullException>();
    }

}
