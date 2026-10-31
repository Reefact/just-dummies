using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Regular;

public sealed class ExperimentalConditionsTests {

    [Fact]
    public void A_centrifuged_chamber_cannot_hold_more_than_10_millilitres() {
        // Arrange
        double anyTemperature = Arbitrary.ExperimentalConditions().Generate().TemperatureCelsius;
        double anyNutrient    = Arbitrary.ExperimentalConditions().Generate().NutrientGramsPerLitre;

        // Act
        Exception? exception = Record.Exception(() => new ExperimentalConditions(1.0, anyTemperature, anyNutrient, 20));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(0.001, true)]
    [InlineData(1.0, false)]
    public void Microgravity_is_below_a_hundredth_of_a_g(double gravity, bool expected) {
        // Arrange
        ExperimentalConditions anyConditions = Arbitrary.ExperimentalConditions().WithGravityG(gravity).Generate();

        // Act
        bool microgravity = anyConditions.IsMicrogravity;

        // Assert
        Assert.Equal(expected, microgravity);
    }

    [Fact]
    public void A_species_grows_20_percent_faster_in_weightlessness_than_at_1_g() {
        // Arrange
        // Below 4 h⁻¹ so that the 20 % boost never reaches the 5 h⁻¹ ceiling of a growth rate.
        Species anyMesophile = Lab.Mesophile().WithMaxGrowthRate(r => r.WithPerHour(p => p.LessThanOrEqualTo(4))).Generate();
        ExperimentalConditions anyOnGround   = Lab.WhereMesophilesGrow(Arbitrary.ExperimentalConditions()).WithGravityG(1.0).Generate();
        ExperimentalConditions anyInOrbitTwin = new ExperimentalConditions(0, anyOnGround.TemperatureCelsius, anyOnGround.NutrientGramsPerLitre, anyOnGround.VolumeMillilitres);

        // Act
        double ratio = anyInOrbitTwin.RelativeGrowthAgainst(anyOnGround, anyMesophile);

        // Assert
        Assert.Equal(1.2, ratio, 9);
    }

    [Fact]
    public void Weightlessness_raises_the_carrying_capacity_by_30_percent() {
        // Arrange
        Species anySpecies = Arbitrary.Species().Generate();
        ExperimentalConditions anyOnGround    = Arbitrary.ExperimentalConditions().WithGravityG(1.0).Generate();
        ExperimentalConditions anyInOrbitTwin = new ExperimentalConditions(0, anyOnGround.TemperatureCelsius, anyOnGround.NutrientGramsPerLitre, anyOnGround.VolumeMillilitres);

        // Act
        double ratio = anyInOrbitTwin.CarryingCapacity(anySpecies).CellsPerMillilitre / anyOnGround.CarryingCapacity(anySpecies).CellsPerMillilitre;

        // Assert
        Assert.Equal(1.3, ratio, 9);
    }

    [Fact]
    public void No_growth_ratio_is_formed_against_conditions_where_the_species_does_not_grow() {
        // Arrange
        Species                anyMesophile = Lab.Mesophile().Generate();
        ExperimentalConditions anyTooCold   = Arbitrary.ExperimentalConditions().WithTemperatureCelsius(5).Generate();
        ExperimentalConditions anyOther     = Arbitrary.ExperimentalConditions().Generate();

        // Act
        Exception? exception = Record.Exception(() => anyOther.RelativeGrowthAgainst(anyTooCold, anyMesophile));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

}
