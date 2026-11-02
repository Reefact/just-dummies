using System.Globalization;

using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class SurfaceTests {

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(123.45)]
    [InlineData(9_999_999.99)]
    [InlineData(10_000_000)]
    public void Accepts_a_surface_measured_to_the_hundredth_up_to_the_maximum(double squareMeters) {
        // Act
        Surface surface = new Surface(squareMeters);

        // Assert
        Assert.Equal(squareMeters, surface.SquareMeters);
    }

    [Fact]
    public void Refuses_a_surface_that_is_not_a_number() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Surface(double.NaN));

        // Assert
        Assert.Equal("squareMeters", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(10_000_000.01)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_surface_that_is_not_above_zero_and_within_the_maximum(double squareMeters) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(squareMeters));
    }

    [Theory]
    [InlineData(1.005)]
    [InlineData(12.345)]
    [InlineData(0.001)]
    [InlineData(0.1 + 0.2)]
    public void Refuses_a_surface_finer_than_the_hundredth(double squareMeters) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Surface(squareMeters));

        // Assert
        Assert.Equal("squareMeters", exception.ParamName);
    }

    [Theory]
    [InlineData(25_000, 2.5)]
    [InlineData(10_000, 1)]
    [InlineData(0.01, 0.000001)]
    [InlineData(10_000_000, 1_000)]
    public void Converts_square_meters_to_hectares(double squareMeters, double hectares) {
        // Act
        double actual = new Surface(squareMeters).Hectares;

        // Assert
        Assert.Equal(hectares, actual, 12);
    }

    [Theory]
    [InlineData(7.99, false)]
    [InlineData(8.00, true)]
    [InlineData(8.01, true)]
    [InlineData(0.01, false)]
    [InlineData(10_000_000, true)]
    public void Counts_under_the_Carrez_law_from_eight_square_meters(double squareMeters, bool counts) {
        // Act
        bool actual = new Surface(squareMeters).CountsUnderCarrezLaw;

        // Assert
        Assert.Equal(counts, actual);
    }

    [Theory]
    [InlineData(12.34, 0.66, 13.00)]
    [InlineData(0.10, 0.20, 0.30)]
    [InlineData(0.01, 0.01, 0.02)]
    [InlineData(5_000_000, 5_000_000, 10_000_000)]
    public void Adds_two_surfaces_to_the_hundredth(double first, double second, double expectedSum) {
        // Act
        Surface sum = new Surface(first).Add(new Surface(second));

        // Assert
        Assert.Equal(new Surface(expectedSum), sum);
    }

    [Fact]
    public void Refuses_a_sum_above_the_maximum() {
        // Arrange
        Surface largest = new Surface(Surface.MaxSquareMeters);
        Surface smallest = new Surface(0.01);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => largest.Add(smallest));
    }

    [Fact]
    public void Leaves_both_operands_untouched_when_adding() {
        // Arrange
        Surface first  = new Surface(12.34);
        Surface second = new Surface(0.66);

        // Act
        first.Add(second);

        // Assert
        Assert.Equal(12.34, first.SquareMeters);
        Assert.Equal(0.66, second.SquareMeters);
    }

    [Fact]
    public void Two_surfaces_of_the_same_size_are_equal() {
        // Arrange
        double anySquareMeters = Any.Surface().Generate().SquareMeters;

        // Act
        Surface first  = new Surface(anySquareMeters);
        Surface second = new Surface(anySquareMeters);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_surfaces_of_different_sizes_are_not_equal() {
        // Arrange
        Surface anySurface      = Any.Surface().Generate();
        Surface anyOtherSurface = Any.Surface().WithSquareMeters(squareMeters => squareMeters.DifferentFrom(anySurface.SquareMeters)).Generate();

        // Act & Assert
        Assert.False(anySurface.Equals(anyOtherSurface));
    }

    [Fact]
    public void Is_not_equal_to_nothing_or_to_a_plain_number() {
        // Arrange
        Surface anySurface = Any.Surface().Generate();

        // Act & Assert
        Assert.False(anySurface.Equals((Surface?)null));
        Assert.False(anySurface.Equals((object?)null));
        Assert.False(anySurface.Equals((object)anySurface.SquareMeters));
    }

    [Fact]
    public void Prints_two_decimals_and_the_unit() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try {
            // Act
            string text = new Surface(12.5).ToString();

            // Assert
            Assert.Equal("12.50 m²", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
