using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Domain.Tests.Discovery;

public sealed class GradeTests {

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(9.5)]
    [InlineData(10)]
    [InlineData(19.5)]
    [InlineData(20)]
    public void A_half_point_between_zero_and_twenty_is_accepted(double value) {
        Grade grade = new Grade((decimal)value);

        Assert.Equal((decimal)value, grade.Value);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(-1)]
    [InlineData(20.5)]
    [InlineData(21)]
    [InlineData(100)]
    public void A_value_outside_zero_to_twenty_is_refused_as_out_of_range(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grade((decimal)value));
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.1)]
    [InlineData(10.3)]
    [InlineData(19.75)]
    [InlineData(12.01)]
    public void A_value_that_is_not_a_multiple_of_a_half_is_refused(double value) {
        Assert.Throws<ArgumentException>(() => new Grade((decimal)value));
    }

    [Fact]
    public void The_range_is_checked_before_the_step() {
        // 20.25 is both out of range and off the half-point grid: the range wins.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grade(20.25m));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4.5, true)]
    [InlineData(5, false)]
    [InlineData(5.5, false)]
    [InlineData(20, false)]
    public void A_grade_below_five_is_eliminatory(double value, bool expected) {
        Grade grade = new Grade((decimal)value);

        Assert.Equal(expected, grade.IsEliminatory);
    }

    [Fact]
    public void Two_grades_with_the_same_value_are_equal_whatever_their_scale() {
        Grade first  = new Grade(12m);
        Grade second = new Grade(12.0m);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_grades_with_different_values_are_not_equal() {
        Assert.NotEqual(new Grade(12m), new Grade(12.5m));
    }

    [Fact]
    public void A_grade_prints_out_of_twenty() {
        Assert.Equal("12.5/20", new Grade(12.5m).ToString());
    }

    [Fact]
    public void A_drawn_grade_is_always_a_valid_half_point_grade() {
        // The generator is hand-completed: check it never draws what the constructor refuses.
        for (int i = 0; i < 300; i++) {
            Grade anyGrade = Any.Grade().Generate();

            Assert.InRange(anyGrade.Value, 0m, 20m);
            Assert.Equal(0m, anyGrade.Value % 0.5m);
        }
    }

}
