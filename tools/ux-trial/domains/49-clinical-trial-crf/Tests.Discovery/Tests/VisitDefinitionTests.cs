using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class VisitDefinitionTests {

    private static string[] NoAnalytes => Array.Empty<string>();

    [Fact]
    public void The_window_spans_from_target_minus_days_before_to_target_plus_days_after() {
        // Act
        VisitDefinition visit = new VisitDefinition("D28", 28, 3, 5, NoAnalytes);

        // Assert
        Assert.Equal(25, visit.EarliestDay);
        Assert.Equal(33, visit.LatestDay);
    }

    [Theory]
    [InlineData(24, false)]
    [InlineData(25, true)]
    [InlineData(28, true)]
    [InlineData(33, true)]
    [InlineData(34, false)]
    public void A_study_day_is_in_the_window_between_its_two_ends_included(int studyDay, bool expected) {
        // Arrange
        VisitDefinition visit = new VisitDefinition("D28", 28, 3, 5, NoAnalytes);

        // Assert
        Assert.Equal(expected, visit.Contains(studyDay));
    }

    [Fact]
    public void A_window_with_no_tolerance_holds_only_its_target_day() {
        // Arrange
        VisitDefinition visit = new VisitDefinition("D28", 28, 0, 0, NoAnalytes);

        // Assert
        Assert.True(visit.Contains(28));
        Assert.False(visit.Contains(27));
        Assert.False(visit.Contains(29));
    }

    [Theory]
    [InlineData("D")]
    [InlineData("D28")]
    [InlineData("ABCDEFGH")]
    [InlineData("V1A2B3C4")]
    public void A_code_of_an_upper_case_letter_then_up_to_7_letters_or_digits_is_accepted(string code) {
        Assert.Equal(code, new VisitDefinition(code, 10, 0, 0, NoAnalytes).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("d28")]
    [InlineData("28D")]
    [InlineData("ABCDEFGHI")]
    [InlineData("D-28")]
    [InlineData("D 28")]
    [InlineData("D28 ")]
    public void A_code_that_is_not_a_letter_followed_by_up_to_7_letters_or_digits_is_refused(string code) {
        Assert.Throws<FormatException>(() => new VisitDefinition(code, 10, 0, 0, NoAnalytes));
    }

    [Fact]
    public void A_visit_has_a_code() {
        Assert.Throws<ArgumentNullException>(() => new VisitDefinition(null!, 10, 0, 0, NoAnalytes));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1095)]
    public void The_first_and_last_target_days_are_accepted(int targetDay) {
        Assert.Equal(targetDay, new VisitDefinition("D1", targetDay, 0, 0, NoAnalytes).TargetDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1096)]
    public void A_target_day_outside_1_to_1095_is_refused(int targetDay) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisitDefinition("D1", targetDay, 0, 0, NoAnalytes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void Tolerances_of_0_and_30_days_are_accepted(int tolerance) {
        VisitDefinition visit = new VisitDefinition("D100", 100, tolerance, tolerance, NoAnalytes);

        Assert.Equal(tolerance, visit.DaysBefore);
        Assert.Equal(tolerance, visit.DaysAfter);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void A_tolerance_before_outside_0_to_30_is_refused(int daysBefore) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisitDefinition("D100", 100, daysBefore, 0, NoAnalytes));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void A_tolerance_after_outside_0_to_30_is_refused(int daysAfter) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisitDefinition("D100", 100, 0, daysAfter, NoAnalytes));
    }

    [Fact]
    public void A_window_may_open_on_day_1() {
        Assert.Equal(1, new VisitDefinition("D5", 5, 4, 0, NoAnalytes).EarliestDay);
    }

    [Fact]
    public void A_window_cannot_open_before_randomisation() {
        Assert.Throws<ArgumentException>(() => new VisitDefinition("D5", 5, 5, 0, NoAnalytes));
    }

    [Fact]
    public void Required_analytes_keep_their_order() {
        // Act
        VisitDefinition visit = new VisitDefinition("D28", 28, 3, 3, new[] { "HGB", "ALT", "CREAT" });

        // Assert
        Assert.Equal(new[] { "HGB", "ALT", "CREAT" }, visit.RequiredAnalytes);
    }

    [Theory]
    [InlineData("alt")]
    [InlineData("A")]
    [InlineData("1AB")]
    [InlineData("ALT 1")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("")]
    public void A_required_analyte_must_be_an_analyte_code(string analyte) {
        Assert.Throws<ArgumentException>(() => new VisitDefinition("D28", 28, 3, 3, new[] { analyte }));
    }

    [Fact]
    public void A_required_analyte_cannot_be_absent() {
        Assert.Throws<ArgumentException>(() => new VisitDefinition("D28", 28, 3, 3, new string[] { null! }));
    }

    [Fact]
    public void An_analyte_is_required_once_per_visit() {
        Assert.Throws<ArgumentException>(() => new VisitDefinition("D28", 28, 3, 3, new[] { "ALT", "HGB", "ALT" }));
    }

    [Fact]
    public void The_list_of_required_analytes_is_not_absent() {
        Assert.Throws<ArgumentNullException>(() => new VisitDefinition("D28", 28, 3, 3, null!));
    }

    [Fact]
    public void A_symmetric_window_prints_with_plus_or_minus() {
        Assert.Equal("D28 (day 28 ± 3)", new VisitDefinition("D28", 28, 3, 3, NoAnalytes).ToString());
    }

    [Fact]
    public void An_asymmetric_window_prints_both_tolerances() {
        Assert.Equal("D28 (day 28 −7/+2)", new VisitDefinition("D28", 28, 7, 2, NoAnalytes).ToString());
    }

    [Fact]
    public void Two_definitions_with_the_same_fields_are_equal() {
        // Arrange
        VisitDefinition anyVisit = Any.VisitDefinition().Generate();

        // Act
        VisitDefinition same = new VisitDefinition(anyVisit.Code, anyVisit.TargetDay, anyVisit.DaysBefore, anyVisit.DaysAfter, anyVisit.RequiredAnalytes);

        // Assert
        Assert.Equal(anyVisit, same);
        Assert.Equal(anyVisit.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void The_order_of_the_required_analytes_makes_definitions_differ() {
        Assert.NotEqual(new VisitDefinition("D28", 28, 3, 3, new[] { "ALT", "HGB" }),
                        new VisitDefinition("D28", 28, 3, 3, new[] { "HGB", "ALT" }));
    }

}
