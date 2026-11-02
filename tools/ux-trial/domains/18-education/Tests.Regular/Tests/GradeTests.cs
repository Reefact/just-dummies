using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Regular;

public sealed class GradeTests {

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(4.5)]
    [InlineData(10)]
    [InlineData(19.5)]
    [InlineData(20)]
    public void A_half_point_value_between_zero_and_twenty_is_kept(double value) {
        // Act
        Grade grade = new Grade((decimal)value);

        // Assert
        Assert.Equal((decimal)value, grade.Value);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(-1)]
    [InlineData(20.5)]
    [InlineData(21)]
    [InlineData(100)]
    public void A_value_outside_zero_to_twenty_is_refused(double value) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Grade((decimal)value));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(10.1)]
    [InlineData(19.75)]
    [InlineData(0.01)]
    public void A_value_that_is_not_a_multiple_of_half_a_point_is_refused(double value) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Grade((decimal)value));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void The_range_is_checked_before_the_step() {
        // 20.25 is both out of range and off the grid: the range wins.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grade(20.25m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grade(-0.25m));
    }

    [Fact]
    public void A_value_written_with_trailing_zeros_is_accepted_and_equal_to_the_plain_one() {
        // Assert
        Assert.Equal(new Grade(12.5m), new Grade(12.500m));
        Assert.Equal(new Grade(12m), new Grade(12.0m));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(4.5, true)]
    [InlineData(5, false)]
    [InlineData(5.5, false)]
    [InlineData(10, false)]
    [InlineData(20, false)]
    public void A_grade_is_eliminatory_strictly_below_five(double value, bool expected) {
        // Arrange
        Grade grade = new Grade((decimal)value);

        // Assert
        Assert.Equal(expected, grade.IsEliminatory);
    }

    [Fact]
    public void Two_grades_with_the_same_value_are_equal() {
        // Arrange
        Grade anyGrade     = Any.Grade().Generate();
        Grade anySameGrade = new Grade(anyGrade.Value);

        // Assert
        Assert.Equal(anyGrade, anySameGrade);
        Assert.Equal(anyGrade.GetHashCode(), anySameGrade.GetHashCode());
    }

    [Fact]
    public void Two_grades_with_different_values_are_different() {
        // Arrange
        Grade anyGrade      = Any.Grade().Generate();
        Grade anyOtherGrade = Any.Grade().WithValue(v => v.DifferentFrom(anyGrade.Value)).Generate();

        // Assert
        Assert.NotEqual(anyGrade, anyOtherGrade);
    }

    [Fact]
    public void A_grade_is_written_out_of_twenty() {
        // Assert
        Assert.Equal("14.5/20", new Grade(14.5m).ToString());
    }

}
