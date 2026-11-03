using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain.Tests.Regular.Generators;
using ClinicalTrial.Domain.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class VisitScheduleTests {

    private static VisitDefinition Visit(string code, int targetDay, int tolerance) {
        return Any.VisitDefinition().WithCode(code).WithTargetDay(targetDay).WithDaysBefore(tolerance).WithDaysAfter(tolerance).Generate();
    }

    [Fact]
    public void A_schedule_lists_its_visits_in_order() {
        // Act
        VisitSchedule schedule = Trial.Protocol();

        // Assert
        Assert.Equal(new[] { "V1", "V2", "V3" }, schedule.Visits.Select(v => v.Code));
    }

    [Fact]
    public void A_schedule_needs_a_visit() {
        Assert.Throws<ArgumentException>(() => new VisitSchedule(Array.Empty<VisitDefinition>()));
    }

    [Fact]
    public void A_missing_list_of_visits_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new VisitSchedule(null!));
    }

    [Fact]
    public void A_schedule_of_one_visit_is_accepted() {
        // Arrange
        VisitDefinition anyVisit = Any.VisitDefinition().Generate();

        // Act
        VisitSchedule schedule = new VisitSchedule(new[] { anyVisit });

        // Assert
        Assert.Single(schedule.Visits);
    }

    [Fact]
    public void A_schedule_cannot_contain_a_null_visit() {
        // Arrange
        VisitDefinition anyVisit = Any.VisitDefinition().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { anyVisit, null! }));
    }

    [Fact]
    public void Visit_codes_are_unique() {
        // Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Visit("V1", 8, 1), Visit("V1", 30, 1) }));
    }

    [Fact]
    public void Visits_are_listed_in_chronological_order() {
        // Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Visit("V2", 30, 1), Visit("V1", 8, 1) }));
    }

    [Fact]
    public void Two_visits_cannot_share_a_target_day() {
        // Assert
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Visit("V1", 8, 0), Visit("V2", 8, 0) }));
    }

    [Fact]
    public void A_window_cannot_overlap_the_previous_one() {
        // V1 closes on day 11, V2 opens on day 10.
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Visit("V1", 8, 3), Visit("V2", 13, 3) }));
    }

    [Fact]
    public void A_window_cannot_touch_the_previous_one() {
        // V1 closes on day 11, V2 opens on day 11.
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Visit("V1", 8, 3), Visit("V2", 14, 3) }));
    }

    [Fact]
    public void A_window_may_open_the_day_after_the_previous_one_closes() {
        // V1 closes on day 11, V2 opens on day 12.
        VisitSchedule schedule = new VisitSchedule(new[] { Visit("V1", 8, 3), Visit("V2", 15, 3) });

        Assert.Equal(2, schedule.Visits.Count);
    }

    [Fact]
    public void A_window_containing_the_previous_target_day_is_refused_even_if_the_targets_are_in_order() {
        // V2's wide window reaches back over V1's whole window.
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Visit("V1", 20, 1), Visit("V2", 40, 30) }));
    }

    [Fact]
    public void Changing_the_list_given_to_the_constructor_does_not_change_the_schedule() {
        // Arrange
        List<VisitDefinition> visits = new List<VisitDefinition> { Visit("V1", 8, 1) };

        // Act
        VisitSchedule schedule = new VisitSchedule(visits);
        visits.Add(Visit("V2", 30, 1));

        // Assert
        Assert.Single(schedule.Visits);
    }

    [Fact]
    public void A_visit_is_found_by_its_code() {
        // Arrange
        VisitSchedule schedule = Trial.Protocol();

        // Assert
        Assert.Equal(15, schedule.Find("V2")!.TargetDay);
        Assert.Null(schedule.Find("V4"));
    }

    [Fact]
    public void Codes_are_compared_exactly() {
        // Arrange
        VisitSchedule schedule = Trial.Protocol();

        // Assert
        Assert.Null(schedule.Find("v1"));
        Assert.Equal(-1, schedule.IndexOf("v1"));
    }

    [Fact]
    public void A_visit_is_positioned_from_zero_or_minus_one_when_unknown() {
        // Arrange
        VisitSchedule schedule = Trial.Protocol();

        // Assert
        Assert.Equal(0, schedule.IndexOf("V1"));
        Assert.Equal(2, schedule.IndexOf("V3"));
        Assert.Equal(-1, schedule.IndexOf("V9"));
    }

    [Fact]
    public void Looking_up_a_missing_code_is_refused() {
        // Arrange
        VisitSchedule schedule = Trial.Protocol();

        // Assert
        Assert.Throws<ArgumentNullException>(() => schedule.Find(null!));
        Assert.Throws<ArgumentNullException>(() => schedule.IndexOf(null!));
    }

    [Theory]
    [InlineData(5, null)]
    [InlineData(6, "V1")]
    [InlineData(10, "V1")]
    [InlineData(11, null)]
    [InlineData(12, "V2")]
    [InlineData(18, "V2")]
    [InlineData(19, null)]
    [InlineData(26, "V3")]
    [InlineData(32, "V3")]
    [InlineData(33, null)]
    [InlineData(-4, null)]
    [InlineData(0, null)]
    public void A_study_day_belongs_to_the_visit_whose_window_contains_it(int studyDay, string? expectedCode) {
        // Arrange
        VisitSchedule schedule = Trial.Protocol();

        // Act
        VisitDefinition? visit = schedule.VisitOn(studyDay);

        // Assert
        Assert.Equal(expectedCode, visit?.Code);
    }

}
