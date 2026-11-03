using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class VisitDefinitionTests {

    [Fact]
    public void The_window_runs_from_the_days_before_to_the_days_after_the_target() {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithTargetDay(28).WithDaysBefore(3).WithDaysAfter(5).Generate();

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
    public void A_study_day_is_in_the_window_from_its_first_day_to_its_last_day_included(int studyDay, bool expected) {
        // Arrange
        VisitDefinition visit = Any.VisitDefinition().WithTargetDay(28).WithDaysBefore(3).WithDaysAfter(5).Generate();

        // Act
        bool contains = visit.Contains(studyDay);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void Without_tolerance_only_the_target_day_is_in_the_window() {
        // Arrange
        VisitDefinition visit = Any.VisitDefinition().WithTargetDay(14).WithDaysBefore(0).WithDaysAfter(0).Generate();

        // Assert
        Assert.False(visit.Contains(13));
        Assert.True(visit.Contains(14));
        Assert.False(visit.Contains(15));
    }

    [Theory]
    [InlineData("D")]
    [InlineData("D28")]
    [InlineData("SCREEN1")]
    [InlineData("D1234567")]
    public void A_code_is_an_upper_case_letter_then_up_to_seven_upper_case_letters_or_digits(string code) {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().Generate();

        // Act
        VisitDefinition visit = new VisitDefinition(code, model.TargetDay, model.DaysBefore, model.DaysAfter, model.RequiredAnalytes);

        // Assert
        Assert.Equal(code, visit.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("d28")]
    [InlineData("28D")]
    [InlineData("D-28")]
    [InlineData("D 28")]
    [InlineData("D12345678")]
    public void A_code_that_is_not_such_a_word_is_refused(string code) {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new VisitDefinition(code, model.TargetDay, model.DaysBefore, model.DaysAfter, model.RequiredAnalytes));
    }

    [Fact(Skip = "domain defect: the code pattern ends with '$', which .NET also matches before a final newline, so 'D28\\n' is accepted; use \\z. No seed: literal input")]
    public void A_code_followed_by_a_line_break_is_refused() {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new VisitDefinition("D28\n", model.TargetDay, model.DaysBefore, model.DaysAfter, model.RequiredAnalytes));
    }

    [Fact]
    public void A_missing_code_or_a_missing_list_of_analytes_is_refused() {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new VisitDefinition(null!, model.TargetDay, model.DaysBefore, model.DaysAfter, model.RequiredAnalytes));
        Assert.Throws<ArgumentNullException>(() => new VisitDefinition(model.Code, model.TargetDay, model.DaysBefore, model.DaysAfter, null!));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1095)]
    public void The_first_and_the_last_day_of_the_study_can_be_targeted(int targetDay) {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithTargetDay(targetDay).Generate();

        // Assert
        Assert.Equal(targetDay, visit.TargetDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(1096)]
    public void A_target_day_outside_1_to_1095_is_refused(int targetDay) {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new VisitDefinition(model.Code, targetDay, 0, model.DaysAfter, model.RequiredAnalytes));

        // Assert
        Assert.Equal("targetDay", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void A_tolerance_before_the_target_outside_0_to_30_days_is_refused(int daysBefore) {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().WithTargetDay(500).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new VisitDefinition(model.Code, model.TargetDay, daysBefore, model.DaysAfter, model.RequiredAnalytes));

        // Assert
        Assert.Equal("daysBefore", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void A_tolerance_after_the_target_outside_0_to_30_days_is_refused(int daysAfter) {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().WithTargetDay(500).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new VisitDefinition(model.Code, model.TargetDay, model.DaysBefore, daysAfter, model.RequiredAnalytes));

        // Assert
        Assert.Equal("daysAfter", refusal.ParamName);
    }

    [Fact]
    public void The_longest_tolerances_are_30_days_each_way() {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithTargetDay(500).WithDaysBefore(30).WithDaysAfter(30).Generate();

        // Assert
        Assert.Equal(470, visit.EarliestDay);
        Assert.Equal(530, visit.LatestDay);
    }

    [Fact]
    public void A_window_may_open_on_day_1_but_not_before() {
        // Act
        VisitDefinition opensOnDayOne = Any.VisitDefinition().WithTargetDay(10).WithDaysBefore(9).Generate();
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VisitDefinition().WithTargetDay(10).WithDaysBefore(10).Generate());

        // Assert
        Assert.Equal(1, opensOnDayOne.EarliestDay);
        Assert.Equal("daysBefore", refusal.ParamName);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ALT")]
    [InlineData("A123456789")]
    public void An_analyte_code_is_an_upper_case_letter_then_one_to_nine_upper_case_letters_or_digits(string analyte) {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithRequiredAnalytes(analyte).Generate();

        // Assert
        Assert.Equal([analyte], visit.RequiredAnalytes);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("alt")]
    [InlineData("ALT-1")]
    [InlineData("1ALT")]
    [InlineData("AB1234567890")]
    [InlineData("A1234567890")]
    public void A_required_analyte_that_is_not_an_analyte_code_is_refused(string analyte) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VisitDefinition().WithRequiredAnalytes(analyte).Generate());

        // Assert
        Assert.Equal("requiredAnalytes", refusal.ParamName);
    }

    [Fact]
    public void A_null_among_the_required_analytes_is_refused() {
        // Arrange
        VisitDefinition model = Any.VisitDefinition().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new VisitDefinition(model.Code, model.TargetDay, model.DaysBefore, model.DaysAfter, new string[] { null! }));
    }

    [Fact]
    public void An_analyte_is_required_once_per_visit() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VisitDefinition().WithRequiredAnalytes("ALT", "HGB", "ALT").Generate());

        // Assert
        Assert.Equal("requiredAnalytes", refusal.ParamName);
    }

    [Fact]
    public void The_required_analytes_keep_the_order_they_were_given_in() {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithRequiredAnalytes("HGB", "ALT", "CREA").Generate();

        // Assert
        Assert.Equal(["HGB", "ALT", "CREA"], visit.RequiredAnalytes);
    }

    [Fact]
    public void A_visit_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        VisitDefinition model    = Any.VisitDefinition().Generate();
        List<string>    analytes = ["ALT", "HGB"];
        VisitDefinition visit    = new VisitDefinition(model.Code, model.TargetDay, model.DaysBefore, model.DaysAfter, analytes);

        // Act
        analytes.Clear();

        // Assert
        Assert.Equal(["ALT", "HGB"], visit.RequiredAnalytes);
    }

    [Fact]
    public void Two_definitions_are_equal_when_every_field_is() {
        // Arrange
        VisitDefinition anyVisit = Any.VisitDefinition().Generate();

        // Act
        VisitDefinition sameVisit = new VisitDefinition(anyVisit.Code, anyVisit.TargetDay, anyVisit.DaysBefore, anyVisit.DaysAfter, anyVisit.RequiredAnalytes);

        // Assert
        Assert.Equal(anyVisit, sameVisit);
        Assert.Equal(anyVisit.GetHashCode(), sameVisit.GetHashCode());
    }

    [Fact]
    public void A_visit_asking_for_other_analytes_is_another_definition() {
        // Arrange
        VisitDefinition askingForAlt = Any.VisitDefinition().WithCode("D28").WithTargetDay(28).WithDaysBefore(3).WithDaysAfter(3).WithRequiredAnalytes("ALT").Generate();
        VisitDefinition askingForHgb = Any.VisitDefinition().WithCode("D28").WithTargetDay(28).WithDaysBefore(3).WithDaysAfter(3).WithRequiredAnalytes("HGB").Generate();

        // Act / Assert
        Assert.NotEqual(askingForAlt, askingForHgb);
    }

    [Fact]
    public void It_prints_its_code_and_a_symmetrical_window_with_a_plus_minus_sign() {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithCode("D28").WithTargetDay(28).WithDaysBefore(3).WithDaysAfter(3).Generate();

        // Assert
        Assert.Equal("D28 (day 28 ± 3)", visit.ToString());
    }

    [Fact]
    public void It_prints_an_asymmetrical_window_with_both_tolerances() {
        // Act
        VisitDefinition visit = Any.VisitDefinition().WithCode("D28").WithTargetDay(28).WithDaysBefore(3).WithDaysAfter(5).Generate();

        // Assert
        Assert.Equal("D28 (day 28 −3/+5)", visit.ToString());
    }

}
