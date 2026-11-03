using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Expert.Support;
using JustDummies;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ReimbursementScheduleTests {

    [Fact]
    public void A_category_in_the_table_is_covered_at_its_rate() {
        ReimbursementSchedule schedule = Schedules.Of((ReimbursementCategory.Major, 65m), (ReimbursementCategory.Low, 15m));

        Assert.True(schedule.Covers(ReimbursementCategory.Major));
        Assert.Equal(new ReimbursementRate(65m), schedule.RateFor(ReimbursementCategory.Major));
        Assert.Equal(new ReimbursementRate(15m), schedule.RateFor(ReimbursementCategory.Low));
    }

    [Fact]
    public void A_category_absent_from_the_table_is_not_covered() {
        ReimbursementSchedule schedule = Schedules.Of((ReimbursementCategory.Major, 65m));

        Assert.False(schedule.Covers(ReimbursementCategory.Unlisted));
        Assert.Null(schedule.RateFor(ReimbursementCategory.Unlisted));
    }

    [Fact]
    public void A_schedule_without_a_table_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ReimbursementSchedule(null!));
    }

    [Fact]
    public void A_schedule_without_any_entry_is_refused() {
        Assert.Throws<ArgumentException>(() => new ReimbursementSchedule(new Dictionary<ReimbursementCategory, ReimbursementRate>()));
    }

    [Fact]
    public void A_schedule_keyed_on_an_undefined_category_is_refused() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();
        Dictionary<ReimbursementCategory, ReimbursementRate> table = new() { [(ReimbursementCategory)42] = anyRate };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementSchedule(table));
    }

    [Fact]
    public void A_schedule_with_a_missing_rate_is_refused() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();
        Dictionary<ReimbursementCategory, ReimbursementRate> table = new() {
            [ReimbursementCategory.Major] = anyRate,
            [ReimbursementCategory.Low]   = null!
        };

        Assert.Throws<ArgumentException>(() => new ReimbursementSchedule(table));
    }

    [Fact]
    public void A_schedule_does_not_follow_the_table_it_was_built_from() {
        ReimbursementRate anyRate      = Any.ReimbursementRate().Generate();
        ReimbursementRate anyOtherRate = Any.ReimbursementRate().Generate();
        Dictionary<ReimbursementCategory, ReimbursementRate> table = new() { [ReimbursementCategory.Major] = anyRate };
        ReimbursementSchedule schedule = new(table);

        table[ReimbursementCategory.Low]   = anyOtherRate;
        table[ReimbursementCategory.Major] = anyOtherRate;

        Assert.False(schedule.Covers(ReimbursementCategory.Low));
        Assert.Equal(anyRate, schedule.RateFor(ReimbursementCategory.Major));
    }

    [Fact]
    public void Two_schedules_with_the_same_entries_are_equal_whatever_the_order_they_were_listed_in() {
        ReimbursementRate anyMajorRate = Any.ReimbursementRate().Generate();
        ReimbursementRate anyLowRate   = Any.ReimbursementRate().Generate();
        ReimbursementSchedule majorFirst = new(new Dictionary<ReimbursementCategory, ReimbursementRate> {
            [ReimbursementCategory.Major] = anyMajorRate,
            [ReimbursementCategory.Low]   = anyLowRate
        });
        ReimbursementSchedule lowFirst = new(new Dictionary<ReimbursementCategory, ReimbursementRate> {
            [ReimbursementCategory.Low]   = new ReimbursementRate(anyLowRate.Percent),
            [ReimbursementCategory.Major] = new ReimbursementRate(anyMajorRate.Percent)
        });

        Assert.Equal(majorFirst, lowFirst);
        Assert.Equal(majorFirst.GetHashCode(), lowFirst.GetHashCode());
    }

    [Fact]
    public void Two_schedules_differing_by_one_rate_are_not_equal() {
        ReimbursementRate anyRate      = Any.ReimbursementRate().Generate();
        ReimbursementRate anyOtherRate = Any.ReimbursementRate().WithPercent(percent => percent.DifferentFrom(anyRate.Percent)).Generate();
        ReimbursementSchedule schedule      = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyRate });
        ReimbursementSchedule otherSchedule = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyOtherRate });

        Assert.NotEqual(schedule, otherSchedule);
    }

    [Fact]
    public void Two_schedules_covering_different_categories_are_not_equal() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();
        ReimbursementSchedule major = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyRate });
        ReimbursementSchedule low   = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Low] = anyRate });

        Assert.NotEqual(major, low);
    }

    [Fact]
    public void A_schedule_covering_more_categories_is_not_equal_to_one_covering_fewer() {
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();
        ReimbursementSchedule one = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyRate });
        ReimbursementSchedule two = new(new Dictionary<ReimbursementCategory, ReimbursementRate> {
            [ReimbursementCategory.Major] = anyRate,
            [ReimbursementCategory.Low]   = anyRate
        });

        Assert.NotEqual(one, two);
        Assert.NotEqual(two, one);
    }

}
