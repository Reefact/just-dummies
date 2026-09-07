#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Regression coverage for issue #187. On <see cref="decimal" /> an exclusive bound is an inclusive bound plus a
///     point exclusion, so <c>LessThan(x)</c> excludes the interval's own ceiling — and the walk that carries a
///     colliding draw off an excluded point used to go one way, upwards, by a nominal <c>1E-28</c> step. Two things
///     followed, and each refused a declaration that held a free value. Ascending cannot leave an exclusion sitting
///     on the ceiling, so the two exclusive spellings behaved differently on the very same set; and past the
///     magnitude where <c>1E-28</c> still changes the value — a <see cref="decimal" /> keeps 28 decimal places at
///     one, but only 22 at a million — the step vanished into rounding and the walk declared itself stuck where
///     nothing had moved.
/// </summary>
public sealed class DecimalExclusionNudgeTests {

    private const int SeedCount = 500;

    // 1.0 and the next value a decimal can represent above it: at this magnitude the step really is 1E-28.
    private const decimal One          = 1.0m;
    private const decimal NextAboveOne = 1.0000000000000000000000000001m;

    // A million and its next representable neighbour: seven integer digits leave only 22 decimal places, so the
    // step here is 1E-22 — six decades coarser, and the reason a nominal 1E-28 step moves nothing.
    private const decimal Million          = 1000000m;
    private const decimal NextAboveMillion = 1000000.0000000000000000000001m;

    [Fact(DisplayName = "An exclusion on the ceiling of a two-value interval yields the surviving lower value for every seed.")]
    public void AnExclusionOnTheCeilingIsLeftByDescending() {
        // [1.0, 1.0 + 1E-28] holds exactly two representable decimals, and LessThan excludes the upper one. The
        // single free value therefore sits BELOW every collision — where a walk that only ascends cannot reach it.
        for (int seed = 0; seed < SeedCount; seed++) {
            decimal value = Any.WithSeed(seed).Decimal().Between(One, NextAboveOne).LessThan(NextAboveOne).Generate();

            Check.That(value).IsEqualTo(One);
        }
    }

    [Fact(DisplayName = "An exclusion on the floor of the same interval yields the surviving upper value for every seed.")]
    public void AnExclusionOnTheFloorIsLeftByAscending() {
        // The mirror image, and the half that always worked — it is here so the two are read as a pair. A walk that
        // only ascends passes this case and fails the one above, on a set the two spellings describe identically.
        for (int seed = 0; seed < SeedCount; seed++) {
            decimal value = Any.WithSeed(seed).Decimal().Between(One, NextAboveOne).GreaterThan(One).Generate();

            Check.That(value).IsEqualTo(NextAboveOne);
        }
    }

    [Fact(DisplayName = "Where the nominal step cannot change the value, the walk widens it until the magnitude can move — both exclusive spellings.")]
    public void TheNominalStepWidensUntilTheMagnitudeCanMove() {
        // Same shape one magnitude up, where adding 1E-28 returns the value unchanged: the step has to widen by
        // decades until the representation can carry it, in whichever direction the free value lies.
        for (int seed = 0; seed < SeedCount; seed++) {
            decimal above = Any.WithSeed(seed).Decimal().Between(Million, NextAboveMillion).GreaterThan(Million).Generate();
            decimal below = Any.WithSeed(seed).Decimal().Between(Million, NextAboveMillion).LessThan(NextAboveMillion).Generate();

            Check.That(above).IsEqualTo(NextAboveMillion);
            Check.That(below).IsEqualTo(Million);
        }
    }

    [Fact(DisplayName = "The grid walk widens its step the same way, in both directions.")]
    public void TheGridWalkWidensItsStepTheSameWay() {
        // WithScale(28) asks for a 1E-28 grid, which at a million is finer than the magnitude can hold — so the
        // grid step vanishes exactly as the nominal one did. It is a second call site, and the two are independent:
        // restoring the plain step in either one leaves the other's cases green.
        for (int seed = 0; seed < SeedCount; seed++) {
            decimal above = Any.WithSeed(seed).Decimal().Between(Million, NextAboveMillion).WithScale(28).GreaterThan(Million).Generate();
            decimal below = Any.WithSeed(seed).Decimal().Between(Million, NextAboveMillion).WithScale(28).LessThan(NextAboveMillion).Generate();

            Check.That(above).IsEqualTo(NextAboveMillion);
            Check.That(below).IsEqualTo(Million);
        }
    }

    [Fact(DisplayName = "At the domain's own edge both exclusive spellings draw, and reach every value their exclusion leaves.")]
    public void AtTheDomainEdgeBothSpellingsReachEveryFreeValue() {
        // [MinValue, MinValue + 2] holds three values and LessThan excludes one: two survive, and BOTH have to be
        // drawable. A walk that always settled on the same bound would quietly halve an already tiny domain.
        SortedSet<decimal> below = [];
        SortedSet<decimal> above = [];
        for (int seed = 0; seed < SeedCount; seed++) {
            below.Add(Any.WithSeed(seed).Decimal().LessThan(decimal.MinValue + 2m).Generate());
            above.Add(Any.WithSeed(seed).Decimal().GreaterThan(decimal.MaxValue - 2m).Generate());
        }

        Check.That(below).HasSize(2);
        Check.That(below).Contains(decimal.MinValue, decimal.MinValue + 1m);
        Check.That(above).HasSize(2);
        Check.That(above).Contains(decimal.MaxValue - 1m, decimal.MaxValue);
    }

    [Fact(DisplayName = "An explicitly bounded interval at the edge reaches every value its exclusion leaves, either side.")]
    public void AnExplicitlyBoundedIntervalAtTheEdgeReachesEveryFreeValue() {
        // Between(...) at the domain's edge is drawn from a different interval than the one-sided spellings above,
        // so it exercises the fix through the other branch. Ten values survive on each side — at this magnitude a
        // decimal carries no fractional digit at all, so ten is the whole of what there is.
        SortedSet<decimal> below = [];
        SortedSet<decimal> above = [];
        for (int seed = 0; seed < SeedCount; seed++) {
            below.Add(Any.WithSeed(seed).Decimal().Between(decimal.MaxValue - 10m, decimal.MaxValue).LessThan(decimal.MaxValue).Generate());
            above.Add(Any.WithSeed(seed).Decimal().Between(decimal.MinValue, decimal.MinValue + 10m).GreaterThan(decimal.MinValue).Generate());
        }

        Check.That(below).HasSize(10);
        Check.That(below).Not.Contains(decimal.MaxValue);
        Check.That(above).HasSize(10);
        Check.That(above).Not.Contains(decimal.MinValue);
    }

    [Fact(DisplayName = "A neighbourhood the walk genuinely cannot leave still refuses, and still says it searched near the candidate.")]
    public void AGenuinelyBlockedNeighbourhoodStillRefuses() {
        // The fix widens which declarations generate; it must not widen what the walk claims to have established.
        // Three hundred contiguous excluded values sit further than the budget from a draw landing among them,
        // while both ends of the range stay free — so the refusal is local and has to keep reading as local. Seed 6
        // lands inside the band, which makes the case pinned rather than statistical.
        List<decimal> excluded = [];
        for (int step = 50; step < 350; step++) { excluded.Add(decimal.MaxValue - step); }

        AnyGenerationException thrown = Assert.Throws<AnyGenerationException>(
            () => Any.WithSeed(6).Decimal().Between(decimal.MaxValue - 400m, decimal.MaxValue).Except(excluded.ToArray()).Generate());

        // Both ends are free, so the declaration is satisfiable and any claim that it is empty would be false.
        Check.That(excluded).Not.Contains(decimal.MaxValue - 400m);
        Check.That(excluded).Not.Contains(decimal.MaxValue);
        Check.That(thrown.Message).Contains("near the drawn candidate");
        Check.That(thrown.InnerException!.Message).Contains("could not leave the excluded point within the allowed range");
    }

    [Fact(DisplayName = "The walk stays reproducible: the same seed yields the same value across runs.")]
    public void TheWalkStaysReproducibleForAGivenSeed() {
        // The walk happens after the draw, so a non-deterministic one would break replay while every value it
        // returned still satisfied the declaration. Ten values are reachable here, which gives it room to drift.
        for (int seed = 0; seed < SeedCount; seed++) {
            decimal first  = Any.WithSeed(seed).Decimal().Between(decimal.MaxValue - 10m, decimal.MaxValue).LessThan(decimal.MaxValue).Generate();
            decimal second = Any.WithSeed(seed).Decimal().Between(decimal.MaxValue - 10m, decimal.MaxValue).LessThan(decimal.MaxValue).Generate();

            Check.That(second).IsEqualTo(first);
        }
    }

}
