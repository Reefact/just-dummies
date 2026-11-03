using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ReimbursementScheduleTests {

    [Fact]
    public void A_schedule_exposes_the_rate_of_each_category_it_lists() {
        // Arrange
        ReimbursementRate majorRate    = new(65m);
        ReimbursementRate moderateRate = new(30m);
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() {
            [ReimbursementCategory.Major]    = majorRate,
            [ReimbursementCategory.Moderate] = moderateRate
        };

        // Act
        ReimbursementSchedule schedule = new(rates);

        // Assert
        Assert.Equal(majorRate, schedule.RateFor(ReimbursementCategory.Major));
        Assert.Equal(moderateRate, schedule.RateFor(ReimbursementCategory.Moderate));
        Assert.Equal(2, schedule.Rates.Count);
    }

    [Fact]
    public void A_category_absent_from_the_table_is_not_covered() {
        // Arrange
        ReimbursementSchedule schedule = Any.ReimbursementSchedule()
                                            .WithRates(new Dictionary<ReimbursementCategory, ReimbursementRate> {
                                                [ReimbursementCategory.Major] = Any.ReimbursementRate().Generate()
                                            })
                                            .Generate();

        // Act & Assert
        Assert.True(schedule.Covers(ReimbursementCategory.Major));
        Assert.False(schedule.Covers(ReimbursementCategory.Low));
        Assert.Null(schedule.RateFor(ReimbursementCategory.Low));
    }

    [Fact]
    public void A_schedule_with_no_entry_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReimbursementSchedule(new Dictionary<ReimbursementCategory, ReimbursementRate>()));

        // Assert
        Assert.Equal("rates", refusal.ParamName);
    }

    [Fact]
    public void A_schedule_without_a_table_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReimbursementSchedule(null!));
    }

    [Fact]
    public void A_category_that_is_not_declared_is_refused() {
        // Arrange
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() {
            [(ReimbursementCategory)99] = Any.ReimbursementRate().Generate()
        };

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementSchedule(rates));

        // Assert
        Assert.Equal("rates", refusal.ParamName);
    }

    [Fact]
    public void A_missing_rate_is_refused() {
        // Arrange
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() {
            [ReimbursementCategory.Low] = null!
        };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReimbursementSchedule(rates));

        // Assert
        Assert.Equal("rates", refusal.ParamName);
    }

    [Fact]
    public void A_schedule_does_not_follow_later_changes_to_the_dictionary_it_was_built_from() {
        // Arrange
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() {
            [ReimbursementCategory.Major] = Any.ReimbursementRate().Generate()
        };
        ReimbursementSchedule schedule = new(rates);

        // Act
        rates[ReimbursementCategory.Low] = Any.ReimbursementRate().Generate();

        // Assert
        Assert.False(schedule.Covers(ReimbursementCategory.Low));
    }

    [Fact]
    public void Two_schedules_with_the_same_entries_are_equal_whatever_the_order_they_were_listed_in() {
        // Arrange
        ReimbursementSchedule first = new(new Dictionary<ReimbursementCategory, ReimbursementRate> {
            [ReimbursementCategory.Major] = new(65m),
            [ReimbursementCategory.Low]   = new(15m)
        });
        ReimbursementSchedule second = new(new Dictionary<ReimbursementCategory, ReimbursementRate> {
            [ReimbursementCategory.Low]   = new(15m),
            [ReimbursementCategory.Major] = new(65m)
        });

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_schedules_differing_by_a_rate_are_not_equal() {
        // Arrange
        ReimbursementSchedule first  = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = new(65m) });
        ReimbursementSchedule second = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = new(60m) });

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Two_schedules_differing_by_a_category_are_not_equal() {
        // Arrange
        ReimbursementRate     anyRate = Any.ReimbursementRate().Generate();
        ReimbursementSchedule first   = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyRate });
        ReimbursementSchedule second  = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Moderate] = anyRate });

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_schedule_with_an_extra_entry_is_not_equal() {
        // Arrange
        ReimbursementRate     anyRate = Any.ReimbursementRate().Generate();
        ReimbursementSchedule first   = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyRate });
        ReimbursementSchedule second  = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = anyRate, [ReimbursementCategory.Low] = anyRate });

        // Assert
        Assert.NotEqual(first, second);
        Assert.NotEqual(second, first);
    }

}
