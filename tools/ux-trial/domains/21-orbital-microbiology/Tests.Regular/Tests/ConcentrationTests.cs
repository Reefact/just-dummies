using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class ConcentrationTests {

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1e13)]
    public void A_concentration_inside_the_range_is_kept(double cellsPerMillilitre) {
        // Act
        Concentration concentration = new Concentration(cellsPerMillilitre);

        // Assert
        Assert.Equal(cellsPerMillilitre, concentration.CellsPerMillilitre);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.0000001e13)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_concentration_outside_the_range_is_refused(double cellsPerMillilitre) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Concentration(cellsPerMillilitre));

        // Assert
        Assert.Equal("cellsPerMillilitre", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1000, 3)]
    [InlineData(1e13, 13)]
    [InlineData(0.01, -2)]
    public void The_logarithm_is_the_decimal_one(double cellsPerMillilitre, double expectedLog10) {
        Assert.Equal(expectedLog10, new Concentration(cellsPerMillilitre).Log10, precision: 12);
    }

    [Fact]
    public void An_empty_culture_has_no_logarithm() {
        Assert.Throws<InvalidOperationException>(() => Concentration.Zero.Log10);
    }

    [Theory]
    [InlineData(100, 800, 3)]
    [InlineData(800, 100, -3)]
    [InlineData(1000, 1000, 0)]
    [InlineData(1, 1024, 10)]
    public void Generations_to_a_target_count_doublings(double from, double to, double expectedGenerations) {
        // Arrange
        Concentration start  = new Concentration(from);
        Concentration target = new Concentration(to);

        // Act
        double generations = start.GenerationsTo(target);

        // Assert
        Assert.Equal(expectedGenerations, generations, precision: 12);
    }

    [Fact]
    public void Generations_cannot_start_from_an_empty_culture() {
        // Arrange
        Concentration anyNonEmpty = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => Concentration.Zero.GenerationsTo(anyNonEmpty));
    }

    [Fact]
    public void Generations_cannot_reach_an_empty_culture() {
        // Arrange
        Concentration anyNonEmpty = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyNonEmpty.GenerationsTo(Concentration.Zero));
    }

    [Fact]
    public void Generations_need_a_target() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyConcentration.GenerationsTo(null!));
    }

    [Fact]
    public void Concentrations_are_ordered_by_cell_count() {
        // Arrange
        Concentration smaller = new Concentration(10);
        Concentration larger  = new Concentration(20);

        // Assert
        Assert.True(smaller.CompareTo(larger) < 0);
        Assert.True(larger.CompareTo(smaller) > 0);
        Assert.Equal(0, smaller.CompareTo(new Concentration(10)));
    }

    [Fact]
    public void Everything_is_greater_than_nothing() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().Generate();

        // Assert
        Assert.Equal(1, anyConcentration.CompareTo(null));
    }

    [Fact]
    public void Two_concentrations_with_the_same_count_are_equal() {
        // Act
        Concentration first  = new Concentration(2.5e6);
        Concentration second = new Concentration(2.5e6);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_concentrations_with_different_counts_are_not_equal() {
        // Assert
        Assert.NotEqual(new Concentration(2.5e6), new Concentration(2.6e6));
    }

}
