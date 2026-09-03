#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Regression coverage for issue #207. On the narrow (quantized) floating-point types an exclusion inside a tight
///     range must be honoured by the type-aware nudge — ascending or descending — instead of stalling on a sub-ulp
///     double step and exhausting the budget on a satisfiable specification. The identical scenarios on
///     <see cref="Dummy.Double" /> guard the shared engine from the other side.
/// </summary>
public sealed class ContinuousExclusionNudgeTests {

    private const int SeedCount = 500;

    [Fact(DisplayName = "Half: an exclusion on the lower bound of a two-value range yields the surviving value for every seed.")]
    public void HalfExclusionOnLowerBound() {
        Half min      = (Half)1f;
        Half max      = (Half)1.001f;   // rounds to 1.0009765625: the next representable Half above 1.0
        Half survivor = max;

        for (int seed = 0; seed < SeedCount; seed++) {
            Half value = Dummy.WithSeed(seed).Half().Between(min, max).DifferentFrom(min).Generate();
            Check.That(value == survivor).IsTrue();
        }
    }

    [Fact(DisplayName = "Half: an exclusion on the upper bound descends to the surviving lower value for every seed.")]
    public void HalfExclusionOnUpperBound() {
        Half min = (Half)1f;
        Half max = (Half)1.001f;

        for (int seed = 0; seed < SeedCount; seed++) {
            Half value = Dummy.WithSeed(seed).Half().Between(min, max).DifferentFrom(max).Generate();
            Check.That(value == min).IsTrue();
        }
    }

    [Fact(DisplayName = "Single: an exclusion inside a narrow range never yields the excluded value, either bound, for any seed.")]
    public void SingleExclusionInsideNarrowRange() {
        float min = 1f;
        float max = MathF.BitIncrement(MathF.BitIncrement(1f));   // 1 + 2 ulp: three representable floats in range

        for (int seed = 0; seed < SeedCount; seed++) {
            float lower = Dummy.WithSeed(seed).Single().Between(min, max).DifferentFrom(min).Generate();
            Check.That(lower).IsStrictlyGreaterThan(min);
            Check.That(lower).IsLessOrEqualThan(max);

            float upper = Dummy.WithSeed(seed).Single().Between(min, max).DifferentFrom(max).Generate();
            Check.That(upper).IsStrictlyLessThan(max);
            Check.That(upper).IsGreaterOrEqualThan(min);
        }
    }

    [Fact(DisplayName = "Double: an exclusion inside a narrow range never yields the excluded value, either bound, for any seed.")]
    public void DoubleExclusionInsideNarrowRange() {
        double min = 1d;
        double max = Math.BitIncrement(Math.BitIncrement(1d));   // 1 + 2 ulp: three representable doubles in range

        for (int seed = 0; seed < SeedCount; seed++) {
            double lower = Dummy.WithSeed(seed).Double().Between(min, max).DifferentFrom(min).Generate();
            Check.That(lower).IsStrictlyGreaterThan(min);
            Check.That(lower).IsLessOrEqualThan(max);

            double upper = Dummy.WithSeed(seed).Double().Between(min, max).DifferentFrom(max).Generate();
            Check.That(upper).IsStrictlyLessThan(max);
            Check.That(upper).IsGreaterOrEqualThan(min);
        }
    }

    [Fact(DisplayName = "A range whose every representable value is excluded fails with a seeded DummyGenerationException whose replay hint points at Dummy.WithSeed, not the inapplicable Dummy.Reproducibly.")]
    public void ExhaustedRangeThrowsSeededGenerationException() {
        Half min = (Half)1f;
        Half max = (Half)1.001f;   // exactly two representable Half values in [min, max]

        Check.ThatCode(() => Dummy.WithSeed(207).Half().Between(min, max).Except(min, max).Generate())
             .Throws<DummyGenerationException>()
             .WithProperty(thrown => thrown.Seed, 207)
             .And.WhichMember(thrown => thrown.Message)
             .Contains("207")
             // The draw came from Dummy.WithSeed(207) — a fixed context that replays by itself — so the hint must name it,
             // not the ambient Dummy.Reproducibly(...) instruction, which would not reproduce this run.
             .And.Contains("Dummy.WithSeed(207)")
             .And.Not.Contains("Dummy.Reproducibly(");
    }

    [Fact(DisplayName = "An exhausted nudge reports a local search, never a claim that the range holds no free value.")]
    public void ExhaustedNudgeDoesNotClaimAnEmptyRange() {
        // A range of 401 representable doubles whose 399 interior values are excluded: both bounds survive, so the
        // range plainly holds free values. They sit further than the 128-step budget from a draw landing mid-range,
        // so both walks give up — and the inner exception used to assert "No representable value in range remains
        // after applying the exclusions", which nothing had established and which is false here. Seed 5 lands in that
        // band on the first draw, so the case is pinned rather than statistical.
        double min = 1d;
        double max = 1d;
        for (int step = 0; step < 400; step++) { max = Math.BitIncrement(max); }

        List<double> excluded = [];
        double       value    = Math.BitIncrement(min);
        for (int step = 0; step < 399; step++) {
            excluded.Add(value);
            value = Math.BitIncrement(value);
        }

        DummyGenerationException thrown = Assert.Throws<DummyGenerationException>(
            () => Dummy.WithSeed(5).Double().Between(min, max).Except(excluded.ToArray()).Generate());

        // The two bounds are free: the range is satisfiable, so any claim that it is empty would be a falsehood.
        Check.That(excluded).Not.Contains(min);
        Check.That(excluded).Not.Contains(max);

        string inner = thrown.InnerException!.Message;
        Check.That(inner).Contains("128 steps");
        Check.That(inner).Contains("not examined");
        Check.That(inner).Not.Contains("No representable value in range remains");
    }

    [Fact(DisplayName = "The nudge stays reproducible: the same seed yields the same value across runs.")]
    public void NudgeIsReproducibleForAGivenSeed() {
        double min = 1d;
        double max = Math.BitIncrement(Math.BitIncrement(1d));

        for (int seed = 0; seed < 50; seed++) {
            double first  = Dummy.WithSeed(seed).Double().Between(min, max).DifferentFrom(min).Generate();
            double second = Dummy.WithSeed(seed).Double().Between(min, max).DifferentFrom(min).Generate();
            Check.That(second).IsEqualTo(first);
        }
    }

}
