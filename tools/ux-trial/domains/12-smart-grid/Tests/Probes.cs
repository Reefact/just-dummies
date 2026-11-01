using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests;

/// <summary>Deliberate edge cases: what the library refuses, how it says so, and what replays.</summary>
public sealed class Probes {

    private static readonly TimeSpan HalfHour = TimeSpan.FromMinutes(30);

    [Fact]
    public void Crossed_date_bounds_fail_fast_naming_both_sides() {
        DateOnly newYear = new DateOnly(2025, 1, 1);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(newYear).Before(newYear));

        Assert.Equal("Cannot apply Before(2025-01-01) because After(2025-01-01) already requires values greater than or equal to 2025-01-02.", conflict.Message);
    }

    [Fact]
    public void A_half_hour_grid_with_no_point_in_range_is_refused_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeOnly().WithGranularity(HalfHour).Between(new TimeOnly(10, 10), new TimeOnly(10, 20)));

        Assert.Contains("Between(10:10:00.0000000, 10:20:00.0000000)", conflict.Message);
        Assert.Contains("WithGranularity(00:30:00)", conflict.Message);
    }

    [Fact]
    public void A_half_hour_grid_draws_only_slot_starts_inside_an_unaligned_range() {
        for (int draw = 0; draw < 200; draw++) {
            TimeOnly anySlot = Any.TimeOnly().WithGranularity(HalfHour).Between(new TimeOnly(10, 10), new TimeOnly(11, 20)).Generate();

            Assert.Contains(anySlot, new[] { new TimeOnly(10, 30), new TimeOnly(11, 0) });
        }
    }

    [Fact]
    public void A_time_range_wrapping_midnight_is_drawn_across_it() {
        // An off-peak window such as 22:00-06:00 is one generator: the start after the end crosses midnight (issue #357).
        AnyTimeOnly offPeak = Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0));

        for (int i = 0; i < 200; i++) { Assert.True(offPeak.Generate().IsBetween(new TimeOnly(22, 0), new TimeOnly(6, 0, 0, 1))); }
    }

    [Fact]
    public void Granularities_larger_than_a_day_collapse_to_midnight() {
        TimeOnly anyTime = Any.TimeOnly().WithGranularity(TimeSpan.FromHours(25)).Generate();

        Assert.Equal(TimeOnly.MinValue, anyTime);
    }

    [Fact]
    public void Calendar_edges_are_honoured_or_refused() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().Between(DateOnly.MaxValue, DateOnly.MaxValue).Generate());
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().AfterOrEqualTo(DateOnly.MaxValue).Generate());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().Before(DateOnly.MinValue));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.TimeOnly().After(TimeOnly.MaxValue));
    }

    [Fact]
    public void A_scale_with_no_grid_point_between_equal_bounds_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Between(1.5m, 1.5m).WithScale(0));

        Assert.Contains("WithScale(0)", conflict.Message);
        Assert.Contains("1.5", conflict.Message);
    }

    [Fact]
    public void WithScale_over_a_pool_is_a_value_lattice_not_a_representation() {
        // The ADR (0015) says so; the user page says WithScale "fixes the number of decimal places".
        decimal anyPooled = Any.Decimal().WithScale(1).OneOf(2.50m).Generate();

        Assert.Equal(2, anyPooled.Scale);
        Assert.Equal(2, Any.Decimal().Between(0m, 10m).WithScale(2).Generate().Scale);
    }

    [Fact]
    public void Decimal_crossed_bounds_message_states_GreaterThan_as_strict() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().GreaterThan(36m).LessThan(35m));

        // Before #311: "... GreaterThan(36) already requires values greater than or equal to 36.", although
        // GreaterThan(36) excludes 36. The Int32 and DateOnly messages always stated the bound correctly.
        Assert.DoesNotContain("greater than or equal to 36.", conflict.Message);
    }

    [Fact]
    public void Decimal_empty_open_interval_message_claims_no_value_is_left() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().GreaterThan(36m).LessThan(36m));

        // Before #311: "... GreaterThan(36) forbids 36, the only value GreaterThan(36) and LessThan(36) leave."
        // No value is left at all; the Int32 equivalent says "already requires values greater than or equal to 37."
        Assert.DoesNotContain("the only value GreaterThan(36) and LessThan(36) leave", conflict.Message);
    }

    [Fact]
    public void Pattern_digits_are_ascii_and_unsupported_constructs_are_refused_by_name() {
        for (int draw = 0; draw < 200; draw++) {
            string anyId = Any.StringMatching(@"^\d{14}$").Generate();

            Assert.All(anyId, digit => Assert.InRange(digit, '0', '9'));
        }
        UnsupportedRegexException category = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"\p{Nd}{14}"));
        UnsupportedRegexException boundary = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"\b\d{14}\b"));

        Assert.Contains("Unicode category", category.Message);
        Assert.Contains("word-boundary", boundary.Message);
    }

    [Fact]
    public void A_full_day_of_distinct_half_hour_slots_is_exactly_48() {
        HashSet<TimeOnly> anyDay = Any.SetOf(Any.TimeOnly().WithGranularity(HalfHour)).WithCount(48).Generate();
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.TimeOnly().WithGranularity(HalfHour)).WithCount(49).Generate());

        Assert.Equal(48, anyDay.Count);
        Assert.Equal("The generator can produce only 48 distinct value(s), not enough for the 49 elements it must still supply to honour WithCount(49).", conflict.Message);
    }

    [Fact]
    public void The_same_seed_replays_a_whole_load_curve() {
        DailyLoadCurve first;
        DailyLoadCurve second;
        using (Any.UseSeed(20261027)) {
            first = Arbitrary.DailyLoadCurve().Generate();
        }
        using (Any.UseSeed(20261027)) {
            second = Arbitrary.DailyLoadCurve().Generate();
        }

        Assert.Equal(first.DeliveryPoint, second.DeliveryPoint);
        Assert.Equal(first.Day, second.Day);
        Assert.Equal(first.Points.Select(point => point.AveragePowerKw), second.Points.Select(point => point.AveragePowerKw));
    }

    [Fact]
    public void A_failing_reproducible_body_reports_a_seed_that_replays_it() {
        string?  report    = null;
        DateOnly firstDraw = default;
        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            firstDraw = Any.DateOnly().Generate();
            throw new InvalidOperationException("deliberate");
        }, message => report = message));
        int seed = int.Parse(report!.Split("seeded with ")[1].Split('.')[0]);

        DateOnly replayed = default;
        Any.Reproducibly(seed, () => replayed = Any.DateOnly().Generate());

        Assert.Equal(firstDraw, replayed);
    }

}
