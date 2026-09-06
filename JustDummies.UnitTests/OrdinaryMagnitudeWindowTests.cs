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

    /// <summary>
    ///     2^45 — a <see cref="float" /> bound where one ulp is 4 194 304, more than twice the carried width, so
    ///     the whole slab is narrower than a single rung of the row's own ladder.
    /// </summary>
    private const float Single2Pow45 = 35_184_372_088_832f;

    /// <summary>
    ///     2^44 — one ulp is 2 097 152, only just wider than the carried width. The slab still holds one rung, but
    ///     its computed floor now quantizes to the rung <b>below</b> it, which is what makes the ceiling side
    ///     asymmetric with the floor side.
    /// </summary>
    private const float Single2Pow44 = 17_592_186_044_416f;

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

    /// <summary>
    ///     The same, plus the stronger claim a "drawn whole" case actually makes: the sample straddles the declared
    ///     midpoint, so the whole range is reached rather than merely not exceeded.
    /// </summary>
    /// <remarks>
    ///     Containment alone cannot state this. An implementation truncating <c>Between(1e6, 1e300)</c> to any
    ///     sub-range — the carried slab, <c>[1e6, 5e6]</c>, <c>[1e6, 1e20]</c> — satisfies "inside the declared
    ///     interval and not a constant" while violating the policy the case is named for. Straddling the midpoint
    ///     is the assertion that separates them, and it is the shape the decimal reachability regression already
    ///     uses.
    /// </remarks>
    private static void CheckDrawnAcross(string what, Func<double> draw, double floor, double ceiling) {
        CheckDrawnWithin(what, draw, floor, ceiling);

        (double lowest, double highest, int _) = Sample(draw);
        double midpoint = floor / 2d + ceiling / 2d;

        Check.WithCustomMessage($"{what} never drew below the declared midpoint {midpoint}: the range was truncated from above.")
             .That(lowest)
             .IsStrictlyLessThan(midpoint);
        Check.WithCustomMessage($"{what} never drew above the declared midpoint {midpoint}: the range was truncated from below.")
             .That(highest)
             .IsStrictlyGreaterThan(midpoint);
    }

    #endregion

    [Fact(DisplayName = "A two-sided interval declared at the window's edge is drawn whole, never collapsed to its floor.")]
    public void AnIntervalDeclaredAtTheWindowEdgeIsDrawnWhole() {
        // Regression. The window stepped aside only where narrowing left the declared interval EMPTY, so an interval
        // whose floor sat exactly on the edge kept the single point the narrowing left: Between(1e6, 5e6) returned
        // 1000000 on every draw, and Between(999_999, 2e6) one ulp below it did not. The caller owns both bounds
        // here, so the interval they wrote is the interval they get.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnAcross("Between(1e6, 5e6)", () => any.Double().Between(Magnitude, 5d * Magnitude).Generate(), Magnitude, 5d * Magnitude);
        CheckDrawnAcross("Between(-2e6, -1e6)", () => any.Double().Between(-2d * Magnitude, -Magnitude).Generate(), -2d * Magnitude, -Magnitude);
        CheckDrawnAcross("Single.Between(1e6f, 2e6f)", () => any.Single().Between(1e6f, 2e6f).Generate(), Magnitude, 2d * Magnitude);
        CheckDrawnAcross("Decimal.Between(1e6, 5e6).WithScale(2)",
                         () => (double)any.Decimal().Between(1_000_000m, 5_000_000m).WithScale(2).Generate(),
                         Magnitude, 5d * Magnitude);
    }

    [Fact(DisplayName = "A coarse scale at the window's edge keeps every grid point the interval declares.")]
    public void ACoarseScaleAtTheWindowEdgeKeepsEveryDeclaredGridPoint() {
        // Regression. Two distinct endpoints do not make two drawable values once a scale lattice is in force:
        // Between(999_999.5m, 1_000_001m).WithScale(0) narrowed to [999_999.5, 1_000_000], whose only grid point
        // is 1 000 000 — so the draw was a constant again and the 1 000 001 the caller declared was unreachable.
        // The seam is the same one as above, seen through the lattice rather than through the numbers.
        AnyContext any = Any.WithSeed(Seed);

        HashSet<decimal> coarse = [];
        for (int i = 0; i < SampleCount; i++) { coarse.Add(any.Decimal().Between(999_999.5m, 1_000_001m).WithScale(0).Generate()); }

        Check.WithCustomMessage("The scale-0 lattice holds 1 000 000 and 1 000 001; the draw reached only one of them.")
             .That(coarse)
             .IsEqualTo(new HashSet<decimal> { 1_000_000m, 1_000_001m });

        // The mirror side, and a finer lattice, so the fix is not read off the one arrangement that exposed it.
        HashSet<decimal> mirrored = [];
        HashSet<decimal> finer    = [];
        for (int i = 0; i < SampleCount; i++) {
            mirrored.Add(any.Decimal().Between(-1_000_001m, -999_999.5m).WithScale(0).Generate());
            finer.Add(any.Decimal().Between(999_999.95m, 1_000_000.5m).WithScale(1).Generate());
        }

        Check.That(mirrored).IsEqualTo(new HashSet<decimal> { -1_000_001m, -1_000_000m });
        // Stated as the two ends rather than the whole six-point grid: reaching the far end is the regression, and
        // pinning every point in between would make an unrelated change to the seed or the draw count read as one.
        Check.That(finer).Contains(1_000_000.0m, 1_000_000.5m);
    }

    [Fact(DisplayName = "An exclusion counts against the drawable values, not only the lattice.")]
    public void AnExclusionCountsAgainstTheDrawableValues() {
        // Regression. Counting grid points before the exclusions leaves the same singleton one step later:
        // Between(999_999m, 1_000_001m).WithScale(0).Except(999_999m) narrowed to [999_999, 1_000_000], which
        // holds two grid points — but one of them is forbidden, so every candidate nudged onto the other and the
        // 1 000 001 the caller declared stayed unreachable. "Two drawable values" has to mean what survives every
        // constraint, and the same holds for the binary engine, whose ladder is the type's own.
        AnyContext any = Any.WithSeed(Seed);

        HashSet<decimal> excluded = [];
        HashSet<decimal> twoGone  = [];
        for (int i = 0; i < SampleCount; i++) {
            excluded.Add(any.Decimal().Between(999_999m, 1_000_001m).WithScale(0).Except(999_999m).Generate());
            twoGone.Add(any.Decimal().Between(999_998m, 1_000_001m).WithScale(0).Except(999_998m, 999_999m).Generate());
        }

        Check.WithCustomMessage("The exclusion left 1 000 000 and 1 000 001 drawable; the draw reached only one.")
             .That(excluded)
             .IsEqualTo(new HashSet<decimal> { 1_000_000m, 1_000_001m });
        Check.That(twoGone).IsEqualTo(new HashSet<decimal> { 1_000_000m, 1_000_001m });

        // The binary engine carries the same hole, its ladder being the type's representable values rather than a
        // grid. Single makes it deterministic: the two adjacent floats either side of the seam are nameable, so
        // the surviving pair can be pinned exactly rather than sampled for.
        HashSet<float> singles = [];
        for (int i = 0; i < SampleCount; i++) { singles.Add(any.Single().Between(999_999.9375f, 1_000_000.0625f).Except(999_999.9375f).Generate()); }

        Check.WithCustomMessage("The clip kept 999_999.9375f and 1_000_000f, then the exclusion left one of them; 1_000_000.0625f was declared and unreachable.")
             .That(singles)
             .IsEqualTo(new HashSet<float> { 1_000_000f, 1_000_000.0625f });

        // And the double row, where the same shape is reached one ulp below the window's edge.
        double justBelow = ContinuousIntervalSpec.NextDown(Magnitude);

        CheckDrawnWithin("Between(nextDown(1e6), 2e6).Except(nextDown(1e6))",
                         () => any.Double().Between(justBelow, 2d * Magnitude).Except(justBelow).Generate(),
                         justBelow, 2d * Magnitude);
    }

    [Fact(DisplayName = "A span of one ladder step returns both its values, not only the lower one.")]
    public void ASpanOfOneLadderStepReturnsBothItsValues() {
        // Regression, and the last layer of the same invariant. Counting representable values was not enough: the
        // midpoint form cannot resolve a span one ulp wide — half is half an ulp, mid ± half rounds back to mid —
        // so the upper of the pair was unreachable and the draw a constant. ADR-0097 states the rule in terms of
        // values a draw can land on, which made the record false at the one-ulp neighbour of the seam.
        AnyContext any = Any.WithSeed(Seed);

        double justBelow = ContinuousIntervalSpec.NextDown(Magnitude);

        HashSet<double> pair = [];
        for (int i = 0; i < SampleCount; i++) { pair.Add(any.Double().Between(justBelow, 2d * Magnitude).Generate()); }

        Check.WithCustomMessage("The narrowing kept the two doubles either side of the seam; the draw returned only one.")
             .That(pair)
             .IsEqualTo(new HashSet<double> { justBelow, Magnitude });

        // Single, where the pair is nameable, and where the same span with one of them excluded must still leave
        // the declared interval reachable rather than collapsing.
        HashSet<float> singles = [];
        for (int i = 0; i < SampleCount; i++) { singles.Add(any.Single().Between(999_999.9375f, 1_000_000.0625f).Generate()); }

        Check.That(singles).IsEqualTo(new HashSet<float> { 999_999.9375f, 1_000_000f });
    }

    [Fact(DisplayName = "The drawable-value walk stops at the domain edge instead of overflowing past it.")]
    public void TheDrawableValueWalkStopsAtTheDomainEdge() {
        // Regression on the bounded walk itself. Its loop stepped past its last grid point before re-testing the
        // bound, which merely leaves the range everywhere except at decimal.MaxValue — where the step throws. A
        // specification whose only surviving value IS the domain edge was then taken down by the check meant to
        // protect it, rather than falling back to the declared interval and drawing that value.
        AnyContext any = Any.WithSeed(Seed);

        Check.ThatCode(() => any.Decimal().GreaterThanOrEqualTo(decimal.MaxValue - 1m).WithScale(0).Except(decimal.MaxValue - 1m).Generate())
             .DoesNotThrow();
        Check.That(any.Decimal().Between(decimal.MaxValue - 1m, decimal.MaxValue).WithScale(0).Except(decimal.MaxValue - 1m).Generate())
             .IsEqualTo(decimal.MaxValue);

        // The floor side, which walks nowhere near the edge it could fall off, and the unexcluded control that must
        // still reach both grid points.
        Check.That(any.Decimal().LessThanOrEqualTo(decimal.MinValue + 1m).WithScale(0).Except(decimal.MinValue + 1m).Generate())
             .IsEqualTo(decimal.MinValue);

        HashSet<decimal> both = [];
        for (int i = 0; i < SampleCount; i++) { both.Add(any.Decimal().Between(decimal.MaxValue - 1m, decimal.MaxValue).WithScale(0).Generate()); }

        Check.That(both).IsEqualTo(new HashSet<decimal> { decimal.MaxValue - 1m, decimal.MaxValue });
    }

    [Fact(DisplayName = "A scale quantum finer than the representation still advances the drawable-value walk.")]
    public void AScaleFinerThanTheRepresentationStillAdvancesTheWalk() {
        // Regression on the bounded walk, again on this branch's own account. The scale quantum is what the snap
        // rounds onto, not an increment the representation can always make: at a magnitude of 1e6 a decimal carries
        // 22 decimal places, so WithScale(28) asks for 1e-28 and adding it leaves the value exactly where it was.
        // The walk then revisited one excluded point until its budget ran out, reported a singleton that was not
        // one, and the two-sided branch handed back the whole declared interval — putting values above a million
        // inside a draw the window had always kept below it.
        AnyContext any = Any.WithSeed(Seed);

        HashSet<decimal> drawn = [];
        for (int i = 0; i < SampleCount; i++) { drawn.Add(any.Decimal().Between(999_999m, 2_000_000m).WithScale(28).Except(999_999m).Generate()); }

        Check.WithCustomMessage($"The narrowing was lost and the draw reached {drawn.Max()}, above the window the declared interval still fits inside.")
             .That(drawn.Max())
             .IsLessOrEqualThan(1_000_000m);
        Check.That(drawn.Min()).IsGreaterOrEqualThan(999_999m);
        Check.That(drawn.Count).IsStrictlyGreaterThan(1);

        // The controls the fix must leave alone: the same shape without the exclusion, which never entered the
        // walk, and a coarse scale whose quantum does move the value.
        HashSet<decimal> unexcluded = [];
        HashSet<decimal> coarse     = [];
        for (int i = 0; i < SampleCount; i++) {
            unexcluded.Add(any.Decimal().Between(999_999m, 2_000_000m).WithScale(28).Generate());
            coarse.Add(any.Decimal().Between(999_999m, 2_000_000m).WithScale(2).Except(999_999m).Generate());
        }

        Check.That(unexcluded.Max()).IsLessOrEqualThan(1_000_000m);
        Check.That(coarse.Max()).IsLessOrEqualThan(1_000_000m);
        Check.That(coarse).Not.Contains(999_999m);
    }

    [Fact(DisplayName = "A carried slab narrower than one rung of the row's ladder is not taken.")]
    public void ACarriedSlabNarrowerThanOneRungIsNotTaken() {
        // Regression on the second branch, and the same invariant one level further out: this spec computes in
        // double while a row draws its own type, so "the carried width moved the endpoint" is not "the slab holds
        // two values this row can return". At 2^45f one float ulp is 4 194 304 — wider than the whole 2e6 slab —
        // so both of its ends cast back to the same float and the draw collapsed onto the declared bound, where
        // before this branch it fell back to the declared domain. The slab now answers to the same ladder the
        // narrowing does.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnAcross("Single.GreaterThanOrEqualTo(2^45f)",
                         () => any.Single().GreaterThanOrEqualTo(Single2Pow45).Generate(),
                         Single2Pow45, float.MaxValue);
        CheckDrawnAcross("Single.LessThanOrEqualTo(-2^45f)",
                         () => any.Single().LessThanOrEqualTo(-Single2Pow45).Generate(),
                         -float.MaxValue, -Single2Pow45);
        CheckDrawnAcross("Single.GreaterThan(2^45f)",
                         () => any.Single().GreaterThan(Single2Pow45).Generate(),
                         Single2Pow45, float.MaxValue);

        // The control: a bound low enough that one slab width still spans millions of floats keeps its slab, so
        // the ladder check narrows nothing it should not.
        CheckDrawnWithin("Single.GreaterThanOrEqualTo(1e7f)",
                         () => any.Single().GreaterThanOrEqualTo(1e7f).Generate(),
                         1e7d, 1e7d + SlabWidth);
    }

    [Fact(DisplayName = "A slab whose computed floor is not a rung is counted from the first rung inside it.")]
    public void ASlabWhoseFloorIsNotARungIsCountedFromInsideIt() {
        // Regression, and the asymmetric half of the case above. A declared bound arrives representable, but the
        // carried CEILING slab computes its floor as `_max - width` in double, and that need not be a rung: at
        // -2^44f one float ulp is 2 097 152 against a carried width of 2 000 000, so the floor quantizes to the
        // float BELOW the slab. Counting the rungs from there credited one the slab does not contain, so a
        // one-rung slab was taken as a pair — and the sampler then returned that out-of-slab value itself, with
        // half of 4 000 draws landing beneath the slab's own floor. The floor slab never showed this: its _min is
        // the bound the caller declared, and so already a rung.
        AnyContext any = Any.WithSeed(Seed);

        CheckDrawnAcross("Single.LessThanOrEqualTo(-2^44f)",
                         () => any.Single().LessThanOrEqualTo(-Single2Pow44).Generate(),
                         -float.MaxValue, -Single2Pow44);
        CheckDrawnAcross("Single.LessThan(-2^44f)",
                         () => any.Single().LessThan(-Single2Pow44).Generate(),
                         -float.MaxValue, -Single2Pow44);

        // The floor side at the same magnitude, which was already right and must stay so.
        CheckDrawnAcross("Single.GreaterThanOrEqualTo(2^44f)",
                         () => any.Single().GreaterThanOrEqualTo(Single2Pow44).Generate(),
                         Single2Pow44, float.MaxValue);

        // And the mirror of the control: a ceiling bound low enough that its slab still spans millions of floats
        // keeps that slab, so starting the count on the ladder narrows nothing it should not.
        CheckDrawnWithin("Single.LessThanOrEqualTo(-1e7f)",
                         () => any.Single().LessThanOrEqualTo(-1e7f).Generate(),
                         -1e7d - SlabWidth, -1e7d);
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

        // Across, not merely within: containment plus "not the carried slab" would still be satisfied by an
        // implementation truncating this 294-decade range to [1e6, 5e6] or [1e6, 1e20]. Straddling the declared
        // midpoint is what states the policy the case is named for.
        CheckDrawnAcross("Between(1e6, 1e300)", () => any.Double().Between(Magnitude, 1e300d).Generate(), Magnitude, 1e300d);
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

        CheckDrawnAcross("Between(1e300, 1e308)", () => any.Double().Between(1e300d, 1e308d).Generate(), 1e300d, 1e308d);
        CheckDrawnAcross("GreaterThanOrEqualTo(1e300)", () => any.Double().GreaterThanOrEqualTo(1e300d).Generate(), 1e300d, double.MaxValue);
        CheckDrawnAcross("GreaterThan(1e300)", () => any.Double().GreaterThan(1e300d).Generate(), 1e300d, double.MaxValue);
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
        CheckDrawnAcross("Half.Between(1000, 2000)", () => (double)any.Half().Between((Half)1000, (Half)2000).Generate(), 1_000d, 2_000d);
        CheckDrawnWithin("Half.GreaterThanOrEqualTo(60000)", () => (double)any.Half().GreaterThanOrEqualTo((Half)60_000).Generate(), 60_000d, 65_504d);
    }
#endif

}
