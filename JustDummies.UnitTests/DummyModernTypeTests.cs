#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class DummyModernTypeTests {

    private const int SampleCount = 200;

    private static readonly DateOnly AnchorDate = new(2026, 1, 1);
    private static readonly TimeOnly AnchorTime = new(12, 0, 0);

    [Fact(DisplayName = "Distinct: the half row counts its own representable values, so a floor beyond them conflicts rather than exhausting.")]
    public void DistinctOverHalvesConflictsBeforeDrawing() {
        // Lives here rather than beside the other collection cases: this file is the one the .NET Framework
        // 4.7.2 floor leg excludes, and Half does not exist on that floor.
        // Sixteen bits hold 63 487 distinct finite values -- the two zeros compare equal, so a set keeps one of them.
        // The shared interval specification answers null for a floating-point range and DummyHalf carries the count
        // itself; without it this floor was accepted, then failed only after a redraw budget sized from the ask
        // (64 x 200 000) rather than from the domain.
        Check.ThatCode(() => Dummy.SetOf(Dummy.Half()).WithMinCount(200_000).Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("63487");

        // A floor the domain does hold is still drawn, so the count refuses nothing it can satisfy.
        Check.ThatCode(() => Dummy.SetOf(Dummy.Half()).WithCount(64).Generate()).DoesNotThrow();
    }

    [Fact(DisplayName = "Half is untouched by the ordinary-magnitude window: its whole domain is already ordinary.")]
    public void HalfIsUnaffectedByTheOrdinaryWindow() {
        // ADR-0031. Half stops at 65 504, well inside the window, so clipping to a window wider than the domain
        // changes nothing — the rule narrows where a type is extravagant and stays silent where it is not. It lives
        // in this file rather than beside the other continuous examples because Half is a .NET 5+ type, absent from
        // the .NET Framework 4.7.2 floor leg this file is excluded from.
        //
        // Still true, and it was long read as saying more than it does: being inside the window never made the
        // magnitudes inside it reachable. That was a separate defect, closed by drawing over the representable
        // values (ADR-0091), and the window remains a no-op here either way — which is what this pins.
        for (int i = 0; i < SampleCount; i++) {
            Check.That((double)Dummy.Half().Generate()).IsStrictlyLessThan(65_505d);
        }
    }

    [Fact(DisplayName = "Half reaches the ordinary magnitudes inside its domain, not only the widest gaps.")]
    public void HalfReachesTheMagnitudesInsideItsDomain() {
        // The defect ADR-0091 closed, stated as the test that would have caught it: drawing uniformly over the real
        // interval and rounding produced NOTHING below 1 in 200 000 draws, because the halves near zero have
        // rounding intervals too narrow to land in. One below 1 and one at or above it, over a modest sample.
        double[] drawn = Enumerable.Range(0, SampleCount).Select(_ => (double)Dummy.Half().Generate()).ToArray();

        Check.That(drawn.Any(value => Math.Abs(value) < 1d)).IsTrue();
        Check.That(drawn.Any(value => Math.Abs(value) >= 1d)).IsTrue();
    }

    [Fact(DisplayName = "DateOnly: Between is inclusive and reached; After/Before are exclusive; conflicts surface.")]
    public void DateOnlyBehaves() {
        HashSet<DateOnly> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            DateOnly value = Dummy.DateOnly().Between(AnchorDate, AnchorDate.AddDays(2)).Generate();
            seen.Add(value);
            Check.That(value >= AnchorDate && value <= AnchorDate.AddDays(2)).IsTrue();
            Check.That(Dummy.DateOnly().After(AnchorDate).Before(AnchorDate.AddDays(2)).Generate()).IsEqualTo(AnchorDate.AddDays(1));
        }
        Check.That(seen.Contains(AnchorDate)).IsTrue();
        Check.That(seen.Contains(AnchorDate.AddDays(2))).IsTrue();

        Check.ThatCode(() => Dummy.DateOnly().After(DateOnly.MaxValue)).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.DateOnly().Between(AnchorDate.AddDays(1), AnchorDate)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "DateOnly: OneOf/Except/DifferentFrom behave.")]
    public void DateOnlySets() {
        DateOnly[] allowed = [AnchorDate, AnchorDate.AddDays(7)];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Dummy.DateOnly().OneOf(allowed).Generate())).IsTrue();
            Check.That(Dummy.DateOnly().OneOf(allowed).Except(AnchorDate).Generate()).IsEqualTo(AnchorDate.AddDays(7));
            Check.That(Dummy.DateOnly().OneOf(allowed).DifferentFrom(AnchorDate.AddDays(7)).Generate()).IsEqualTo(AnchorDate);
        }
    }

    [Fact(DisplayName = "TimeOnly: bounds behave and the exclusive window pins the middle tick.")]
    public void TimeOnlyBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            TimeOnly value = Dummy.TimeOnly().Between(AnchorTime, AnchorTime.Add(TimeSpan.FromMinutes(5))).Generate();
            Check.That(value >= AnchorTime && value <= AnchorTime.Add(TimeSpan.FromMinutes(5))).IsTrue();

            TimeOnly middle = Dummy.TimeOnly().After(AnchorTime).Before(new TimeOnly(AnchorTime.Ticks + 2)).Generate();
            Check.That(middle.Ticks).IsEqualTo(AnchorTime.Ticks + 1);
        }

        Check.ThatCode(() => Dummy.TimeOnly().After(TimeOnly.MaxValue)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Int128: signs, pins, full-width variety, extremes and conflicts.")]
    public void Int128Behaves() {
        HashSet<Int128> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Dummy.Int128().Generate());
            Check.That(Dummy.Int128().Positive().Generate() > 0).IsTrue();
            Check.That(Dummy.Int128().Negative().Generate() < 0).IsTrue();

            Int128 bounded = Dummy.Int128().Between(1, 3).Generate();
            Check.That(bounded >= 1 && bounded <= 3).IsTrue();
        }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.That(Dummy.Int128().Zero().Generate() == 0).IsTrue();
        Check.ThatCode(() => Dummy.Int128().GreaterThan(Int128.MaxValue)).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Int128().Positive().Negative()).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "UInt128: bounds, exclusivity and full-width variety.")]
    public void UInt128Behaves() {
        HashSet<UInt128> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Dummy.UInt128().Generate());

            UInt128 bounded = Dummy.UInt128().Between(1, 3).Generate();
            Check.That(bounded >= 1 && bounded <= 3).IsTrue();
            Check.That(Dummy.UInt128().GreaterThan(5).LessThanOrEqualTo(6).Generate() == 6).IsTrue();
        }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.That(Dummy.UInt128().Zero().Generate() == 0).IsTrue();
        Check.ThatCode(() => Dummy.UInt128().GreaterThan(UInt128.MaxValue)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Half: finite draws, strict Positive, pinned Zero, contained bounds, argument checks.")]
    public void HalfBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            Half value = Dummy.Half().Generate();
            Check.That(Half.IsNaN(value) || Half.IsInfinity(value)).IsFalse();
            Check.That(Dummy.Half().Positive().Generate() > Half.Zero).IsTrue();

            Half bounded = Dummy.Half().Between((Half)1f, (Half)2f).Generate();
            Check.That(bounded >= (Half)1f && bounded <= (Half)2f).IsTrue();
        }

        Check.That(Dummy.Half().Zero().Generate() == Half.Zero).IsTrue();
        Check.ThatCode(() => Dummy.Half().GreaterThan(Half.NaN)).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.Half().GreaterThan(Half.MaxValue)).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Half().Positive().Negative()).Throws<ConflictingDummyConstraintException>();
    }

}
