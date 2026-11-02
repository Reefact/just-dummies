namespace RealEstate.Tests.Regular;

public sealed class SurfaceTests {

    [Fact]
    public void A_surface_exposes_the_square_meters_it_was_built_with() {
        // Act
        Surface surface = new Surface(83.25);

        // Assert
        Assert.Equal(83.25, surface.SquareMeters);
    }

    [Fact]
    public void A_surface_can_be_as_small_as_a_hundredth_of_a_square_meter() {
        // Act
        Surface surface = new Surface(0.01);

        // Assert
        Assert.Equal(0.01, surface.SquareMeters);
    }

    [Fact]
    public void A_surface_can_be_as_large_as_the_maximum() {
        // Act
        Surface surface = new Surface(Surface.MaxSquareMeters);

        // Assert
        Assert.Equal(10_000_000d, surface.SquareMeters);
    }

    [Fact]
    public void NaN_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Surface(double.NaN));

        // Assert
        Assert.Equal("squareMeters", exception.ParamName);
    }

    [Fact]
    public void Zero_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(0d));
    }

    [Fact]
    public void A_negative_surface_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(anyNegative));
    }

    [Fact]
    public void A_surface_above_the_maximum_is_refused() {
        // Arrange
        double anyTooLarge = Any.Double().GreaterThan(Surface.MaxSquareMeters).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(anyTooLarge));
    }

    [Fact]
    public void The_first_hundredth_above_the_maximum_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(10_000_000.01));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_infinity_is_refused_as_out_of_range(double infinity) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Surface(infinity));
    }

    [Theory]
    [InlineData(12.345)]
    [InlineData(0.001)]
    [InlineData(80.005)]
    [InlineData(1.0000001)]
    public void A_measure_finer_than_the_hundredth_is_refused(double tooFine) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Surface(tooFine));

        // Assert
        Assert.Equal("squareMeters", exception.ParamName);
    }

    [Theory]
    [InlineData(10_000d, 1d)]
    [InlineData(25_000d, 2.5d)]
    [InlineData(1d, 0.0001d)]
    public void The_surface_is_expressed_in_hectares(double squareMeters, double hectares) {
        // Arrange
        Surface surface = new Surface(squareMeters);

        // Act
        double actual = surface.Hectares;

        // Assert
        Assert.Equal(hectares, actual, 10);
    }

    [Theory]
    [InlineData(7.99, false)]
    [InlineData(8.00, true)]
    [InlineData(8.01, true)]
    [InlineData(0.01, false)]
    [InlineData(120.00, true)]
    public void A_lot_counts_under_the_Carrez_law_from_eight_square_meters(double squareMeters, bool counts) {
        // Arrange
        Surface surface = new Surface(squareMeters);

        // Act
        bool actual = surface.CountsUnderCarrezLaw;

        // Assert
        Assert.Equal(counts, actual);
    }

    [Theory]
    [InlineData(12.34, 0.01, 12.35)]
    [InlineData(0.1, 0.2, 0.3)]
    [InlineData(0.07, 0.01, 0.08)]
    [InlineData(1_000.5, 2_000.25, 3_000.75)]
    public void Adding_two_surfaces_gives_their_sum_to_the_hundredth(double left, double right, double sum) {
        // Arrange
        Surface leftSurface  = new Surface(left);
        Surface rightSurface = new Surface(right);

        // Act
        Surface total = leftSurface.Add(rightSurface);

        // Assert
        Assert.Equal(new Surface(sum), total);
        Assert.Equal(sum, total.SquareMeters);
    }

    [Fact]
    public void Adding_does_not_change_the_operands() {
        // Arrange
        Surface left  = new Surface(5);
        Surface right = new Surface(6);

        // Act
        _ = left.Add(right);

        // Assert
        Assert.Equal(5d, left.SquareMeters);
        Assert.Equal(6d, right.SquareMeters);
    }

    [Fact]
    public void A_sum_above_the_maximum_is_refused() {
        // Arrange
        Surface atMaximum = new Surface(Surface.MaxSquareMeters);
        Surface anyAddition = Any.Surface().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => atMaximum.Add(anyAddition));
    }

    [Fact]
    public void Two_surfaces_with_the_same_measure_are_equal() {
        // Arrange
        Surface first  = new Surface(42.5);
        Surface second = new Surface(42.5);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_surfaces_with_different_measures_are_not_equal() {
        // Arrange
        Surface first  = new Surface(42.5);
        Surface second = new Surface(42.51);

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_surface_is_not_equal_to_null_or_to_a_number() {
        // Arrange
        Surface anySurface = Any.Surface().Generate();

        // Assert
        Assert.False(anySurface.Equals((Surface?)null));
        Assert.False(anySurface.Equals(null));
        Assert.False(anySurface.Equals(anySurface.SquareMeters));
    }

}
