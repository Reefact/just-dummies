#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class AnyTimeTests {

    private const int SampleCount = 200;

    private static readonly DateTime Anchor = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact(DisplayName = "TimeSpan: Positive and Negative are strict against zero, and contradict each other.")]
    public void TimeSpanSigns() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.TimeSpan().Positive().Generate() > TimeSpan.Zero).IsTrue();
            Check.That(Dummy.TimeSpan().Negative().Generate() < TimeSpan.Zero).IsTrue();
        }

        Check.ThatCode(() => Dummy.TimeSpan().Positive().Negative())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Negative()", "Positive()");
    }

    [Fact(DisplayName = "TimeSpan: Zero pins, Between is inclusive over a tiny tick window, GreaterThan is exclusive.")]
    public void TimeSpanBounds() {
        Check.That(Dummy.TimeSpan().Zero().Generate()).IsEqualTo(TimeSpan.Zero);

        HashSet<long> ticks = [];
        for (int i = 0; i < SampleCount; i++) {
            TimeSpan value = Dummy.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromTicks(3)).Generate();
            ticks.Add(value.Ticks);
            Check.That(value.Ticks).IsGreaterOrEqualThan(1L);
            Check.That(value.Ticks).IsLessOrEqualThan(3L);

            Check.That(Dummy.TimeSpan().GreaterThan(TimeSpan.FromTicks(5)).LessThanOrEqualTo(TimeSpan.FromTicks(6)).Generate().Ticks).IsEqualTo(6L);
        }
        Check.That(ticks.Contains(1L)).IsTrue();
        Check.That(ticks.Contains(3L)).IsTrue();
    }

    [Fact(DisplayName = "DateTime: every generated value carries Utc kind.")]
    public void DateTimeIsUtc() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.DateTime().Generate().Kind == DateTimeKind.Utc).IsTrue();
            Check.That(Dummy.DateTime().Between(Anchor, Anchor.AddDays(1)).Generate().Kind == DateTimeKind.Utc).IsTrue();
        }
    }

    [Fact(DisplayName = "DateTime: After and Before are exclusive — a three-tick window pins the middle tick.")]
    public void DateTimeExclusiveWindow() {
        for (int i = 0; i < SampleCount; i++) {
            DateTime value = Dummy.DateTime().After(Anchor).Before(Anchor.AddTicks(2)).Generate();
            Check.That(value.Ticks).IsEqualTo(Anchor.Ticks + 1);
        }
    }

    [Fact(DisplayName = "DateTime: an impossible After/Before pair conflicts naming both sides; crossed Between is an argument error.")]
    public void DateTimeConflicts() {
        Check.ThatCode(() => Dummy.DateTime().After(Anchor).Before(Anchor))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Before(", "After(");

        Check.ThatCode(() => Dummy.DateTime().Between(Anchor.AddDays(1), Anchor)).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.DateTime().After(DateTime.MaxValue)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "DateTime: OneOf yields only supplied instants and DifferentFrom always picks the other of two.")]
    public void DateTimeSets() {
        DateTime[] allowed = [Anchor, Anchor.AddDays(1)];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Dummy.DateTime().OneOf(allowed).Generate())).IsTrue();
            Check.That(Dummy.DateTime().Between(Anchor, Anchor.AddTicks(1)).DifferentFrom(Anchor).Generate().Ticks).IsEqualTo(Anchor.Ticks + 1);
        }
    }

    [Fact(DisplayName = "DateTimeOffset: generated values carry a zero offset, and comparisons work by instant.")]
    public void DateTimeOffsetIsUtc() {
        DateTimeOffset start = new(Anchor, TimeSpan.Zero);
        for (int i = 0; i < SampleCount; i++) {
            DateTimeOffset value = Dummy.DateTimeOffset().Between(start, start.AddDays(1)).Generate();
            Check.That(value.Offset).IsEqualTo(TimeSpan.Zero);
        }

        // A +02:00 bound constrains by UtcTicks: the exclusive three-tick window still pins the middle tick.
        DateTimeOffset shifted = start.ToOffset(TimeSpan.FromHours(2));
        for (int i = 0; i < SampleCount; i++) {
            DateTimeOffset value = Dummy.DateTimeOffset().After(shifted).Before(shifted.AddTicks(2)).Generate();
            Check.That(value.UtcTicks).IsEqualTo(start.UtcTicks + 1);
        }
    }

    [Fact(DisplayName = "DateTimeOffset: OneOf returns the supplied values as given, offset included.")]
    public void DateTimeOffsetOneOfPreservesOffsets() {
        DateTimeOffset supplied = new(2026, 7, 18, 10, 0, 0, TimeSpan.FromHours(2));
        for (int i = 0; i < SampleCount; i++) {
            DateTimeOffset value = Dummy.DateTimeOffset().OneOf(supplied).Generate();
            Check.That(value).IsEqualTo(supplied);
            Check.That(value.Offset).IsEqualTo(TimeSpan.FromHours(2));
        }
    }

    [Fact(DisplayName = "DateTimeOffset: Except excludes by instant.")]
    public void DateTimeOffsetExceptByInstant() {
        DateTimeOffset start = new(Anchor, TimeSpan.Zero);
        for (int i = 0; i < SampleCount; i++) {
            DateTimeOffset value = Dummy.DateTimeOffset().Between(start, start.AddTicks(1)).Except(start).Generate();
            Check.That(value.UtcTicks).IsEqualTo(start.UtcTicks + 1);
        }
    }

}
