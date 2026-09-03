#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Behaviour of the <see cref="DummyDateTimeOffset" /> offset dimension — <c>WithOffset</c> pins it,
///     <c>WithOffsetBetween</c> draws it bounded, the default stays UTC, and values stay valid at the domain edges
///     because the instant is tightened before the offset is drawn.
/// </summary>
public sealed class DummyDateTimeOffsetOffsetTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "Offset: unconstrained, generated values carry UTC (zero) offset.")]
    public void DefaultOffsetIsZero() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.DateTimeOffset().Generate().Offset).IsEqualTo(TimeSpan.Zero);
        }
    }

    [Fact(DisplayName = "WithOffset: every generated value carries the pinned offset.")]
    public void WithOffsetPins() {
        TimeSpan offset = TimeSpan.FromHours(2);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.DateTimeOffset().WithOffset(offset).Generate().Offset).IsEqualTo(offset);
        }
    }

    [Fact(DisplayName = "WithOffsetBetween: offsets stay within the range, in whole minutes, and vary.")]
    public void WithOffsetBetweenBounds() {
        TimeSpan          min  = TimeSpan.FromHours(-5);
        TimeSpan          max  = TimeSpan.FromHours(5);
        HashSet<TimeSpan> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            DateTimeOffset value = Dummy.DateTimeOffset().WithOffsetBetween(min, max).Generate();
            Check.That(value.Offset >= min && value.Offset <= max).IsTrue();
            Check.That(value.Offset.Ticks % TimeSpan.TicksPerMinute).IsEqualTo(0L);
            seen.Add(value.Offset);
        }

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "WithOffset: stays valid and after the floor at the top of the domain.")]
    public void WithOffsetValidNearMaxValue() {
        DateTimeOffset floor = DateTimeOffset.MaxValue.AddDays(-1);
        for (int i = 0; i < SampleCount; i++) {
            DateTimeOffset value = Dummy.DateTimeOffset().After(floor).WithOffset(TimeSpan.FromHours(14)).Generate();
            Check.That(value.Offset).IsEqualTo(TimeSpan.FromHours(14));
            Check.That(value.UtcTicks > floor.UtcTicks).IsTrue();
        }
    }

    [Fact(DisplayName = "WithOffset: an instant window with no room for the offset conflicts eagerly.")]
    public void WithOffsetImpossibleWindowConflicts() {
        // The last 12h of the domain cannot host a +14h offset: the local ticks would overflow.
        Check.ThatCode(() => Dummy.DateTimeOffset().After(DateTimeOffset.MaxValue.AddHours(-12)).WithOffset(TimeSpan.FromHours(14)))
             .Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "WithOffset: arguments are validated (whole minutes, ±14:00, ordered range).")]
    public void WithOffsetArguments() {
        Check.ThatCode(() => Dummy.DateTimeOffset().WithOffset(TimeSpan.FromSeconds(30))).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.DateTimeOffset().WithOffset(TimeSpan.FromHours(15))).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(-2))).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "WithOffset filters the OneOf pool instead of being ignored, in either order.")]
    public void OneOfIsFilteredByTheDeclaredOffset() {
        // ADR-0029 supersedes ADR-0016's accepted risk. A pooled value is still returned verbatim, offset included —
        // rebuilding it from the instant would normalize the offset to UTC — but the offset dimension now decides
        // WHICH pooled values may be drawn, rather than being silently dropped. The public contract of WithOffset says
        // every generated value carries exactly that offset; it now does.
        DateTimeOffset utc      = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset plusFive = new(2021, 1, 1, 0, 0, 0, TimeSpan.FromHours(5));

        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.DateTimeOffset().WithOffset(TimeSpan.Zero).OneOf(utc, plusFive).Generate()).IsEqualTo(utc);
            Check.That(Dummy.DateTimeOffset().OneOf(utc, plusFive).WithOffset(TimeSpan.Zero).Generate()).IsEqualTo(utc);
            Check.That(Dummy.DateTimeOffset().OneOf(utc, plusFive).WithOffset(TimeSpan.FromHours(5)).Generate()).IsEqualTo(plusFive);
        }
    }

    [Fact(DisplayName = "WithOffset: an offset no pooled value carries is a conflict, in either order.")]
    public void AnOffsetNoPooledValueCarriesConflicts() {
        // The other half of the filter. Under the old behaviour both of these silently returned the UTC value,
        // honouring neither the pool's offset nor the one the caller asked for.
        DateTimeOffset utc       = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        TimeSpan       requested = TimeSpan.FromHours(5);

        ConflictingDummyConstraintException afterPool = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.DateTimeOffset().OneOf(utc).WithOffset(requested));
        ConflictingDummyConstraintException beforePool = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.DateTimeOffset().WithOffset(requested).OneOf(utc));

        Check.That(afterPool.Message).Contains("no pooled value carries an offset it admits");
        Check.That(beforePool.Message).Contains("no pooled value carries an offset it admits");
    }

    [Fact(DisplayName = "WithOffset: a conflict raised while declaring the pool names the pool, and blames the offset.")]
    public void APoolConflictNamesThePoolAndBlamesTheOffset() {
        // The two halves of the sentence answer different questions, and the offset is not the answer to both.
        // The pool is what the caller is writing -- WithOffset was accepted on an earlier line -- while the offset
        // is what to loosen. Applying the declared offset to an arriving pool once used one call for both roles,
        // so the message read "Cannot apply WithOffset(...)" and sent the reader at a line that had succeeded.
        // The case runs past the eager "no pooled value carries an offset" guard: `kept` does carry +01:00.
        DateTimeOffset kept    = new(2026, 6, 2, 13, 0, 0, TimeSpan.FromHours(1));
        DateTimeOffset refused = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        Check.ThatCode(() => Dummy.DateTimeOffset().Before(kept).WithOffset(TimeSpan.FromHours(1)).OneOf(refused, kept))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(caught => caught.Message)
             .StartsWith("Cannot apply OneOf(")
             .And.Contains("WithOffset(01:00:00)");
    }

    [Fact(DisplayName = "Without an offset constraint, OneOf still returns every pooled value with its own offset.")]
    public void AnUnconstrainedOneOfKeepsEveryOffset() {
        // The filter must only fire when an offset is actually declared: an unconstrained pool is unchanged, and that
        // half of ADR-0016 stands.
        DateTimeOffset utc      = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset plusFive = new(2021, 1, 1, 0, 0, 0, TimeSpan.FromHours(5));

        HashSet<TimeSpan> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Dummy.DateTimeOffset().OneOf(utc, plusFive).Generate().Offset);
        }

        Check.That(seen).Contains(TimeSpan.Zero, TimeSpan.FromHours(5));
    }

    [Fact(DisplayName = "WithOffset: a second, different offset is rejected as already declared.")]
    public void WithOffsetDeclaredOnce() {
        Check.ThatCode(() => Dummy.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).WithOffset(TimeSpan.FromHours(3)))
             .Throws<ConflictingDummyConstraintException>();
        // The same offset twice is idempotent, not a conflict.
        Check.That(Dummy.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).WithOffset(TimeSpan.FromHours(2)).Generate().Offset)
             .IsEqualTo(TimeSpan.FromHours(2));
    }

}
