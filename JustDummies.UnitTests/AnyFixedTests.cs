#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <see cref="Any.Fixed{T}" />: a literal the test supplies, wearing the generator's shape so it composes. Each case
///     is one of the three promises the factory makes — it draws nothing, it accepts <c>null</c>, it returns the same
///     instance — or the one consequence of drawing nothing that the composition seams must respect: a fixed operand
///     never qualifies a replay it takes no part in (ADR-0118).
/// </summary>
public sealed class AnyFixedTests {

    private const int Seed = 20261101;

    [Fact(DisplayName = "A fixed value consumes no draw, so the values drawn around it replay unchanged.")]
    public void AFixedValueConsumesNoDraw() {
        int alone;
        using (Any.UseSeed(Seed)) { alone = Any.Int32().Generate(); }

        int afterFixed;
        using (Any.UseSeed(Seed)) {
            _ = Any.Fixed(42).Generate();
            _ = Any.Fixed("pinned").Generate();
            afterFixed = Any.Int32().Generate();
        }

        Check.That(afterFixed).IsEqualTo(alone);
    }

    [Fact(DisplayName = "A fixed value returns exactly what was given, null included.")]
    public void AFixedValueReturnsWhatWasGivenNullIncluded() {
        Check.That(Any.Fixed(42).Generate()).IsEqualTo(42);
        Check.That(Any.Fixed<string?>(null).Generate()).IsNull();
        Check.That(Any.Fixed<int?>(null).Generate()).IsNull();
    }

    [Fact(DisplayName = "A fixed value returns the same instance on every call — a mutable object is shared, not copied.")]
    public void AFixedValueReturnsTheSameInstance() {
        List<int>      shared    = [1, 2, 3];
        IAny<List<int>> generator = Any.Fixed(shared);

        Check.That(ReferenceEquals(generator.Generate(), shared)).IsTrue();
        Check.That(ReferenceEquals(generator.Generate(), generator.Generate())).IsTrue();
    }

    [Fact(DisplayName = "A fixed operand does not qualify a Combine's replay hint: it takes no part in the draw.")]
    public void AFixedOperandDoesNotQualifyTheReplayHint() {
        // A Combine names one seed and promises a full replay only when every operand draws from it. A fixed operand
        // draws from nothing, so it must neither hijack the reported source nor withdraw the promise: the seeded
        // operand alone decides, exactly as if the constant had been written inside the composer.
        AnyContext seeded = Any.WithSeed(4242);

        IAny<string> generator = Any.Combine<string, int, string>(
            Any.Fixed("pinned"),
            seeded.Int32().Between(1, 3),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        Check.ThatCode(() => generator.Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 4242)
             .And.WhichMember(caught => caught.Message)
             .Contains("Any.WithSeed(4242)")
             .And.Not.Contains("replay from this seed only if it draws");
    }

    [Fact(DisplayName = "OrNull over a fixed value draws its coin from the ambient source and reports it.")]
    public void OrNullOverAFixedValueReportsTheAmbientSource() {
        // The coin is the only draw such a chain makes, and it comes from the ambient source: the chain must replay
        // under the ambient seed, so it reports that source rather than none.
        List<int?> first;
        List<int?> second;
        using (Any.UseSeed(Seed)) { first = Enumerable.Range(0, 50).Select(_ => Any.Fixed(7).OrNull().Generate()).ToList(); }
        using (Any.UseSeed(Seed)) { second = Enumerable.Range(0, 50).Select(_ => Any.Fixed(7).OrNull().Generate()).ToList(); }

        Check.That(second).ContainsExactly(first);
        Check.That(first).Contains((int?)null, 7);
    }

    [Fact(DisplayName = "A distinct collection over a fixed value counts one value, and refuses two as a conflict rather than redrawing.")]
    public void ADistinctCollectionOverAFixedValueCountsOneValue() {
        Check.That(Any.SetOf(Any.Fixed(5)).WithCount(1).Generate()).ContainsExactly(5);
        Check.ThatCode(() => Any.SetOf(Any.Fixed(5)).WithCount(2).Generate()).Throws<ConflictingAnyConstraintException>();
    }

}
