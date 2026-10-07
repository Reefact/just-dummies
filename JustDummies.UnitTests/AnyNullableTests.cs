#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class AnyNullableTests {

    #region Statics members declarations

    private const int SampleCount = 200;

    private enum Slot { None, Morning, Evening }

    // Note: chaining OrNull twice (a nullable of a nullable) is a compile-time error — a Nullable<T> is not a
    // struct and not a class, so neither OrNull overload applies. That guard needs no runtime test.

    private static string Render(int? value) {
        return value?.ToString() ?? "null";
    }

    #endregion

    [Fact(DisplayName = "OrNull on a value type yields both null and non-null; the non-null values honour the inner constraints.")]
    public void ValueTypeOrNullYieldsBothCases() {
        IAny<int?> generator = Any.Int32().Between(1, 100).OrNull();

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
        IAny<string?> generator = Any.String().NonEmpty().OrNull();

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
        IAny<int?> first  = Any.WithSeed(123).Int32().OrNull();
        IAny<int?> second = Any.WithSeed(123).Int32().OrNull();

        string sequenceOne = string.Join("|", Enumerable.Range(0, 30).Select(_ => Render(first.Generate())));
        string sequenceTwo = string.Join("|", Enumerable.Range(0, 30).Select(_ => Render(second.Generate())));

        Check.That(sequenceTwo).IsEqualTo(sequenceOne);
        // The sequence exercises both branches — otherwise the reproducibility guarantee would be vacuous.
        Check.That(sequenceOne).Contains("null");
    }

    [Fact(DisplayName = "OrNull composes with As to produce an optional value object.")]
    public void OrNullComposesWithAs() {
        IAny<OrderReference?> generator = Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).OrNull();

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

    [Fact(DisplayName = "A distinct collection over OrNull draws within the wrapped domain plus null.")]
    public void ADistinctCollectionOverOrNullDraws() {
        // Issue #212, at the coordinates it was reported on: the set had no ceiling, drew a size the four values could
        // not fill, and exhausted its redraw on more than half the seeds.
        using (Any.UseSeed(0)) {
            Check.That(Any.SetOf(Any.Enum<Slot>().OrNull()).NonEmpty().Generate().Count).IsStrictlyLessThan(5);
        }
        using (Any.UseSeed(0)) {
            Check.That(Any.SetOf(Any.String().OneOf("EUR", "USD", "GBP").OrNull()).NonEmpty().Generate().Count).IsStrictlyLessThan(5);
        }
    }

    [Fact(DisplayName = "OrNull counts null exactly once: the full domain draws, one more is refused at once.")]
    public void OrNullCountsNullExactlyOnce() {
        Check.That(Any.SetOf(Any.Enum<Slot>().OrNull()).WithCount(4).Generate()).Contains((Slot?)null);
        Check.ThatCode(() => Any.SetOf(Any.Enum<Slot>().OrNull()).WithCount(5).Generate())
             .Throws<ConflictingAnyConstraintException>();

        // A null pinned into the collection is already inside the domain, so it extends nothing.
        Check.ThatCode(() => Any.SetOf(Any.Enum<Slot>().OrNull()).Containing(null).WithCount(5).Generate())
             .Throws<ConflictingAnyConstraintException>();

        // Applying OrNull() twice to a reference generator still compiles, because nullable reference annotations do
        // not change the runtime type; null is still one value.
        IAny<string> optional = (IAny<string>)Any.String().OneOf("EUR", "USD").OrNull();
        Check.That(Any.SetOf(optional.OrNull()).WithCount(3).Generate()).Contains((string?)null);
        Check.ThatCode(() => Any.SetOf(optional.OrNull()).WithCount(4).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "OrNull reads null from the domain it wraps, not from the wrapper's type.")]
    public void OrNullReadsNullFromTheWrappedDomain() {
        // Review of #212: an optional generator relayed by a wrapper that forwards its domain already holds null, so
        // OrNull() over the relay must not count it a second time.
        IAny<string> relayed = new RelayingAny<string>((IAny<string>)Any.String().OneOf("EUR", "USD").OrNull());

        Check.That(Any.SetOf(relayed.OrNull()).WithCount(3).Generate()).Contains((string?)null);
        Check.ThatCode(() => Any.SetOf(relayed.OrNull()).WithCount(4).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "OrNull validates its argument on both the value-type and reference-type overloads.")]
    public void OrNullValidatesItsArgument() {
        Check.ThatCode(() => ((IAny<int>)null!).OrNull()).Throws<ArgumentNullException>();
        Check.ThatCode(() => ((IAny<string>)null!).OrNull()).Throws<ArgumentNullException>();
    }

    #region Nested types

    // A generator that forwards another one's draws and its whole cardinality hint, adding nothing: the shape of any
    // wrapper that preserves a domain without being the generator that built it.
    private sealed class RelayingAny<T> : IAny<T>, ICardinalityHint<T> {

        private readonly IAny<T> _inner;

        public RelayingAny(IAny<T> inner) {
            _inner = inner;
        }

        long? ICardinalityHint<T>.DistinctCardinality => ((ICardinalityHint<T>)_inner).DistinctCardinality;

        bool ICardinalityHint<T>.Contains(T value) {
            return ((ICardinalityHint<T>)_inner).Contains(value);
        }

        public T Generate() {
            return _inner.Generate();
        }

    }

    #endregion

}
