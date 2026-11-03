using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class VisitScheduleTests {

    private static AnyVisitDefinition Visit(string code, int targetDay, int daysBefore, int daysAfter) {
        return Any.VisitDefinition().WithCode(code).WithTargetDay(targetDay).WithDaysBefore(daysBefore).WithDaysAfter(daysAfter);
    }

    [Fact]
    public void It_lists_its_visits_in_the_order_they_were_given() {
        // Act
        VisitSchedule schedule = Protocol.Schedule();

        // Assert
        Assert.Equal(["V1", "V2", "V3", "V4"], schedule.Visits.Select(v => v.Code));
    }

    [Fact]
    public void A_visit_is_found_by_its_code() {
        // Arrange
        VisitSchedule schedule = Protocol.Schedule();

        // Act / Assert
        Assert.Equal(8, schedule.Find("V2")!.TargetDay);
        Assert.Null(schedule.Find("V9"));
        Assert.Null(schedule.Find("v2"));
    }

    [Fact]
    public void A_visit_position_is_its_zero_based_index_or_minus_one() {
        // Arrange
        VisitSchedule schedule = Protocol.Schedule();

        // Act / Assert
        Assert.Equal(0, schedule.IndexOf("V1"));
        Assert.Equal(2, schedule.IndexOf("V3"));
        Assert.Equal(-1, schedule.IndexOf("V9"));
    }

    [Fact]
    public void A_missing_code_is_refused_by_the_lookups() {
        // Arrange
        VisitSchedule schedule = Any.VisitSchedule().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => schedule.Find(null!));
        Assert.Throws<ArgumentNullException>(() => schedule.IndexOf(null!));
    }

    [Theory]
    [InlineData(1, "V1")]
    [InlineData(6, "V2")]
    [InlineData(10, "V2")]
    [InlineData(13, "V3")]
    [InlineData(17, "V3")]
    [InlineData(25, "V4")]
    [InlineData(31, "V4")]
    [InlineData(-3, null)]
    [InlineData(0, null)]
    [InlineData(2, null)]
    [InlineData(11, null)]
    [InlineData(12, null)]
    [InlineData(32, null)]
    public void A_study_day_belongs_to_the_visit_whose_window_contains_it_if_any(int studyDay, string? expectedCode) {
        // Arrange
        VisitSchedule schedule = Protocol.Schedule();

        // Act
        VisitDefinition? visit = schedule.VisitOn(studyDay);

        // Assert
        Assert.Equal(expectedCode, visit?.Code);
    }

    [Fact]
    public void A_schedule_needs_at_least_one_visit() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitSchedule([]));

        // Assert
        Assert.Equal("visits", refusal.ParamName);
    }

    [Fact]
    public void A_missing_list_of_visits_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new VisitSchedule(null!));
    }

    [Fact]
    public void A_null_visit_is_refused() {
        // Arrange
        VisitDefinition anyVisit = Any.VisitDefinition().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule([anyVisit, null!]));
    }

    [Fact]
    public void Two_visits_cannot_share_a_code() {
        // Arrange
        VisitDefinition first  = Visit("V1", 5, 0, 0).Generate();
        VisitDefinition second = Visit("V1", 50, 0, 0).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitSchedule([first, second]));

        // Assert
        Assert.Equal("visits", refusal.ParamName);
    }

    [Fact]
    public void Visits_must_be_listed_by_increasing_target_day() {
        // Arrange
        VisitDefinition first  = Any.VisitDefinition().WithTargetDay(50).WithDaysBefore(0).WithDaysAfter(0).Generate();
        VisitDefinition second = Any.VisitDefinition().WithCodeOtherThan(first.Code).WithTargetDay(10).WithDaysBefore(0).WithDaysAfter(0).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule([first, second]));
    }

    [Fact]
    public void Two_visits_cannot_target_the_same_day() {
        // Arrange
        VisitDefinition first  = Any.VisitDefinition().WithTargetDay(20).WithDaysBefore(0).WithDaysAfter(0).Generate();
        VisitDefinition second = Any.VisitDefinition().WithCodeOtherThan(first.Code).WithTargetDay(20).WithDaysBefore(0).WithDaysAfter(0).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule([first, second]));
    }

    [Fact]
    public void A_window_cannot_open_on_the_day_the_previous_one_closes() {
        // Arrange: the first window is days 5 to 15, the second opens on day 15
        VisitDefinition first  = Any.VisitDefinition().WithTargetDay(10).WithDaysBefore(5).WithDaysAfter(5).Generate();
        VisitDefinition second = Any.VisitDefinition().WithCodeOtherThan(first.Code).WithTargetDay(20).WithDaysBefore(5).WithDaysAfter(0).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitSchedule([first, second]));

        // Assert
        Assert.Equal("visits", refusal.ParamName);
    }

    [Fact]
    public void A_window_may_open_the_day_after_the_previous_one_closes() {
        // Arrange: the first window is days 5 to 15, the second opens on day 16
        VisitDefinition first  = Any.VisitDefinition().WithTargetDay(10).WithDaysBefore(5).WithDaysAfter(5).Generate();
        VisitDefinition second = Any.VisitDefinition().WithCodeOtherThan(first.Code).WithTargetDay(20).WithDaysBefore(4).WithDaysAfter(0).Generate();

        // Act
        VisitSchedule schedule = new VisitSchedule([first, second]);

        // Assert
        Assert.Equal(2, schedule.Visits.Count);
    }

    [Fact]
    public void A_schedule_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<VisitDefinition> visits   = [Visit("V1", 1, 0, 0).Generate()];
        VisitSchedule         schedule = new VisitSchedule(visits);

        // Act
        visits.Add(Visit("V2", 8, 0, 0).Generate());

        // Assert
        Assert.Single(schedule.Visits);
    }

}
