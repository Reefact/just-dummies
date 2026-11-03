using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.ValueObjects;

public sealed class PlanningPeriodTests {

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(13)]
    public void A_period_lasts_from_one_to_thirteen_weeks(int weekCount) {
        // Arrange
        IsoWeek anyFirstWeek = new AnyIsoWeek().Generate();

        // Act
        PlanningPeriod period = new PlanningPeriod(anyFirstWeek, weekCount);

        // Assert
        Assert.Equal(weekCount, period.WeekCount);
        Assert.Same(anyFirstWeek, period.FirstWeek);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(-1)]
    public void A_period_of_another_length_is_refused(int weekCount) {
        // Arrange
        IsoWeek anyFirstWeek = new AnyIsoWeek().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlanningPeriod(anyFirstWeek, weekCount));
    }

    [Fact]
    public void A_period_needs_a_first_week() {
        Assert.Throws<ArgumentNullException>(() => new PlanningPeriod(null!, 4));
    }

    [Fact]
    public void A_period_starts_on_the_monday_of_its_first_week_and_ends_on_a_sunday() {
        // Arrange
        IsoWeek anyFirstWeek = new AnyIsoWeek().Generate();
        int     anyWeekCount = Any.Int32().Between(1, PlanningPeriod.MaxWeeks).Generate();

        // Act
        PlanningPeriod period = new PlanningPeriod(anyFirstWeek, anyWeekCount);

        // Assert
        Assert.Equal(anyFirstWeek.Monday, period.FirstDay);
        Assert.Equal(DayOfWeek.Sunday, period.LastDay.DayOfWeek);
        Assert.Equal((7 * anyWeekCount) - 1, period.LastDay.DayNumber - period.FirstDay.DayNumber);
    }

    [Fact]
    public void A_period_contains_its_first_and_last_day_and_nothing_beyond() {
        // Arrange
        PlanningPeriod period = new PlanningPeriod(new AnyIsoWeek().Generate(), Any.Int32().Between(1, PlanningPeriod.MaxWeeks).Generate());

        // Assert
        Assert.True(period.Contains(period.FirstDay));
        Assert.True(period.Contains(period.LastDay));
        Assert.False(period.Contains(period.FirstDay.AddDays(-1)));
        Assert.False(period.Contains(period.LastDay.AddDays(1)));
    }

    [Fact]
    public void The_weeks_of_a_period_follow_each_other_across_a_year_of_53_weeks() {
        // Act
        IReadOnlyList<IsoWeek> weeks = new PlanningPeriod(new IsoWeek(2026, 52), 3).Weeks();

        // Assert
        Assert.Equal(new[] { new IsoWeek(2026, 52), new IsoWeek(2026, 53), new IsoWeek(2027, 1) }, weeks);
    }

    [Fact]
    public void The_weeks_of_a_period_follow_each_other_across_a_year_of_52_weeks() {
        // Act
        IReadOnlyList<IsoWeek> weeks = new PlanningPeriod(new IsoWeek(2025, 52), 2).Weeks();

        // Assert
        Assert.Equal(new[] { new IsoWeek(2025, 52), new IsoWeek(2026, 1) }, weeks);
    }

    [Fact]
    public void A_period_lists_as_many_weeks_as_it_lasts_starting_with_its_first() {
        // Arrange
        IsoWeek anyFirstWeek = new AnyIsoWeek().Generate();
        int     anyWeekCount = Any.Int32().Between(1, PlanningPeriod.MaxWeeks).Generate();

        // Act
        IReadOnlyList<IsoWeek> weeks = new PlanningPeriod(anyFirstWeek, anyWeekCount).Weeks();

        // Assert
        Assert.Equal(anyWeekCount, weeks.Count);
        Assert.Equal(anyFirstWeek, weeks[0]);
        Assert.Equal(weeks.OrderBy(w => w), weeks);
    }

    [Theory]
    [InlineData(9999, 51, 1, true)]
    [InlineData(9999, 51, 2, false)]
    [InlineData(9999, 50, 2, true)]
    [InlineData(9999, 50, 3, false)]
    [InlineData(9999, 52, 1, false)]
    public void A_period_cannot_run_past_the_last_representable_date(int year, int week, int weekCount, bool accepted) {
        // Act
        Func<PlanningPeriod> build = () => new PlanningPeriod(new IsoWeek(year, week), weekCount);

        // Assert
        if (accepted) { Assert.Equal(weekCount, build().WeekCount); }
        else { Assert.Throws<ArgumentOutOfRangeException>(build); }
    }

    [Fact]
    public void Periods_are_equal_when_first_week_and_length_are_equal() {
        // Arrange
        IsoWeek anyFirstWeek = new AnyIsoWeek().Generate();
        PlanningPeriod period = new PlanningPeriod(anyFirstWeek, 4);

        // Assert
        Assert.Equal(period, new PlanningPeriod(new IsoWeek(anyFirstWeek.Year, anyFirstWeek.Week), 4));
        Assert.Equal(period.GetHashCode(), new PlanningPeriod(new IsoWeek(anyFirstWeek.Year, anyFirstWeek.Week), 4).GetHashCode());
        Assert.NotEqual(period, new PlanningPeriod(anyFirstWeek, 5));
    }

    [Fact]
    public void A_period_prints_its_first_week_and_its_length() {
        Assert.Equal("2026-W03 +4w", new PlanningPeriod(new IsoWeek(2026, 3), 4).ToString());
    }

}
