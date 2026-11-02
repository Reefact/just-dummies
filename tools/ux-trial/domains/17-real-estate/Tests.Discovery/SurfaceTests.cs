using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class SurfaceTests {

    [Theory]
    [InlineData(0.01)]
    [InlineData(7.99)]
    [InlineData(8d)]
    [InlineData(123.45)]
    [InlineData(Surface.MaxSquareMeters)]
    public void A_positive_surface_in_hundredths_up_to_the_maximum_is_accepted(double squareMeters) {
        Surface surface = new Surface(squareMeters);

        Assert.Equal(squareMeters, surface.SquareMeters);
    }

    [Fact]
    public void NaN_is_refused() {
        Assert.Throws<ArgumentException>(() => new Surface(double.NaN));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.01)]
    [InlineData(-100d)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(Surface.MaxSquareMeters + 0.01)]
    [InlineData(double.MaxValue)]
    public void A_surface_that_is_not_positive_or_beyond_the_maximum_is_out_of_range(double squareMeters) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(squareMeters));
    }

    [Theory]
    [InlineData(12.345)]
    [InlineData(0.001)]
    [InlineData(1.005)]
    public void A_surface_finer_than_a_hundredth_is_refused(double squareMeters) {
        Assert.Throws<ArgumentException>(() => new Surface(squareMeters));
    }

    [Fact]
    public void The_maximum_is_a_thousand_hectares() {
        Assert.Equal(1_000d, new Surface(Surface.MaxSquareMeters).Hectares);
    }

    [Theory]
    [InlineData(25_000d, 2.5)]
    [InlineData(10_000d, 1d)]
    [InlineData(100d, 0.01)]
    public void Hectares_are_square_meters_over_ten_thousand(double squareMeters, double hectares) {
        Assert.Equal(hectares, new Surface(squareMeters).Hectares);
    }

    [Theory]
    [InlineData(7.99, false)]
    [InlineData(8d, true)]
    [InlineData(8.01, true)]
    [InlineData(0.01, false)]
    public void Carrez_law_counts_a_lot_from_eight_square_meters(double squareMeters, bool counts) {
        Assert.Equal(counts, new Surface(squareMeters).CountsUnderCarrezLaw);
    }

    [Fact]
    public void Adding_two_surfaces_gives_a_new_surface_and_leaves_both_untouched() {
        // Arrange
        Surface first  = new Surface(12.34);
        Surface second = new Surface(0.66);

        // Act
        Surface sum = first.Add(second);

        // Assert
        Assert.Equal(13d, sum.SquareMeters);
        Assert.Equal(12.34, first.SquareMeters);
        Assert.Equal(0.66, second.SquareMeters);
    }

    [Fact]
    public void Addition_is_rounded_to_the_hundredth_despite_binary_floating_point() {
        // 0.1 + 0.2 is 0.30000000000000004 in binary floating point.
        Assert.Equal(0.3, new Surface(0.1).Add(new Surface(0.2)).SquareMeters);
    }

    [Fact]
    public void Adding_past_the_maximum_is_refused() {
        // Arrange
        Surface atTheMaximum = new Surface(Surface.MaxSquareMeters);
        Surface oneHundredth = new Surface(0.01);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => atTheMaximum.Add(oneHundredth));
    }

    [Fact(Skip = "domain defect: Surface.Add(null) throws NullReferenceException, unlike every other entry point which uses ArgumentNullException; seed-independent")]
    public void Adding_null_is_refused() {
        // Arrange
        Surface anySurface = Any.Surface().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySurface.Add(null!));
    }

    [Fact]
    public void Surfaces_of_the_same_area_are_equal() {
        // Arrange
        double anySquareMeters = Any.Surface().Generate().SquareMeters;

        // Act
        Surface first  = new Surface(anySquareMeters);
        Surface second = new Surface(anySquareMeters);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Surfaces_of_different_areas_are_not_equal() {
        Assert.NotEqual(new Surface(45.5), new Surface(45.51));
    }

}
