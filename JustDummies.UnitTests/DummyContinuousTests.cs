#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The example-based half of the continuous generators' contract: conflict messages, the named domain
///     extremes, the exclusion families the property suite leaves alone, and the seeded regression for issue
///     #206. Containment, strictness, inclusiveness, sign handling and the rejection of non-finite arguments
///     hold for <i>every</i> bound and are quantified in <c>JustDummies.PropertyTests</c> (ADR-0019); the #206
///     regression stays here because it pins the interval where the defect actually occurred.
/// </summary>
public sealed class DummyContinuousTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "An unconstrained draw survives ordinary arithmetic, on every continuous type.")]
    public void UnconstrainedDrawsSurviveOrdinaryArithmetic() {
        // Regression, ADR-0031. Measured before the ordinary-magnitude window existed: uniform sampling over a
        // type's whole domain put 16.1 % of Positive() doubles where a single multiplication overflows to
        // Infinity, and 17.1 % of decimals where the same multiplication throws OverflowException. Neither was a
        // defect of the code under test — the dummy itself was breaking the Arrange.
        for (int i = 0; i < SampleCount; i++) {
            Check.That(IsFinite(Dummy.Double().Generate() * 1.2d)).IsTrue();
            Check.That(IsFinite(Dummy.Double().Positive().Generate() * 1.2d)).IsTrue();
            Check.That(IsFinite(Dummy.Single().Generate() * 1.2f)).IsTrue();
            Check.ThatCode(() => Dummy.Decimal().Generate() * 1.2m).DoesNotThrow();
        }
    }

    /// <summary>
    ///     Finiteness, spelled the way the .NET Framework 4.7.2 floor leg understands: <c>double.IsFinite</c> arrived
    ///     with .NET Core 3.0, and this suite is built against the support floor too.
    /// </summary>
    private static bool IsFinite(double value) {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    [Fact(DisplayName = "A scale constraint still constrains: an unconstrained decimal has room for its fraction.")]
    public void AScaleConstraintKeepsItsMeaning() {
        // ADR-0031 restores what the old default emptied out. Near decimal.MaxValue a value has no fractional
        // digits left, so WithScale(2) was satisfied by every draw and constrained none of them: 5000/5000
        // "honoured", every one of them a 29-digit integer.
        bool anyFraction = false;
        for (int i = 0; i < SampleCount; i++) {
            decimal value = Dummy.Decimal().WithScale(2).Generate();

            Check.That(value).IsEqualTo(Math.Round(value, 2));
            if (value != Math.Truncate(value)) { anyFraction = true; }
        }

        Check.WithCustomMessage("No draw carried a fractional part, so WithScale(2) constrained nothing.")
             .That(anyFraction)
             .IsTrue();
    }

    [Fact(DisplayName = "A named magnitude is honoured; a merely permitted one is not targeted.")]
    public void ANamedMagnitudeIsHonouredAndAPermittedOneIsNot() {
        // The two named coordinates of the rule, at the extremes the property suite deliberately leaves to an
        // example: asking for a magnitude and merely allowing one.
        Check.That(Dummy.Double().Between(1e300d, 1e308d).Generate()).IsStrictlyGreaterThan(1e300d * 0.99d);
        Check.That(Dummy.Double().GreaterThan(1e300d).Generate()).IsStrictlyGreaterThan(1e300d);

        Check.That(Math.Abs(Dummy.Double().Between(0d, double.MaxValue).Generate())).IsStrictlyLessThan(1.000001e6d);
    }

    [Fact(DisplayName = "Double: sign constraints are strict, Zero pins, NonZero excludes.")]
    public void DoubleSignFamily() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Double().Positive().Generate()).IsStrictlyGreaterThan(0d);
            Check.That(Dummy.Double().Negative().Generate()).IsStrictlyLessThan(0d);
            Check.That(Dummy.Double().NonZero().Generate()).IsNotEqualTo(0d);
        }
        Check.That(Dummy.Double().Zero().Generate()).IsEqualTo(0d);
        Check.ThatCode(() => Dummy.Double().Zero().NonZero()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Double().Positive().Negative()).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Double: Between contains, GreaterThan is strict, and conflicts name both sides.")]
    public void DoubleBounds() {
        for (int i = 0; i < SampleCount; i++) {
            double bounded = Dummy.Double().Between(1d, 2d).Generate();
            Check.That(bounded).IsGreaterOrEqualThan(1d);
            Check.That(bounded).IsLessOrEqualThan(2d);
            Check.That(Dummy.Double().GreaterThan(1d).LessThanOrEqualTo(2d).Generate()).IsStrictlyGreaterThan(1d);
        }

        Check.ThatCode(() => Dummy.Double().GreaterThan(100d).LessThan(10d))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("LessThan(10)", "GreaterThan(100)");
        Check.ThatCode(() => Dummy.Double().GreaterThan(double.MaxValue)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Double: OneOf stays within and Except/DifferentFrom never yield the excluded value.")]
    public void DoubleSets() {
        double[] allowed = [1.5d, 2.5d];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Dummy.Double().OneOf(allowed).Generate())).IsTrue();
            Check.That(Dummy.Double().OneOf(allowed).Except(1.5d).Generate()).IsEqualTo(2.5d);
            Check.That(Dummy.Double().OneOf(allowed).DifferentFrom(2.5d).Generate()).IsEqualTo(1.5d);
        }
    }

    [Fact(DisplayName = "Single: finite draws, strict signs, bounds contained, NaN rejected.")]
    public void SingleBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            float value = Dummy.Single().Generate();
            Check.That(float.IsNaN(value) || float.IsInfinity(value)).IsFalse();
            Check.That(Dummy.Single().Positive().Generate()).IsStrictlyGreaterThan(0f);

            float bounded = Dummy.Single().Between(1f, 2f).Generate();
            Check.That(bounded).IsGreaterOrEqualThan(1f);
            Check.That(bounded).IsLessOrEqualThan(2f);
        }

        Check.That(Dummy.Single().Zero().Generate()).IsEqualTo(0f);
        Check.ThatCode(() => Dummy.Single().GreaterThan(float.NaN)).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.Single().GreaterThan(float.MaxValue)).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Single().Positive().Negative()).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Decimal: strict signs, pinned zero, contained bounds, and strict GreaterThan via exclusion.")]
    public void DecimalBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Decimal().Positive().Generate()).IsStrictlyGreaterThan(0m);
            Check.That(Dummy.Decimal().Negative().Generate()).IsStrictlyLessThan(0m);

            decimal bounded = Dummy.Decimal().Between(1m, 2m).Generate();
            Check.That(bounded).IsGreaterOrEqualThan(1m);
            Check.That(bounded).IsLessOrEqualThan(2m);
            Check.That(Dummy.Decimal().Between(1m, 2m).GreaterThan(1m).Generate()).IsStrictlyGreaterThan(1m);
        }

        Check.That(Dummy.Decimal().Zero().Generate()).IsEqualTo(0m);
        Check.ThatCode(() => Dummy.Decimal().Zero().NonZero()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Decimal().Between(10m, 1m)).Throws<ArgumentException>();

        Check.ThatCode(() => Dummy.Decimal().GreaterThan(100m).LessThan(10m))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("LessThan(10)", "GreaterThan(100)");
    }

    [Fact(DisplayName = "Decimal: Between reaches both halves of a range, up to near the inclusive maximum.")]
    public void DecimalBetweenReachesBothHalves() {
        // Regression for #206: the fraction was built from three non-negative Random.Next() draws over
        // the full 96-bit mantissa denominator, so each limb's top bit stayed zero, the fraction never
        // crossed ~0.5, and every candidate fell in [min, mid). Seeded and deterministic — both halves,
        // and a value near the inclusive maximum, must be observed.
        const decimal min = 0m;
        const decimal max = 100m;
        const decimal mid = 50m;

        DummyContext any = Dummy.WithSeed(20260721);

        decimal lowest  = decimal.MaxValue;
        decimal highest = decimal.MinValue;
        for (int i = 0; i < 5000; i++) {
            decimal value = any.Decimal().Between(min, max).Generate();
            Check.That(value).IsGreaterOrEqualThan(min);
            Check.That(value).IsLessOrEqualThan(max);
            if (value < lowest) { lowest   = value; }
            if (value > highest) { highest = value; }
        }

        Check.That(lowest).IsStrictlyLessThan(mid);     // the lower half stays covered
        Check.That(highest).IsStrictlyGreaterThan(mid); // the upper half — unreachable before the fix
        Check.That(highest).IsStrictlyGreaterThan(99m); // and up to near the inclusive maximum
    }

    [Fact(DisplayName = "Every continuous generator materializes its own value type through Generate().")]
    public void MaterializesEachValueType() {
        double  d = Dummy.Double().Between(1d, 2d).Generate();
        float   f = Dummy.Single().Between(1f, 2f).Generate();
        decimal m = Dummy.Decimal().Between(1m, 2m).Generate();

        Check.That(d).IsGreaterOrEqualThan(1d);
        Check.That(f).IsGreaterOrEqualThan(1f);
        Check.That(m).IsGreaterOrEqualThan(1m);
    }

}
