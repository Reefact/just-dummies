using Education.Domain;

using JustDummies;

using Xunit;

using static Education.Tests.Expert.Fixtures;

namespace Education.Tests.Expert;

public sealed class EnrolmentResultsTests {

    // ---- Average -----------------------------------------------------------------------------------------

    [Fact]
    public void The_average_of_an_open_enrolment_is_not_available() {
        // Arrange
        Enrolment enrolment = GradedEnrolment((6, 12m));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => enrolment.Average());

        // Assert
        Assert.Contains("closed", refusal.Message);
    }

    [Fact]
    public void The_average_weighs_each_grade_by_the_credits_of_its_course() {
        // Arrange (6 credits at 12 and 3 credits at 15: (72 + 45) / 9)
        Enrolment enrolment = ClosedEnrolment((6, 12m), (3, 15m));

        // Act
        decimal average = enrolment.Average();

        // Assert
        Assert.Equal(13m, average);
    }

    [Fact]
    public void The_average_is_rounded_to_two_decimals() {
        // Arrange (10, 11 and 11 over one credit each: 32 / 3 = 10.666...)
        Enrolment enrolment = ClosedEnrolment((1, 10m), (1, 11m), (1, 11m));

        // Act
        decimal average = enrolment.Average();

        // Assert
        Assert.Equal(10.67m, average);
    }

    [Fact]
    public void The_average_rounds_a_tie_away_from_zero() {
        // Arrange (2 credits at 7.5 and 6 credits at 11 weigh 81 over 8 credits: 10.125, half way between 10.12 and 10.13)
        Enrolment enrolment = ClosedEnrolment((2, 7.5m), (6, 11m));

        // Act
        decimal average = enrolment.Average();

        // Assert
        Assert.Equal(10.13m, average);
    }

    [Fact]
    public void A_single_course_average_is_its_grade() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment((5, 17.5m));

        // Assert
        Assert.Equal(17.5m, enrolment.Average());
    }

    // ---- IsPassed ----------------------------------------------------------------------------------------

    [Fact]
    public void An_open_enrolment_is_neither_passed_nor_failed() {
        // Arrange
        Enrolment enrolment = GradedEnrolment((6, 12m));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => enrolment.IsPassed());
    }

    [Theory]
    [InlineData(10, true)]     // the pass mark itself
    [InlineData(9.5, false)]
    [InlineData(15, true)]
    public void A_single_course_enrolment_is_passed_from_a_grade_of_10(double grade, bool expected) {
        // Arrange
        Enrolment enrolment = ClosedEnrolment((6, (decimal)grade));

        // Act & Assert
        Assert.Equal(expected, enrolment.IsPassed());
    }

    [Fact]
    public void A_good_course_compensates_a_weak_one() {
        // Arrange (average 11, one grade under 10 but not eliminatory)
        Enrolment enrolment = ClosedEnrolment((6, 14m), (6, 8m));

        // Assert
        Assert.True(enrolment.IsPassed());
    }

    [Fact]
    public void A_grade_of_exactly_5_is_not_eliminatory() {
        // Arrange (average (120 + 15) / 12 = 11.25)
        Enrolment enrolment = ClosedEnrolment((10, 12m), (2, 5m));

        // Assert
        Assert.True(enrolment.IsPassed());
    }

    [Fact]
    public void An_eliminatory_grade_fails_the_enrolment_whatever_the_average() {
        // Arrange (average (200 + 9) / 12 = 17.42)
        Enrolment enrolment = ClosedEnrolment((10, 20m), (2, 4.5m));

        // Act
        decimal average = enrolment.Average();

        // Assert
        Assert.True(average >= Enrolment.PassMark);
        Assert.False(enrolment.IsPassed());
    }

    // ---- AcquiredCredits ---------------------------------------------------------------------------------

    [Fact]
    public void An_open_enrolment_has_no_acquired_credits_yet() {
        // Arrange
        Enrolment enrolment = GradedEnrolment((6, 12m));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => enrolment.AcquiredCredits());
    }

    [Fact]
    public void A_passed_enrolment_acquires_all_its_credits_even_the_ones_of_a_course_under_10() {
        // Arrange (compensation: 6 credits at 14 and 6 credits at 8)
        Enrolment enrolment = ClosedEnrolment((6, 14m), (6, 8m));

        // Assert
        Assert.Equal(12, enrolment.AcquiredCredits());
    }

    [Fact]
    public void A_failed_enrolment_acquires_the_credits_of_the_courses_graded_10_or_more() {
        // Arrange (average (60 + 50 + 24 + 15) / 20 = 7.45)
        Enrolment enrolment = ClosedEnrolment((5, 12m), (5, 10m), (4, 6m), (5, 3m));

        // Act
        int acquired = enrolment.AcquiredCredits();

        // Assert
        Assert.Equal(10, acquired);
    }

    [Fact]
    public void A_course_graded_9_and_a_half_acquires_nothing_in_a_failed_enrolment() {
        // Arrange (average 9.5)
        Enrolment enrolment = ClosedEnrolment((5, 9.5m), (5, 9.5m));

        // Assert
        Assert.Equal(0, enrolment.AcquiredCredits());
    }

    [Fact]
    public void An_eliminatory_grade_leaves_only_the_credits_of_the_courses_graded_10_or_more() {
        // Arrange (average 17.42 but a 4.5)
        Enrolment enrolment = ClosedEnrolment((10, 20m), (2, 4.5m));

        // Assert
        Assert.Equal(10, enrolment.AcquiredCredits());
    }

    // ---- Honours -----------------------------------------------------------------------------------------

    public static TheoryData<decimal, Honours> HonoursByAverage => new TheoryData<decimal, Honours> {
        { 20m, Honours.VeryGood },
        { 16m, Honours.VeryGood },
        { 15.5m, Honours.Good },
        { 14m, Honours.Good },
        { 13.5m, Honours.Fair },
        { 12m, Honours.Fair },
        { 11.5m, Honours.Pass },
        { 10m, Honours.Pass },
        { 9.5m, Honours.None },
        { 0m, Honours.None }
    };

    [Theory]
    [MemberData(nameof(HonoursByAverage))]
    public void Honours_follow_the_average_at_16_14_12_and_10(decimal grade, Honours expected) {
        // Arrange (one course: the average is its grade, whatever its credits)
        int       anyCredits = Any.Course().Generate().Credits;
        Enrolment enrolment  = ClosedEnrolment((anyCredits, grade));

        // Act
        Honours honours = enrolment.Honours();

        // Assert
        Assert.Equal(expected, honours);
    }

    [Fact]
    public void An_eliminatory_grade_forfeits_the_honours_an_average_would_earn() {
        // Arrange (average 17.42, which is very good, but a 4.5)
        Enrolment enrolment = ClosedEnrolment((10, 20m), (2, 4.5m));

        // Assert
        Assert.Equal(Honours.None, enrolment.Honours());
    }

    [Fact]
    public void The_honours_of_an_open_enrolment_are_not_available() {
        // Arrange
        Enrolment enrolment = GradedEnrolment((6, 12m));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => enrolment.Honours());
    }

}
