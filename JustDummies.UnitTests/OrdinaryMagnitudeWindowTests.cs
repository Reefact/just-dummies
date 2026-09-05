#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The seam of the ordinary-magnitude window (ADR-0031): what a generator draws when a declared bound lands
///     exactly on the window's edge, where narrowing the declared interval leaves fewer than two values. Each case
///     is a named coordinate of the rule rather than a quantity, so they are examples rather than properties
///     (ADR-0019) — and the defect they regress was invisible to the property suite precisely because its
///     expectation mirrored the windowing formula instead of stating it.
/// </summary>
/// <remarks>
///     The rule, in the terms a caller reads it in: a one-sided constraint stays ordinary <b>around</b> the bound it
///     declares; an explicitly bounded interval belongs to the caller and is drawn whole; a one-sided bound so
///     extraordinary that no ordinary slab is representable beside it falls back to the declared domain.
/// </remarks>
public sealed class OrdinaryMagnitudeWindowTests {

    /// <summary>The window's half-width — the magnitude a declared bound has to land on to reach this seam.</summary>
    private const double Magnitude = 1_000_000d;

    /// <summary>The width the window carries to a declared bound when the other side is the type's own edge.</summary>
    private const double SlabWidth = 2d * Magnitude;

    private const int SampleCount = 400;

    /// <summary>The seed every case draws under, so a failure names one run rather than a flaky one.</summary>
    private const int Seed = 20260905;

    #region Statics members declarations

    private static (double Lowest, double Highest, int Distinct) Sample(Func<double> draw) {
        double        lowest  = double.MaxValue;
        double        highest = double.MinValue;
        HashSet<double> seen  = [];
        for (int i = 0; i < SampleCount; i++) {
            double value = draw();
            seen.Add(value);
            if (value < lowest) { lowest   = value; }
            if (value > highest) { highest = value; }
        }

        return (lowest, highest, seen.Count);
    }

    /// <summary>Asserts the draw stays inside <c>[floor, ceiling]</c> and is a draw rather than a constant.</summary>
    private static void CheckDrawnWithin(string what, Func<double> draw, double floor, double ceiling) {
        (double lowest, double highest, int distinct) = Sample(draw);

        Check.WithCustomMessage($"{what} drew below {floor}.").That(lowest).IsGreaterOrEqualThan(floor);
        Check.WithCustomMessage($"{what} drew above {ceiling}.").That(highest).IsLessOrEqualThan(ceiling);
        Check.WithCustomMessage($"{what} collapsed to the single value {lowest}: a dummy nobody can tell from a hand-picked literal.")
             .That(distinct)
             .IsStrictlyGreaterThan(1);
    }

    #endregion

    [Fact(DisplayName = "A two-sided interval declared at the window's edge is drawn whole, never collapsed to its floor.")]
    public void AnIntervalDeclaredAtTheWindowEdgeIsDrawnWhole() {
        // Regression. The window stepped aside only where narrowing left the declared interval EMPTY, so an interval
        // whose floor sat exactly on the edge kept the single point the narrowing left: Between(1e6, 5e6) returned
        // 1000000 on every draw, and Between(999_999, 2e6) one ulp below it did not. The caller owns both bounds
        // here, so the interval they wrote is the interval they get.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("Between(1e6, 5e6)", () => any.Double().Between(Magnitude, 5d * Magnitude).Generate(), Magnitude, 5d * Magnitude);
        CheckDrawnWithin("Between(-2e6, -1e6)", () => any.Double().Between(-2d * Magnitude, -Magnitude).Generate(), -2d * Magnitude, -Magnitude);
        CheckDrawnWithin("Single.Between(1e6f, 2e6f)", () => any.Single().Between(1e6f, 2e6f).Generate(), Magnitude, 2d * Magnitude);
        CheckDrawnWithin("Decimal.Between(1e6, 5e6).WithScale(2)",
                         () => (double)any.Decimal().Between(1_000_000m, 5_000_000m).WithScale(2).Generate(),
                         Magnitude, 5d * Magnitude);
    }

    [Fact(DisplayName = "A decimal exclusive bound on the window's edge generates instead of failing.")]
    public void ADecimalExclusiveBoundOnTheWindowEdgeGenerates() {
        // Regression, and the loudest symptom of the same seam. GreaterThan on a decimal is an inclusive bound plus
        // a point exclusion; with the narrowing collapsed onto that single point, the drawn candidate WAS the
        // excluded one, and the 1E-28 nudge vanishes in rounding at this magnitude — so an amount above a million
        // threw AnyGenerationException on every single draw.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("Decimal.GreaterThan(1e6)",
                         () => (double)any.Decimal().GreaterThan(1_000_000m).Generate(),
                         Magnitude, Magnitude + SlabWidth);
        CheckDrawnWithin("Decimal.LessThan(-1e6)",
                         () => (double)any.Decimal().LessThan(-1_000_000m).Generate(),
                         -Magnitude - SlabWidth, -Magnitude);
    }

    [Fact(DisplayName = "A one-sided bound on the window's edge draws an ordinary slab beside it, not the whole domain.")]
    public void AOneSidedBoundOnTheWindowEdgeDrawsAnOrdinarySlab() {
        // The other half of the seam. The bound the caller wrote is kept; the domain edge on the far side is a
        // permission rather than a request, so the window is carried to the declared bound instead of being dropped
        // — which is what keeps this from drawing at 1e308, the magnitude ADR-0031 exists to avoid.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("GreaterThanOrEqualTo(1e6)", () => any.Double().GreaterThanOrEqualTo(Magnitude).Generate(), Magnitude, Magnitude + SlabWidth);
        CheckDrawnWithin("LessThanOrEqualTo(-1e6)", () => any.Double().LessThanOrEqualTo(-Magnitude).Generate(), -Magnitude - SlabWidth, -Magnitude);
        CheckDrawnWithin("Single.GreaterThanOrEqualTo(1e6f)", () => any.Single().GreaterThanOrEqualTo(1e6f).Generate(), Magnitude, Magnitude + SlabWidth);
        CheckDrawnWithin("Decimal.GreaterThanOrEqualTo(1e6)",
                         () => (double)any.Decimal().GreaterThanOrEqualTo(1_000_000m).Generate(),
                         Magnitude, Magnitude + SlabWidth);
    }

    [Fact(DisplayName = "Inclusive and exclusive spellings of the same bound draw at the same magnitude.")]
    public void TheTwoSpellingsOfABoundDrawAtTheSameMagnitude() {
        // Regression. GreaterThan moves the bound one ulp, which used to empty the narrowing rather than collapse
        // it — so the two branches parted: GreaterThanOrEqualTo(1e6) returned the constant 1000000 while
        // GreaterThan(1e6) drew around 1e308. One ulp of declared difference, 300 decades of drawn difference.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("GreaterThan(1e6)", () => any.Double().GreaterThan(Magnitude).Generate(), Magnitude, Magnitude + SlabWidth);
        CheckDrawnWithin("LessThan(-1e6)", () => any.Double().LessThan(-Magnitude).Generate(), -Magnitude - SlabWidth, -Magnitude);
    }

    [Fact(DisplayName = "A bound spelled against the type's own edge draws as the one-sided constraint it equals.")]
    public void ABoundSpelledAgainstTheDomainEdgeDrawsAsItsOneSidedTwin() {
        // Between(1e6, double.MaxValue) and GreaterThanOrEqualTo(1e6) denote the SAME set, so they must draw alike.
        // What decides the branch is therefore the VALUE of the opposing bound, never whether a constraint declared
        // it: reading the constraint would split these two spellings 300 decades apart.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("Between(1e6, double.MaxValue)", () => any.Double().Between(Magnitude, double.MaxValue).Generate(), Magnitude, Magnitude + SlabWidth);
        CheckDrawnWithin("Between(double.MinValue, -1e6)", () => any.Double().Between(double.MinValue, -Magnitude).Generate(), -Magnitude - SlabWidth, -Magnitude);
    }

    [Fact(DisplayName = "An explicitly bounded interval is drawn whole however wide it is.")]
    public void AnExplicitlyBoundedIntervalIsDrawnWholeHoweverWide() {
        // The decided policy for the last open case: both bounds were written by the caller, so treating that as
        // intentional is the simplest and most predictable rule. A caller who wanted "at least a million, but
        // otherwise ordinary" writes GreaterThanOrEqualTo(1e6) and gets the slab above.
        AnyContext any = Any.WithSeed(Seed);

        (double lowest, double highest, int distinct) = Sample(() => any.Double().Between(Magnitude, 1e300d).Generate());

        Check.That(lowest).IsGreaterOrEqualThan(Magnitude);
        Check.That(highest).IsLessOrEqualThan(1e300d);
        Check.That(distinct).IsStrictlyGreaterThan(1);
        Check.WithCustomMessage("The declared interval was truncated to an ordinary slab instead of being drawn whole.")
             .That(highest)
             .IsStrictlyGreaterThan(Magnitude + SlabWidth);
    }

    [Fact(DisplayName = "The window still narrows everything it narrowed before.")]
    public void TheWindowStillNarrowsWhatItAlreadyNarrowed() {
        // The controls. Every one of these already held, and the seam fix must not have widened any of them: an
        // unconstrained draw, a merely permitted magnitude, and a bound one ulp short of the window's edge.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("Double()", () => any.Double().Generate(), -Magnitude, Magnitude);
        CheckDrawnWithin("Single()", () => any.Single().Generate(), -Magnitude, Magnitude);
        CheckDrawnWithin("Decimal()", () => (double)any.Decimal().Generate(), -Magnitude, Magnitude);
        CheckDrawnWithin("Between(0, double.MaxValue)", () => any.Double().Between(0d, double.MaxValue).Generate(), 0d, Magnitude);
        CheckDrawnWithin("Between(999_999, 2e6)", () => any.Double().Between(999_999d, 2d * Magnitude).Generate(), 999_999d, Magnitude);
        CheckDrawnWithin("Positive()", () => any.Double().Positive().Generate(), double.Epsilon, Magnitude);
    }

    [Fact(DisplayName = "A magnitude beyond the window is still reached, one-sided or two-sided.")]
    public void AMagnitudeBeyondTheWindowIsStillReached() {
        // The other controls: where no ordinary slab is representable beside the declared bound, the declared
        // interval stands. These are the cases ADR-0031 was already right about, and the fix leaves them alone.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("Between(1e300, 1e308)", () => any.Double().Between(1e300d, 1e308d).Generate(), 1e300d, 1e308d);
        CheckDrawnWithin("GreaterThanOrEqualTo(1e300)", () => any.Double().GreaterThanOrEqualTo(1e300d).Generate(), 1e300d, double.MaxValue);
        CheckDrawnWithin("GreaterThan(1e300)", () => any.Double().GreaterThan(1e300d).Generate(), 1e300d, double.MaxValue);
    }

#if NET8_0_OR_GREATER
    [Fact(DisplayName = "Half is untouched: its whole domain already lies inside the window.")]
    public void HalfIsUntouched() {
        // Half stops at 65 504, so the narrowing never leaves fewer than two values and the seam is unreachable —
        // the reason ADR-0031 needs no special case for it, restated as a guard against one creeping in.
        // Conditioned in-source rather than moved to AnyModernTypeTests: this is the file's one net8-only draw
        // among cases the .NET Framework 4.7.2 floor must keep running, which is the split the test project's
        // net472 <Compile Remove> list draws.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnWithin("Half()", () => (double)any.Half().Generate(), -65_504d, 65_504d);
        CheckDrawnWithin("Half.Between(1000, 2000)", () => (double)any.Half().Between((Half)1000, (Half)2000).Generate(), 1_000d, 2_000d);
        CheckDrawnWithin("Half.GreaterThanOrEqualTo(60000)", () => (double)any.Half().GreaterThanOrEqualTo((Half)60_000).Generate(), 60_000d, 65_504d);
    }
#endif

}
