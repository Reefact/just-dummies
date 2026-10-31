namespace OrbitalMicrobiology.Tests.Expert;

public sealed class ExperimentalConditionsTests {

    [Fact]
    public void A_centrifuged_chamber_cannot_hold_more_than_ten_millilitres() {
        // Arrange
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new ExperimentalConditions(1.0, anyConditions.TemperatureCelsius, anyConditions.NutrientGramsPerLitre, 20));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Microgravity_carries_thirty_percent_more_cells_than_the_one_g_control() {
        // Arrange
        Species                anySpecies      = Any.Species().Generate();
        ExperimentalConditions anyGroundControl = Any.ExperimentalConditions().WithGravityG(1.0).Generate();
        ExperimentalConditions sameInOrbit      = SameChamberAt(anyGroundControl, gravityG: 0);

        // Act
        double ratio = sameInOrbit.CarryingCapacity(anySpecies).CellsPerMillilitre
                     / anyGroundControl.CarryingCapacity(anySpecies).CellsPerMillilitre;

        // Assert
        Assert.Equal(1.3, ratio, 9);
    }

    [Fact]
    public void Microgravity_speeds_growth_up_by_twenty_percent_against_the_one_g_control() {
        // Arrange
        // Below 4 h^-1 the boosted rate stays under the 5 h^-1 ceiling a growth rate never exceeds.
        Species anySpecies = Any.Species().WithMaxGrowthRate(r => r.WithPerHour(p => p.LessThanOrEqualTo(4))).Generate();
        ExperimentalConditions anyGroundControl = Any.ExperimentalConditions().WithGravityG(1.0).AtTheOptimumOf(anySpecies).Generate();
        ExperimentalConditions sameInOrbit      = SameChamberAt(anyGroundControl, gravityG: 0);

        // Act
        double relativeGrowth = sameInOrbit.RelativeGrowthAgainst(anyGroundControl, anySpecies);

        // Assert
        Assert.Equal(1.2, relativeGrowth, 9);
    }

    [Fact]
    public void Growth_cannot_be_compared_against_conditions_where_the_species_does_not_grow() {
        // Arrange
        Species                anySpecies    = Any.Species().WithCardinalTemperatures(minimum: 10, optimum: 30, maximum: 40).Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        ExperimentalConditions anyTooCold    = Any.ExperimentalConditions().WithTemperatureCelsius(5).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyConditions.RelativeGrowthAgainst(anyTooCold, anySpecies));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    private static ExperimentalConditions SameChamberAt(ExperimentalConditions conditions, double gravityG) {
        return new ExperimentalConditions(gravityG, conditions.TemperatureCelsius, conditions.NutrientGramsPerLitre, conditions.VolumeMillilitres);
    }

}
