#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The <see cref="decimal" /> sampler's interpolation at the edge of the type's own domain: a declared
///     interval the library accepts and reports satisfiable must not then fail on the arithmetic that draws from
///     it. Named cases rather than properties (ADR-0019) — each is a coordinate of one defect, and the rate that
///     made it a defect is the reason the counts below are what they are.
/// </summary>
/// <remarks>
///     The two algebraically identical forms fail on complementary interval shapes, so the engine picks by the
///     interval's sign and, when both ends share one, anchors the offset to the nearer of them. The failure this
///     regresses was a bare <c>OverflowException</c> — from <c>System</c>, carrying no seed and naming no
///     constraint — on roughly one draw in a thousand.
/// </remarks>
public sealed class DecimalDomainEdgeInterpolationTests {

    /// <summary>
    ///     Enough draws that the defect is certain rather than likely: it struck about one draw in a thousand, so
    ///     twenty thousand expects some twenty failures and a pinned seed makes "some" deterministic.
    /// </summary>
    private const int SampleCount = 20_000;

    private const int Seed = 20260906;

    [Fact(DisplayName = "A decimal interval within a unit of the domain edge draws instead of overflowing.")]
    public void AnIntervalAtTheDomainEdgeDrawsInsteadOfOverflowing() {
        // Regression. The convex combination rounds each of its two products to the type's 28-29 significant
        // digits; where both endpoints sit within a unit or two of the edge, both products carry the same sign
        // and their rounding errors sum past it. decimal throws where the binary types saturate, so the draw died
        // with a System.OverflowException — intermittently, and naming nothing the test had written.
        AnyContext any = Any.WithSeed(Seed);

        Check.ThatCode(() => {
                  for (int i = 0; i < SampleCount; i++) { any.Decimal().GreaterThanOrEqualTo(decimal.MaxValue - 1m).Generate(); }
              })
             .DoesNotThrow();
        Check.ThatCode(() => {
                  for (int i = 0; i < SampleCount; i++) { any.Decimal().LessThanOrEqualTo(decimal.MinValue + 1m).Generate(); }
              })
             .DoesNotThrow();
    }

    [Fact(DisplayName = "Every draw at the domain edge lands inside the declared interval.")]
    public void EveryDrawAtTheDomainEdgeLandsInsideTheDeclaredInterval() {
        // The other half of the same defect, and the one a clamp would hide: the interpolation could also land
        // outside [lower, upper] without overflowing. Asserted on the drawn values rather than on the absence of
        // an exception, because the engine clamps afterwards and a clamp turns a wrong value into a biased one.
        AnyContext any = Any.WithSeed(Seed);

        for (int i = 0; i < SampleCount; i++) {
            decimal high = any.Decimal().GreaterThanOrEqualTo(decimal.MaxValue - 1m).Generate();
            decimal low  = any.Decimal().LessThanOrEqualTo(decimal.MinValue + 1m).Generate();

            Check.That(high).IsGreaterOrEqualThan(decimal.MaxValue - 1m);
            Check.That(low).IsLessOrEqualThan(decimal.MinValue + 1m);
        }
    }

    [Fact(DisplayName = "The interpolation returns its endpoints exactly, at either end of the domain.")]
    public void TheInterpolationReturnsItsEndpointsExactly() {
        // Why the offset is anchored to the NEARER endpoint rather than always to the floor. Anchoring to the
        // floor leaves one hole: on a wide interval `upper - lower` needs more significant digits than the type
        // carries and rounds up, so `lower + (upper - lower)` lands past `upper` — past decimal.MaxValue itself
        // where that is the ceiling. The engine reaches fraction 1 only when all 96 mantissa bits come up 1, so
        // this is asserted on the arithmetic rather than through a draw that would never make it.
        decimal wideFloor = 4639949771532854111014110215.5m;

        (decimal Lower, decimal Upper)[] intervals = [
            (wideFloor, decimal.MaxValue),
            (decimal.MaxValue - 1m, decimal.MaxValue),
            (decimal.MinValue, decimal.MinValue + 1m),
            (decimal.MinValue, -wideFloor),
            (1m, decimal.MaxValue),
            (decimal.MinValue, decimal.MaxValue),
            (0m, 10_000m)
        ];

        foreach ((decimal lower, decimal upper) in intervals) {
            Check.WithCustomMessage($"[{lower}, {upper}] did not return its floor at fraction 0.")
                 .That(DecimalIntervalSpec.Interpolated(lower, upper, 0m))
                 .IsEqualTo(lower);
            Check.WithCustomMessage($"[{lower}, {upper}] did not return its ceiling at fraction 1.")
                 .That(DecimalIntervalSpec.Interpolated(lower, upper, 1m))
                 .IsEqualTo(upper);
        }
    }

    [Fact(DisplayName = "The controls the interval-shaped branch must leave alone are unchanged.")]
    public void TheControlsTheBranchMustLeaveAloneAreUnchanged() {
        // An interval straddling zero keeps the convex combination, which is the form the unconstrained draw was
        // written for — `upper - lower` is what overflows there, the full domain spanning twice decimal.MaxValue.
        // These are the cases the fix must not disturb, and the ordinary ones agree with the old form value for
        // value: SeedGoldenMasterTests pins two of them and did not move.
        AnyContext any = Any.WithSeed(Seed);

        Check.ThatCode(() => {
                  for (int i = 0; i < SampleCount; i++) {
                      any.Decimal().Generate();
                      any.Decimal().Between(1_000_000m, 5_000_000m).Generate();
                      any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();
                      any.Decimal().Between(-1_000_000m, 1_000_000m).Generate();
                  }
              })
             .DoesNotThrow();
    }

}
