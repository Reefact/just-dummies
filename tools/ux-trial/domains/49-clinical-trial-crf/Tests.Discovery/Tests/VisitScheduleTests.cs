using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class VisitScheduleTests {

    private static VisitDefinition Window(string code, int targetDay, int before, int after) {
        return new VisitDefinition(code, targetDay, before, after, Array.Empty<string>());
    }

    [Fact]
    public void A_schedule_lists_its_visits_in_order() {
        // Arrange
        VisitDefinition d8  = Window("D8", 8, 2, 2);
        VisitDefinition d29 = Window("D29", 29, 3, 3);

        // Act
        VisitSchedule schedule = new VisitSchedule(new[] { d8, d29 });

        // Assert
        Assert.Equal(new[] { d8, d29 }, schedule.Visits);
    }

    [Fact]
    public void A_schedule_has_at_least_one_visit() {
        Assert.Throws<ArgumentException>(() => new VisitSchedule(Array.Empty<VisitDefinition>()));
    }

    [Fact]
    public void A_schedule_is_not_absent() {
        Assert.Throws<ArgumentNullException>(() => new VisitSchedule(null!));
    }

    [Fact]
    public void A_schedule_cannot_contain_a_null_visit() {
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new VisitDefinition[] { Window("D8", 8, 2, 2), null! }));
    }

    [Fact]
    public void Visit_codes_are_unique() {
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Window("D8", 8, 1, 1), Window("D8", 29, 1, 1) }));
    }

    [Fact]
    public void Visits_are_listed_by_increasing_target_day() {
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Window("D29", 29, 1, 1), Window("D8", 8, 1, 1) }));
    }

    [Fact]
    public void Two_visits_cannot_share_a_target_day() {
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Window("A", 10, 0, 0), Window("B", 10, 0, 0) }));
    }

    [Fact]
    public void Overlapping_windows_are_refused() {
        // 7..13 and 11..17
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Window("A", 10, 3, 3), Window("B", 14, 3, 3) }));
    }

    [Fact]
    public void A_window_that_opens_on_the_day_the_previous_one_closes_is_refused() {
        // 7..13 and 13..19
        Assert.Throws<ArgumentException>(() => new VisitSchedule(new[] { Window("A", 10, 3, 3), Window("B", 16, 3, 3) }));
    }

    [Fact]
    public void A_window_that_opens_the_day_after_the_previous_one_closes_is_accepted() {
        // 7..13 and 14..20
        VisitSchedule schedule = new VisitSchedule(new[] { Window("A", 10, 3, 3), Window("B", 17, 3, 3) });

        Assert.Equal(2, schedule.Visits.Count);
    }

    [Fact]
    public void A_visit_is_found_by_its_code() {
        // Arrange
        VisitDefinition d29      = Window("D29", 29, 3, 3);
        VisitSchedule   schedule = new VisitSchedule(new[] { Window("D8", 8, 2, 2), d29 });

        // Assert
        Assert.Same(d29, schedule.Find("D29"));
    }

    [Fact]
    public void An_unknown_code_finds_nothing() {
        // Arrange
        VisitSchedule anySchedule = Any.VisitSchedule().Generate();

        // Assert: no code has 9 characters
        Assert.Null(anySchedule.Find("NOTAVISIT"));
        Assert.Equal(-1, anySchedule.IndexOf("NOTAVISIT"));
    }

    [Fact]
    public void A_visit_position_is_its_zero_based_index() {
        // Arrange
        VisitSchedule schedule = new VisitSchedule(new[] { Window("D8", 8, 2, 2), Window("D29", 29, 3, 3), Window("D85", 85, 7, 7) });

        // Assert
        Assert.Equal(0, schedule.IndexOf("D8"));
        Assert.Equal(2, schedule.IndexOf("D85"));
    }

    [Fact]
    public void Looking_up_a_code_requires_one() {
        // Arrange
        VisitSchedule anySchedule = Any.VisitSchedule().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySchedule.Find(null!));
        Assert.Throws<ArgumentNullException>(() => anySchedule.IndexOf(null!));
    }

    [Theory]
    [InlineData(5, null)]
    [InlineData(6, "D8")]
    [InlineData(10, "D8")]
    [InlineData(11, null)]
    [InlineData(25, null)]
    [InlineData(26, "D29")]
    [InlineData(32, "D29")]
    [InlineData(33, null)]
    public void A_study_day_belongs_to_the_visit_whose_window_contains_it(int studyDay, string? expectedCode) {
        // Arrange
        VisitSchedule schedule = new VisitSchedule(new[] { Window("D8", 8, 2, 2), Window("D29", 29, 3, 3) });

        // Assert
        Assert.Equal(expectedCode, schedule.VisitOn(studyDay)?.Code);
    }

}
