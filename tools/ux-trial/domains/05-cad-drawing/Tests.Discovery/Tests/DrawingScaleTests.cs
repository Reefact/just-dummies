using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class DrawingScaleTests {

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 50)]
    [InlineData(1, 7)]            // no preferred series is enforced
    [InlineData(1, int.MaxValue)]
    [InlineData(2, 1)]
    [InlineData(10, 1)]
    [InlineData(13, 1)]
    [InlineData(int.MaxValue, 1)]
    public void A_scale_with_a_one_on_one_side_is_accepted(int numerator, int denominator) {
        // Act
        DrawingScale scale = new DrawingScale(numerator, denominator);

        // Assert
        Assert.Equal(numerator, scale.Numerator);
        Assert.Equal(denominator, scale.Denominator);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(int.MinValue, 1)]
    [InlineData(0, 0)]            // the numerator is checked first
    [InlineData(0, 5)]
    public void A_numerator_below_one_is_refused(int numerator, int denominator) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(numerator, denominator));

        Assert.Equal("numerator", error.ParamName);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(5, 0)]            // 5:0 fails on the term before it fails on the shape
    [InlineData(1, int.MinValue)]
    public void A_denominator_below_one_is_refused(int numerator, int denominator) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(numerator, denominator));

        Assert.Equal("denominator", error.ParamName);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(5, 10)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void A_scale_with_no_one_on_either_side_is_refused_as_an_argument_error(int numerator, int denominator) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new DrawingScale(numerator, denominator));

        Assert.IsNotType<ArgumentOutOfRangeException>(error);
    }

    [Theory]
    [InlineData(1, 2, 100.0, 50.0)]
    [InlineData(1, 1, 37.5, 37.5)]
    [InlineData(5, 1, 10.0, 50.0)]
    [InlineData(2, 1, 0.0, 0.0)]
    [InlineData(1, 4, 10.0, 2.5)]
    [InlineData(1, 3, 1.0, 1.0 / 3.0)]
    [InlineData(2, 1, -4.0, -8.0)]     // PaperLengthOf does not validate
    public void The_paper_length_scales_the_real_length(int numerator, int denominator, double real, double expected) {
        // Arrange
        DrawingScale scale = new DrawingScale(numerator, denominator);

        // Act
        double paper = scale.PaperLengthOf(real);

        // Assert
        Assert.Equal(expected, paper, 12);
    }

    [Fact]
    public void A_large_enlargement_does_not_overflow_the_paper_length() {
        // Arrange
        DrawingScale scale = new DrawingScale(int.MaxValue, 1);

        // Act
        double paper = scale.PaperLengthOf(1000.0);

        // Assert
        Assert.Equal(1000.0 * int.MaxValue, paper, 3);
    }

    [Theory]
    [InlineData(1, 2, "1:2")]
    [InlineData(5, 1, "5:1")]
    [InlineData(1, 1, "1:1")]
    public void ToString_writes_numerator_colon_denominator(int numerator, int denominator, string expected) {
        Assert.Equal(expected, new DrawingScale(numerator, denominator).ToString());
    }

}
