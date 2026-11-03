using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class DrawingScaleTests {

    [Fact]
    public void An_enlargement_keeps_its_numerator_and_has_one_as_denominator() {
        // Arrange
        int anyFactor = Any.Int32().GreaterThan(1).Generate();

        // Act
        DrawingScale scale = new DrawingScale(anyFactor, 1);

        // Assert
        Assert.Equal(anyFactor, scale.Numerator);
        Assert.Equal(1, scale.Denominator);
    }

    [Fact]
    public void A_reduction_keeps_its_denominator_and_has_one_as_numerator() {
        // Arrange
        int anyFactor = Any.Int32().GreaterThan(1).Generate();

        // Act
        DrawingScale scale = new DrawingScale(1, anyFactor);

        // Assert
        Assert.Equal(1, scale.Numerator);
        Assert.Equal(anyFactor, scale.Denominator);
    }

    [Fact]
    public void The_full_size_scale_is_one_to_one() {
        // Act
        DrawingScale scale = new DrawingScale(1, 1);

        // Assert
        Assert.Equal("1:1", scale.ToString());
    }

    [Fact]
    public void Any_generated_scale_has_one_on_a_side() {
        // Act
        DrawingScale anyScale = Any.DrawingScale().Generate();

        // Assert
        Assert.True(anyScale.Numerator == 1 || anyScale.Denominator == 1);
    }

    [Fact]
    public void A_numerator_below_one_is_refused() {
        // Arrange
        int anyNonPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(anyNonPositive, 1));

        // Assert
        Assert.Equal("numerator", exception.ParamName);
    }

    [Fact]
    public void A_denominator_below_one_is_refused() {
        // Arrange
        int anyNonPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(1, anyNonPositive));

        // Assert
        Assert.Equal("denominator", exception.ParamName);
    }

    [Fact]
    public void The_numerator_is_checked_before_the_denominator() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(0, 0));

        // Assert
        Assert.Equal("numerator", exception.ParamName);
    }

    [Fact]
    public void A_scale_with_no_one_on_either_side_is_refused_as_an_argument_error() {
        // Arrange
        int anyNumerator   = Any.Int32().GreaterThan(1).Generate();
        int anyDenominator = Any.Int32().GreaterThan(1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DrawingScale(anyNumerator, anyDenominator));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void A_one_to_n_scale_is_not_restricted_to_the_preferred_series() {
        // Arrange
        int anyReduction = Any.Int32().GreaterThan(1).Generate();

        // Act / Assert
        Assert.Equal($"1:{anyReduction}", new DrawingScale(1, anyReduction).ToString());
    }

    [Theory]
    [InlineData(2, 1, 50.0, 100.0)]
    [InlineData(1, 2, 50.0, 25.0)]
    [InlineData(1, 1, 37.5, 37.5)]
    [InlineData(5, 1, 0.0, 0.0)]
    [InlineData(1, 10, 125.0, 12.5)]
    public void The_paper_length_scales_the_real_length(int numerator, int denominator, double real, double expected) {
        // Arrange
        DrawingScale scale = new DrawingScale(numerator, denominator);

        // Act
        double paper = scale.PaperLengthOf(real);

        // Assert
        Assert.Equal(expected, paper, 9);
    }

    [Fact]
    public void The_paper_length_is_not_validated() {
        // Arrange
        DrawingScale scale = new DrawingScale(1, 2);

        // Assert
        Assert.Equal(-5.0, scale.PaperLengthOf(-10.0), 9);
    }

    [Fact]
    public void A_scale_reads_as_numerator_colon_denominator() {
        // Assert
        Assert.Equal("5:1", new DrawingScale(5, 1).ToString());
        Assert.Equal("1:20", new DrawingScale(1, 20).ToString());
    }

}
