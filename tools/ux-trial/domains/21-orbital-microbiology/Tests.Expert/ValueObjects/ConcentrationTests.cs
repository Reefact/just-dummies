using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class ConcentrationTests {

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(1e13)]
    public void A_concentration_within_zero_and_ten_trillion_cells_per_millilitre_is_kept(double cells) {
        Assert.Equal(cells, new Concentration(cells).CellsPerMillilitre);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(1.0000001e13)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_concentration_outside_the_range_or_not_finite_is_refused(double cells) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Concentration(cells));
    }

    [Theory]
    [InlineData(1e6, 6d)]
    [InlineData(1d, 0d)]
    [InlineData(1e13, 13d)]
    [InlineData(1e-2, -2d)]
    public void The_log10_of_a_concentration_is_its_decimal_exponent(double cells, double expected) {
        Assert.Equal(expected, new Concentration(cells).Log10, precision: 10);
    }

    [Fact]
    public void An_empty_culture_has_no_logarithm() {
        Assert.Throws<InvalidOperationException>(() => Concentration.Zero.Log10);
    }

    [Theory]
    [InlineData(1e6, 8e6, 3d)]
    [InlineData(8e6, 1e6, -3d)]
    [InlineData(5e4, 5e4, 0d)]
    [InlineData(1d, 1024d, 10d)]
    public void The_generations_between_two_concentrations_are_the_log2_of_their_ratio(double from, double to, double expected) {
        // Act
        double generations = new Concentration(from).GenerationsTo(new Concentration(to));

        // Assert
        Assert.Equal(expected, generations, precision: 10);
    }

    [Fact]
    public void Generations_are_not_counted_from_an_empty_culture() {
        // Arrange
        Concentration anyTarget = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => Concentration.Zero.GenerationsTo(anyTarget));
    }

    [Fact]
    public void Generations_are_not_counted_to_an_empty_culture() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyConcentration.GenerationsTo(Concentration.Zero));
    }

    [Fact]
    public void Generations_are_not_counted_to_nothing() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyConcentration.GenerationsTo(null!));
    }

    [Fact]
    public void A_smaller_concentration_sorts_before_a_larger_one() {
        // Arrange
        Concentration anySmaller = Any.Concentration().WithCellsPerMillilitre(c => c.LessThan(1e6)).Generate();
        Concentration anyLarger  = Any.Concentration().WithCellsPerMillilitre(c => c.GreaterThan(1e6)).Generate();

        // Assert
        Assert.True(anySmaller.CompareTo(anyLarger) < 0);
        Assert.True(anyLarger.CompareTo(anySmaller) > 0);
        Assert.Equal(0, anySmaller.CompareTo(new Concentration(anySmaller.CellsPerMillilitre)));
    }

    [Fact]
    public void Everything_sorts_after_nothing() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().Generate();

        // Assert
        Assert.True(anyConcentration.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_concentrations_of_the_same_value_are_equal() {
        // Arrange
        double anyCells = Any.Concentration().Generate().CellsPerMillilitre;

        // Act
        Concentration first  = new Concentration(anyCells);
        Concentration second = new Concentration(anyCells);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_empty_concentration_is_a_concentration_of_zero() {
        Assert.Equal(0d, Concentration.Zero.CellsPerMillilitre);
        Assert.Equal(new Concentration(0d), Concentration.Zero);
    }

}
