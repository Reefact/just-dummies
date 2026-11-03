using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class ConcentrationTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(2.5e6)]
    [InlineData(1e13)]
    public void A_concentration_from_zero_to_ten_trillion_cells_per_millilitre_is_accepted(double cells) {
        // Act
        Concentration concentration = new Concentration(cells);

        // Assert
        Assert.Equal(cells, concentration.CellsPerMillilitre);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(1.0000001e13)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_concentration_outside_its_range_or_not_finite_is_refused(double cells) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Concentration(cells));
    }

    [Fact]
    public void A_million_cells_per_millilitre_is_six_logs() {
        // Arrange
        Concentration concentration = new Concentration(1e6);

        // Act / Assert
        Assert.Equal(6.0, concentration.Log10, precision: 12);
    }

    [Fact]
    public void An_empty_culture_has_no_logarithm() {
        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => Concentration.Zero.Log10);
    }

    [Fact]
    public void Growing_from_one_thousand_to_eight_thousand_takes_three_generations() {
        // Arrange
        Concentration from = new Concentration(1e3);
        Concentration to   = new Concentration(8e3);

        // Act
        double generations = from.GenerationsTo(to);

        // Assert
        Assert.Equal(3.0, generations, precision: 12);
    }

    [Fact]
    public void Shrinking_to_a_quarter_takes_minus_two_generations() {
        // Arrange
        Concentration from = new Concentration(4e6);
        Concentration to   = new Concentration(1e6);

        // Act / Assert
        Assert.Equal(-2.0, from.GenerationsTo(to), precision: 12);
    }

    [Fact]
    public void Staying_at_the_same_concentration_takes_no_generation() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act
        double generations = anyConcentration.GenerationsTo(anyConcentration);

        // Assert
        Assert.Equal(0.0, generations);
    }

    [Fact]
    public void Generations_cannot_be_counted_from_an_empty_culture() {
        // Arrange
        Concentration anyTarget = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => Concentration.Zero.GenerationsTo(anyTarget));
    }

    [Fact]
    public void Generations_cannot_be_counted_to_an_empty_culture() {
        // Arrange
        Concentration anyStart = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyStart.GenerationsTo(Concentration.Zero));
    }

    [Fact]
    public void Generations_cannot_be_counted_to_nothing() {
        // Arrange
        Concentration anyStart = Any.Concentration().WithCellsPerMillilitre(c => c.Positive()).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyStart.GenerationsTo(null!));
    }

    [Fact]
    public void A_lower_concentration_sorts_before_a_higher_one() {
        // Arrange
        Concentration lower  = new Concentration(1e3);
        Concentration higher = new Concentration(1e4);

        // Act / Assert
        Assert.True(lower.CompareTo(higher) < 0);
        Assert.True(higher.CompareTo(lower) > 0);
        Assert.Equal(0, lower.CompareTo(new Concentration(1e3)));
    }

    [Fact]
    public void Anything_sorts_after_nothing() {
        // Arrange
        Concentration anyConcentration = Any.Concentration().Generate();

        // Act / Assert
        Assert.True(anyConcentration.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_concentrations_with_the_same_value_are_equal() {
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
    public void Two_concentrations_with_different_values_are_not_equal() {
        // Arrange
        Concentration anyConcentration      = Any.Concentration().Generate();
        Concentration anyOtherConcentration = Any.Concentration().WithCellsPerMillilitre(c => c.DifferentFrom(anyConcentration.CellsPerMillilitre)).Generate();

        // Act / Assert
        Assert.NotEqual(anyConcentration, anyOtherConcentration);
    }

}
