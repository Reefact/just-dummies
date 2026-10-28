#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     A conflict message must name the constraint that actually caused the conflict. When an <b>exclusion</b>
///     (<c>NonZero</c>/<c>Except</c>/<c>DifferentFrom</c>) empties the domain, the interval engines used to name a
///     bound instead — or the constraint being applied — producing messages that were self-referential
///     (<c>"Cannot apply Zero() because Zero() already pins the value to 0"</c>) or factually false
///     (<c>"GreaterThanOrEqualTo(5) already pins the value to 5"</c>, which allows 5..MaxValue). Issue #312.
/// </summary>
/// <remarks>
///     Message content is the example suite's job (ADR-0019): these pin the contract "name the excluding
///     constraint", not the exact prose. Each asserts that the offending exclusion appears in the message — the
///     information that was missing — which is red against the old engines and green once exclusions carry
///     provenance. The four interval engines (ordinal, wide, decimal, continuous) share one exhaustion path, so a
///     case per engine guards them all.
/// </remarks>
[TestSubject(typeof(ConflictingAnyConstraintException))]
public sealed class ConflictMessageProvenanceTests {

    #region Statics members declarations

    private static string ConflictMessage(Action build) {
        try {
            build();
        } catch (ConflictingAnyConstraintException exception) {
            return exception.Message;
        }

        return "<no ConflictingAnyConstraintException was thrown>";
    }

    #endregion

    // ----- OrdinalIntervalSpec: integers (and, by sharing the engine, TimeSpan/DateTime/DateOnly/TimeOnly) -----

    [Fact(DisplayName = "A pin emptied by NonZero names NonZero, not the pin itself.")]
    public void PinEmptiedByNonZeroNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Byte().NonZero().Zero());

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("NonZero()");
    }

    [Fact(DisplayName = "A single-value bound emptied by NonZero names NonZero.")]
    public void BoundEmptiedByNonZeroNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Byte().NonZero().LessThan(1));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("NonZero()");
    }

    [Fact(DisplayName = "A range pinned by two bounds and emptied by Except names Except, not a bound.")]
    public void PinEmptiedByExceptNamesTheExclusionNotABound() {
        string message = ConflictMessage(() => Any.Int32().Except(5).GreaterThanOrEqualTo(5).LessThanOrEqualTo(5));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(5)");
    }

    [Fact(DisplayName = "A pin emptied by DifferentFrom names DifferentFrom.")]
    public void PinEmptiedByDifferentFromNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Int32().DifferentFrom(-1).Negative().GreaterThan(-2));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("DifferentFrom(-1)");
    }

    [Fact(DisplayName = "A lattice emptied by Except names Except and the lattice, not the lattice alone.")]
    public void LatticeEmptiedByExceptNamesBothTheExclusionAndTheLattice() {
        string message = ConflictMessage(() => Any.Int32().MultipleOf(5).Except(0).Between(-4, 4));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(0)");
        Check.WithCustomMessage($"The lattice was not named. Message: {message}").That(message).Contains("MultipleOf(5)");
    }

    [Fact(DisplayName = "An allow-list emptied by Except names Except, not just the allow-list.")]
    public void AllowListEmptiedByExceptNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Int32().Except(1, 2).OneOf(1, 2));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(1, 2)");
    }

    // ----- DecimalIntervalSpec -----

    [Fact(DisplayName = "A decimal pin emptied by DifferentFrom names DifferentFrom.")]
    public void DecimalPinEmptiedByDifferentFromNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Decimal().DifferentFrom(1m).Between(1m, 1m));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("DifferentFrom(1)");
    }

    [Fact(DisplayName = "A decimal strict bound crossing an inclusive one says the strict bound excludes its own value.")]
    public void DecimalStrictLowerBoundAboveAnUpperBoundIsWordedAsStrict() {
        string message = ConflictMessage(() => Any.Decimal().GreaterThan(100m).LessThan(10m));

        Check.That(message).Contains("GreaterThan(100) already requires values greater than 100");
        Check.That(message).Not.Contains("or equal to");
    }

    [Fact(DisplayName = "A decimal empty open interval is refused as crossed bounds, not as an exhausted exclusion.")]
    public void DecimalEmptyOpenIntervalIsRefusedAsCrossedBounds() {
        string message = ConflictMessage(() => Any.Decimal().GreaterThan(36m).LessThan(36m));

        Check.That(message).Contains("GreaterThan(36) already requires values greater than 36");
        Check.That(message).Not.Contains("the only value");
    }

    [Fact(DisplayName = "A decimal Negative after Positive is refused as crossed bounds, not as excluded zero.")]
    public void DecimalPositiveThenNegativeIsRefusedAsCrossedBounds() {
        string message = ConflictMessage(() => Any.Decimal().Positive().Negative());

        Check.That(message).Contains("Positive() already requires values greater than 0");
        Check.That(message).Not.Contains("the only value");
    }

    [Fact(DisplayName = "A decimal inclusive bound meeting a strict one at the same point is refused and says so.")]
    public void DecimalInclusiveBoundMeetingAStrictOneIsRefused() {
        string message = ConflictMessage(() => Any.Decimal().GreaterThan(5m).LessThanOrEqualTo(5m));

        Check.That(message).Contains("GreaterThan(5) already requires values greater than 5");
    }

    [Fact(DisplayName = "A decimal strict bound refused over a scaled range names the Between that bounds it.")]
    public void DecimalStrictBoundOverAScaledRangeNamesTheBetween() {
        string message = ConflictMessage(() => Any.Decimal().Between(0m, 5000m).WithScale(2).Negative());

        Check.That(message).Contains("Between(0, 5000)");
    }

    [Fact(DisplayName = "A decimal strict bound emptying a scale lattice does not name itself as its own cause.")]
    public void DecimalStrictBoundEmptyingALatticeIsNotItsOwnCause() {
        // LessThan(0.01) excludes 0.01, the last point Positive() left on the WithScale(2) grid. Listing it among the
        // constraints that forbid the grid named it on both sides of "because" (issue #340).
        string message = ConflictMessage(() => Any.Decimal().Positive().WithScale(2).LessThan(0.01m));

        Check.WithCustomMessage($"The applied constraint is echoed after 'because'. Message: {message}").That(message).Not.Contains("because Positive(), LessThan(0.01)");
        Check.That(message).Contains("Positive() forbids every WithScale(2) value between 0 and 0.01 except 0.01, and it forbids that one");
    }

    [Fact(DisplayName = "A decimal exclusion emptying a scale lattice names only the exclusions declared before it.")]
    public void DecimalExclusionEmptyingALatticeNamesOnlyTheEarlierExclusions() {
        string message = ConflictMessage(() => Any.Decimal().Between(0m, 0.01m).WithScale(2).Except(0m).DifferentFrom(0.01m));

        Check.That(message).Contains("because Except(0) forbids every WithScale(2) value between 0 and 0.01 except 0.01, and it forbids that one");
    }

    [Fact(DisplayName = "A decimal exclusion of several points emptying a scale lattice lists the points the earlier ones left.")]
    public void DecimalExclusionsOfSeveralPointsEmptyingALatticeListTheLeftPoints() {
        // Each Except removes two of the four grid points: the earlier one forbids neither "every other" value nor
        // every value but one, so the message names exactly the points it leaves to the applied one.
        string message = ConflictMessage(() => Any.Decimal().Between(0m, 0.03m).WithScale(2).Except(0m, 0.01m).Except(0.02m, 0.03m));

        Check.That(message).Contains("because Except(0, 0.01) forbids every WithScale(2) value between 0 and 0.03 except 0.02 and 0.03, and it forbids those");
    }

    // ----- ContinuousIntervalSpec: double (and, by sharing the engine, Single/Half) -----

    [Fact(DisplayName = "A double pin emptied by DifferentFrom names DifferentFrom.")]
    public void DoublePinEmptiedByDifferentFromNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Double().DifferentFrom(1d).Between(1d, 1d));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("DifferentFrom(1)");
    }

#if NET8_0_OR_GREATER
    // ----- WideIntervalSpec: Int128/UInt128 (net8.0 leg only) -----

    [Fact(DisplayName = "An Int128 pin emptied by NonZero names NonZero.")]
    public void Int128PinEmptiedByNonZeroNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Int128().NonZero().Between(System.Int128.Zero, System.Int128.Zero));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("NonZero()");
    }
#endif

    // ----- Correctness of the claim, not only the naming (surfaced by the exhaustive audit). -----

    [Fact(DisplayName = "An allow-list narrowed by a bound before an exclusion empties it does not claim the exclusion forbids every allowed value.")]
    public void AllowListNarrowedByABoundIsNotOverclaimed() {
        // OneOf(1, 3) offers two values, but Between(0, 1) already drops 3; Except(1) then removes the only one
        // that survived. Saying Except(1) forbids *every* value OneOf allows would be false — it never forbids 3 —
        // so the claim must be qualified to the values the other constraints leave.
        string message = ConflictMessage(() => Any.Int32().Except(1).Between(0, 1).OneOf(1, 3));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(1)");
        Check.WithCustomMessage($"The message overclaims that Except(1) forbids every value OneOf allows. Message: {message}")
             .That(message).Contains("that the other constraints leave");
    }

    [Fact(DisplayName = "When the applied exclusion is itself the sole cause, the message reads 'it forbids', not the constraint twice.")]
    public void AnExclusionAppliedLastIsNotRepeatedOnBothSides() {
        // Zero() pins the byte to 0; NonZero(), applied last, is itself the forbidder. Repeating "NonZero()" on
        // both sides of "because" reads as circular, so the clause refers back to the applied constraint as "it".
        string message = ConflictMessage(() => Any.Byte().Zero().NonZero());

        Check.WithCustomMessage($"The applied constraint should be referred to as 'it'. Message: {message}").That(message).Contains("it forbids");
        Check.WithCustomMessage($"The applied constraint is echoed after 'because'. Message: {message}").That(message).Not.Contains("because NonZero()");
    }

    // ----- An allow-list emptied by what was declared before it (issue #215). -----
    // Applied last, the allow-list is the side the caller just wrote; the message used to name it on both sides of
    // "because" and never name the bound, sign, lattice or scale that turned every value away.

    [Fact(DisplayName = "An allow-list emptied by a bound declared before it names the bound.")]
    public void AllowListEmptiedByAnEarlierBoundNamesTheBound() {
        string message = ConflictMessage(() => Any.Int32().Between(10, 20).OneOf(1, 2, 3));

        Check.WithCustomMessage($"The bound was not named. Message: {message}").That(message).Contains("Between(10, 20) allows none of its values");
    }

    [Fact(DisplayName = "An allow-list emptied by a lattice or a sign names that constraint, and only the one that refuses every value.")]
    public void AllowListEmptiedByAnEarlierLatticeNamesOnlyTheLattice() {
        // Positive() and LessThan(1000) admit both 10 and 20; only MultipleOf(7) refuses them all, so only it is
        // the constraint whose removal changes the verdict.
        string message = ConflictMessage(() => Any.Int32().Positive().LessThan(1000).MultipleOf(7).OneOf(10, 20));

        Check.WithCustomMessage($"The lattice was not named. Message: {message}").That(message).Contains("MultipleOf(7) allows none of its values");
        Check.WithCustomMessage($"A constraint admitting every value was named. Message: {message}").That(message).Not.Contains("Positive()", "LessThan(1000)");

        string signed = ConflictMessage(() => Any.Int64().Positive().OneOf(-1L, -2L));

        Check.WithCustomMessage($"The sign was not named. Message: {signed}").That(signed).Contains("Positive() allows none of its values");
    }

    [Fact(DisplayName = "An allow-list only the combination of earlier constraints empties names no single one.")]
    public void AllowListEmptiedOnlyByACombinationIsNotPinnedOnOne() {
        // Positive() refuses -2 and MultipleOf(2) refuses 1: each leaves a value standing, so neither alone is to
        // blame and naming one would send the caller to a constraint whose removal leaves the chain refused.
        string message = ConflictMessage(() => Any.Int32().Positive().MultipleOf(2).OneOf(-2, 1));

        Check.WithCustomMessage($"A single constraint was blamed for a combination. Message: {message}").That(message).Not.Contains("allows none of its values");
        Check.That(message).Contains("no value it offers satisfies the constraints already declared");
    }

    [Fact(DisplayName = "A decimal allow-list emptied by a scale declared before it names the scale.")]
    public void DecimalAllowListEmptiedByAnEarlierScaleNamesTheScale() {
        string message = ConflictMessage(() => Any.Decimal().WithScale(1).OneOf(1.234m));

        Check.WithCustomMessage($"The scale was not named. Message: {message}").That(message).Contains("WithScale(1) allows none of its values");
    }

    [Fact(DisplayName = "A double allow-list emptied by a bound declared before it names the bound.")]
    public void DoubleAllowListEmptiedByAnEarlierBoundNamesTheBound() {
        string message = ConflictMessage(() => Any.Double().Between(10d, 20d).OneOf(1.5d));

        Check.WithCustomMessage($"The bound was not named. Message: {message}").That(message).Contains("Between(10, 20) allows none of its values");
    }

#if NET8_0_OR_GREATER
    [Fact(DisplayName = "An Int128 allow-list emptied by a bound declared before it names the bound.")]
    public void Int128AllowListEmptiedByAnEarlierBoundNamesTheBound() {
        string message = ConflictMessage(() => Any.Int128().Between(10, 20).OneOf((Int128)1));

        Check.WithCustomMessage($"The bound was not named. Message: {message}").That(message).Contains("Between(10, 20) allows none of its values");
    }
#endif

    [Fact(DisplayName = "An allow-list declared first is still named as the other side when a bound empties it.")]
    public void AllowListDeclaredFirstIsStillTheOtherSide() {
        string message = ConflictMessage(() => Any.Int32().OneOf(1, 2, 3).Between(10, 20));

        Check.That(message).Contains("Cannot apply Between(10, 20)");
        Check.That(message).Contains("OneOf(1, 2, 3)");
    }

    // ----- Regression guard: bound-vs-bound messages must stay correct and unchanged. -----

    [Fact(DisplayName = "A bound-vs-bound conflict still names the opposing bound (unchanged).")]
    public void BoundVersusBoundStillNamesBothSides() {
        string message = ConflictMessage(() => Any.Int32().Between(1, 10).GreaterThan(50));

        Check.That(message).Contains("Between(1, 10)");
        Check.That(message).Contains("GreaterThan(50)");
    }

}
