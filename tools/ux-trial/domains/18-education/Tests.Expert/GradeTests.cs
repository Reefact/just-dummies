using Education.Domain;

using Xunit;

namespace Education.Tests.Expert;

public sealed class GradeTests {

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(10)]
    [InlineData(19.5)]
    [InlineData(20)]
    public void A_grade_in_half_points_between_0_and_20_is_kept(double value) {
        // Act
        Grade grade = new Grade((decimal)value);

        // Assert
        Assert.Equal((decimal)value, grade.Value);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(20.5)]
    [InlineData(21)]
    public void A_grade_outside_0_to_20_is_refused_as_out_of_range(double value) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Grade((decimal)value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
        Assert.Equal((decimal)value, refusal.ActualValue);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(10.25)]
    [InlineData(12.75)]
    [InlineData(19.99)]
    public void A_grade_that_is_not_a_multiple_of_half_a_point_is_refused(double value) {
        // Act (the exact type: an out-of-range refusal is not what is expected here)
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Grade((decimal)value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_grade_both_out_of_range_and_off_the_half_point_grid_is_refused_for_its_range() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grade(-0.25m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grade(20.25m));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4.5, true)]
    [InlineData(5, false)]
    [InlineData(5.5, false)]
    [InlineData(20, false)]
    public void A_grade_below_5_is_eliminatory(double value, bool expected) {
        // Arrange
        Grade grade = new Grade((decimal)value);

        // Assert
        Assert.Equal(expected, grade.IsEliminatory);
    }

    [Fact]
    public void Two_grades_of_the_same_value_are_equal_whatever_their_decimal_representation() {
        // Arrange
        Grade grade = new Grade(12.5m);
        Grade same  = new Grade(12.50m);

        // Assert
        Assert.True(grade.Equals(same));
        Assert.Equal(grade.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_grades_of_different_values_are_not_equal() {
        // Assert
        Assert.False(new Grade(12.5m).Equals(new Grade(13m)));
    }

    [Fact]
    public void A_grade_is_not_equal_to_its_number_nor_to_null() {
        // Arrange
        Grade grade = new Grade(12m);

        // Assert
        Assert.False(grade.Equals(12m));
        Assert.False(grade.Equals(null));
    }

    [Fact]
    public void A_whole_grade_prints_out_of_20() {
        // Assert
        Assert.Equal("12/20", new Grade(12m).ToString());
    }

}
