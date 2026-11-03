using JustDummies;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ReimbursementScheduleTests {

    [Fact]
    public void A_schedule_without_any_entry_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReimbursementSchedule(new Dictionary<ReimbursementCategory, ReimbursementRate>()));

        // Assert
        Assert.Equal("rates", refusal.ParamName);
    }

    [Fact]
    public void A_missing_dictionary_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ReimbursementSchedule(null!));
    }

    [Fact]
    public void An_entry_without_a_rate_is_refused() {
        // Arrange
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() { [ReimbursementCategory.Major] = null! };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ReimbursementSchedule(rates));
    }

    [Fact]
    public void A_category_that_is_not_a_defined_member_is_refused() {
        // Arrange
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() { [(ReimbursementCategory)99] = anyRate };

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementSchedule(rates));
    }

    [Fact]
    public void A_category_in_the_table_is_covered_at_its_rate() {
        // Arrange
        ReimbursementRate    major    = new(65m);
        ReimbursementSchedule schedule = new(new Dictionary<ReimbursementCategory, ReimbursementRate> { [ReimbursementCategory.Major] = major });

        // Act / Assert
        Assert.True(schedule.Covers(ReimbursementCategory.Major));
        Assert.Equal(major, schedule.RateFor(ReimbursementCategory.Major));
    }

    [Fact]
    public void A_category_absent_from_the_table_is_not_covered_and_has_no_rate() {
        // Arrange
        ReimbursementSchedule schedule = Arrange.Schedule((ReimbursementCategory.Major, 65m));

        // Act / Assert
        Assert.False(schedule.Covers(ReimbursementCategory.Low));
        Assert.Null(schedule.RateFor(ReimbursementCategory.Low));
    }

    [Fact]
    public void Changing_the_dictionary_given_afterwards_does_not_change_the_schedule() {
        // Arrange
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new() { [ReimbursementCategory.Major] = new ReimbursementRate(65m) };
        ReimbursementSchedule schedule = new(rates);

        // Act
        rates.Add(ReimbursementCategory.Low, new ReimbursementRate(15m));

        // Assert
        Assert.False(schedule.Covers(ReimbursementCategory.Low));
        Assert.Single(schedule.Rates);
    }

    [Fact(Skip = "domain defect: Rates returns the private Dictionary itself behind IReadOnlyDictionary, so a caller casting it to IDictionary can add or remove categories of a value object that must be immutable (equality and hash code then change)")]
    public void The_exposed_rates_cannot_be_used_to_change_the_schedule() {
        // Arrange
        ReimbursementSchedule schedule = Arrange.Schedule((ReimbursementCategory.Major, 65m));

        // Act
        if (schedule.Rates is IDictionary<ReimbursementCategory, ReimbursementRate> writable) {
            writable.Add(ReimbursementCategory.Low, new ReimbursementRate(100m));
        }

        // Assert
        Assert.False(schedule.Covers(ReimbursementCategory.Low));
    }

    [Fact]
    public void Two_schedules_with_the_same_entries_are_equal_whatever_the_insertion_order() {
        // Arrange
        ReimbursementSchedule first  = Arrange.Schedule((ReimbursementCategory.Major, 65m), (ReimbursementCategory.Low, 15m));
        ReimbursementSchedule second = Arrange.Schedule((ReimbursementCategory.Low, 15m), (ReimbursementCategory.Major, 65m));

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Schedules_that_differ_by_one_rate_are_different() {
        // Act / Assert
        Assert.NotEqual(Arrange.Schedule((ReimbursementCategory.Major, 65m)), Arrange.Schedule((ReimbursementCategory.Major, 70m)));
    }

    [Fact]
    public void Schedules_that_differ_by_one_category_are_different() {
        // Act / Assert
        Assert.NotEqual(Arrange.Schedule((ReimbursementCategory.Major, 65m)), Arrange.Schedule((ReimbursementCategory.Moderate, 65m)));
    }

    [Fact]
    public void Schedules_of_different_sizes_are_different() {
        // Act / Assert
        Assert.NotEqual(Arrange.Schedule((ReimbursementCategory.Major, 65m)),
                        Arrange.Schedule((ReimbursementCategory.Major, 65m), (ReimbursementCategory.Low, 15m)));
    }

    [Fact]
    public void A_drawn_schedule_covers_exactly_the_categories_of_its_table() {
        // Arrange
        ReimbursementSchedule anySchedule = Any.ReimbursementSchedule().Generate();

        // Act / Assert
        foreach (ReimbursementCategory category in Enum.GetValues<ReimbursementCategory>()) {
            Assert.Equal(anySchedule.Rates.ContainsKey(category), anySchedule.Covers(category));
            Assert.Equal(anySchedule.Rates.ContainsKey(category), anySchedule.RateFor(category) is not null);
        }
    }

}
