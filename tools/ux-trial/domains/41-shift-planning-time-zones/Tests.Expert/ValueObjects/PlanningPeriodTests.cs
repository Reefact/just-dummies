using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class PlanningPeriodTests {

    [Fact]
    public void A_period_starts_on_the_Monday_of_its_first_week_and_ends_on_the_Sunday_of_its_last() {
        // Arrange
        IsoWeek firstWeek = new IsoWeek(2027, 1);

        // Act
        PlanningPeriod period = new PlanningPeriod(firstWeek, 4);

        // Assert
        Assert.Equal(firstWeek, period.FirstWeek);
        Assert.Equal(4, period.WeekCount);
        Assert.Equal(Clock.Day("2027-01-04"), period.FirstDay);
        Assert.Equal(Clock.Day("2027-01-31"), period.LastDay);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(13)]
    public void A_period_lasts_from_1_to_13_weeks(int weekCount) {
        // Act
        PlanningPeriod period = Any.PlanningPeriod().WithWeekCount(weekCount).Generate();

        // Assert
        Assert.Equal(weekCount, period.WeekCount);
        Assert.Equal(weekCount, period.Weeks().Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(14)]
    public void A_period_of_fewer_than_one_or_more_than_thirteen_weeks_is_refused(int weekCount) {
        // Arrange
        IsoWeek anyFirstWeek = Any.IsoWeek().WithYear(year => year.LessThanOrEqualTo(2000)).Generate();

        // Act
        Action construct = () => new PlanningPeriod(anyFirstWeek, weekCount);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("weekCount", construct);
    }

    [Fact]
    public void A_missing_first_week_is_refused() {
        // Act
        Action construct = () => new PlanningPeriod(null!, 4);

        // Assert
        Assert.Throws<ArgumentNullException>("firstWeek", construct);
    }

    [Fact]
    public void The_last_week_a_period_can_start_on_in_year_9999_is_week_51() {
        // Arrange
        IsoWeek lastButOne = new IsoWeek(9999, 51);   // 9999-12-20 to 9999-12-26

        // Act
        PlanningPeriod period = new PlanningPeriod(lastButOne, 1);

        // Assert
        Assert.Equal(Clock.Day("9999-12-26"), period.LastDay);
    }

    [Theory]
    [InlineData(9999, 52, 1)]    // the week of 9999-12-27 ends on 10000-01-02
    [InlineData(9999, 51, 2)]    // 9999-12-20 plus two weeks
    [InlineData(9999, 40, 13)]   // 9999-10-04 plus thirteen weeks
    public void A_period_cannot_run_past_the_last_representable_date(int year, int week, int weekCount) {
        // Arrange
        IsoWeek firstWeek = new IsoWeek(year, week);

        // Act
        Action construct = () => new PlanningPeriod(firstWeek, weekCount);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("weekCount", construct);
    }

    [Fact]
    public void A_period_may_end_exactly_on_the_last_day_it_can_reach_in_year_9999() {
        // Arrange
        IsoWeek lastButTwelve = new IsoWeek(9999, 39);   // 9999-09-27 plus thirteen weeks ends on 9999-12-26

        // Act
        PlanningPeriod period = new PlanningPeriod(lastButTwelve, 13);

        // Assert
        Assert.Equal(Clock.Day("9999-12-26"), period.LastDay);
    }

    [Theory]
    [InlineData("2027-01-03", false)]   // the Sunday before the first Monday
    [InlineData("2027-01-04", true)]    // the first day
    [InlineData("2027-01-31", true)]    // the last day
    [InlineData("2027-02-01", false)]
    public void A_period_contains_every_date_from_its_first_to_its_last_day_inclusive(string date, bool expected) {
        // Arrange
        PlanningPeriod period = new PlanningPeriod(new IsoWeek(2027, 1), 4);

        // Act
        bool contains = period.Contains(Clock.Day(date));

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void The_weeks_of_a_period_follow_each_other_across_the_turn_of_the_year() {
        // Arrange
        PlanningPeriod period = new PlanningPeriod(new IsoWeek(2020, 52), 3);

        // Act
        IReadOnlyList<IsoWeek> weeks = period.Weeks();

        // Assert
        Assert.Equal(["2020-W52", "2020-W53", "2021-W01"], weeks.Select(week => week.ToString()));
        Assert.Equal(Clock.Day("2021-01-10"), period.LastDay);
    }

    [Fact]
    public void Periods_are_equal_when_first_week_and_length_are() {
        // Arrange
        PlanningPeriod period = new PlanningPeriod(new IsoWeek(2027, 1), 4);

        // Assert
        Assert.Equal(new PlanningPeriod(new IsoWeek(2027, 1), 4), period);
        Assert.Equal(new PlanningPeriod(new IsoWeek(2027, 1), 4).GetHashCode(), period.GetHashCode());
        Assert.NotEqual(new PlanningPeriod(new IsoWeek(2027, 1), 5), period);
        Assert.NotEqual(new PlanningPeriod(new IsoWeek(2027, 2), 4), period);
    }

    [Fact]
    public void A_period_reads_as_its_first_week_and_its_length() {
        // Assert
        Assert.Equal("2027-W01 +4w", new PlanningPeriod(new IsoWeek(2027, 1), 4).ToString());
    }

}
