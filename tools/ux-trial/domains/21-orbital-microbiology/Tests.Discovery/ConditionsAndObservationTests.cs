using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class ExperimentalConditionsTests {

    [Fact]
    public void A_centrifuged_chamber_refuses_more_than_ten_millilitres() {
        // Arrange
        double anyTemperature = Any.Double().Between(ExperimentalConditions.MinTemperatureCelsius, ExperimentalConditions.MaxTemperatureCelsius).Generate();
        double anyNutrient    = Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxNutrientGramsPerLitre).Generate();

        // Act
        Exception? error = Record.Exception(() => new ExperimentalConditions(1.0, anyTemperature, anyNutrient, 20));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_station_chamber_at_a_thousandth_of_a_g_is_in_microgravity() {
        // Arrange
        ExperimentalConditions anyStationChamber = Any.ExperimentalConditions().WithGravityG(0.001).Generate();

        // Act
        bool microgravity = anyStationChamber.IsMicrogravity;

        // Assert
        Assert.True(microgravity);
    }

    [Fact]
    public void Microgravity_raises_the_carrying_capacity_by_thirty_percent() {
        // Arrange
        Species anySpecies = Any.Species().WithYieldCellsPerGram(1e12).Generate();
        ExperimentalConditions anyWeightless = Any.ExperimentalConditions().WithGravityG(0).WithNutrientGramsPerLitre(10).Generate();
        ExperimentalConditions anyOneG       = Any.ExperimentalConditions().WithGravityG(1).WithNutrientGramsPerLitre(10).Generate();

        // Act
        Concentration weightless = anyWeightless.CarryingCapacity(anySpecies);
        Concentration oneG       = anyOneG.CarryingCapacity(anySpecies);

        // Assert
        Assert.Equal(1.0e10, oneG.CellsPerMillilitre, 1e-3);
        Assert.Equal(1.3e10, weightless.CellsPerMillilitre, 1e-3);
    }

    [Fact]
    public void A_species_does_not_grow_above_its_maximum_temperature() {
        // Arrange
        Species anySpecies10To40 = Any.Species()
                                      .WithMinTemperatureCelsius(10)
                                      .WithOptimalTemperatureCelsius(30)
                                      .WithMaxTemperatureCelsius(40)
                                      .Generate();
        ExperimentalConditions anyHotChamber = Any.ExperimentalConditions().WithTemperatureCelsius(50).Generate();

        // Act
        GrowthRate rate = anyHotChamber.EffectiveGrowthRate(anySpecies10To40);

        // Assert
        Assert.Equal(0.0, rate.PerHour);
    }

    [Fact]
    public void Weightlessness_speeds_growth_by_twenty_percent_against_a_one_g_control() {
        // Arrange
        Species anySpecies = Any.Species().WithMaxGrowthRate(new GrowthRate(0.5)).Generate();
        // Same temperature and nutrient on both sides: only gravity differs. Which ones does not matter,
        // as long as the species grows there.
        double anyTemperature = Any.Double().GreaterThan(anySpecies.MinTemperatureCelsius).LessThan(anySpecies.MaxTemperatureCelsius).Generate();
        double anyNutrient    = Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxNutrientGramsPerLitre).Generate();
        ExperimentalConditions weightless = new ExperimentalConditions(0, anyTemperature, anyNutrient, 5);
        ExperimentalConditions control    = new ExperimentalConditions(1, anyTemperature, anyNutrient, 5);

        // Act
        double relative = weightless.RelativeGrowthAgainst(control, anySpecies);

        // Assert
        Assert.Equal(1.2, relative, 9);
    }

    [Fact]
    public void No_ratio_is_formed_against_conditions_where_the_species_does_not_grow() {
        // Arrange
        Species anySpecies10To40 = Any.Species()
                                      .WithMinTemperatureCelsius(10)
                                      .WithOptimalTemperatureCelsius(30)
                                      .WithMaxTemperatureCelsius(40)
                                      .Generate();
        ExperimentalConditions anyConditions = Any.ExperimentalConditions().Generate();
        ExperimentalConditions anyTooCold    = Any.ExperimentalConditions().WithTemperatureCelsius(0).Generate();

        // Act
        Exception? error = Record.Exception(() => anyConditions.RelativeGrowthAgainst(anyTooCold, anySpecies10To40));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}

public sealed class ObservationTests {

    [Fact]
    public void An_optical_density_reading_cannot_claim_better_than_ten_percent() {
        // Arrange
        AnyObservation anyOpticalDensity = Any.Observation().WithMethod(ObservationMethod.OpticalDensity).WithRelativeUncertainty(0.05);

        // Act
        Exception? error = Record.Exception(() => anyOpticalDensity.Generate());

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_plate_count_may_claim_five_percent() {
        // Arrange
        AnyObservation anyPlateCount = Any.Observation().WithMethod(ObservationMethod.PlateCount).WithRelativeUncertainty(0.05);

        // Act
        Observation observation = anyPlateCount.Generate();

        // Assert
        Assert.False(observation.IsIndirect);
    }

    [Fact]
    public void The_one_sigma_bounds_frame_the_measurement() {
        // Arrange
        Observation anyObservation = Any.Observation()
                                        .WithMeasured(new Concentration(1e6))
                                        .WithRelativeUncertainty(0.1)
                                        .Generate();

        // Act
        double lower = anyObservation.LowerBoundCellsPerMillilitre;
        double upper = anyObservation.UpperBoundCellsPerMillilitre;

        // Assert
        Assert.Equal(9e5, lower, 1e-6);
        Assert.Equal(1.1e6, upper, 1e-6);
    }

}

public sealed class PredictionTests {

    [Fact]
    public void An_observation_within_the_combined_uncertainty_confirms_the_forecast() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Prediction prediction = new Prediction(anySpecies, madeAtHours: 10, horizonHours: 24, new Concentration(1e6), relativeUncertainty: 0.1);
        Observation anyObservation = Any.Observation()
                                        .WithSpecies(anySpecies)
                                        .WithElapsedHours(34)
                                        .WithMeasured(new Concentration(1.12e6))
                                        .WithRelativeUncertainty(0.1)
                                        .Generate();

        // Act
        bool confirmed = prediction.IsConfirmedBy(anyObservation);

        // Assert
        Assert.True(confirmed);
    }

    [Fact]
    public void An_observation_far_off_the_forecast_does_not_confirm_it() {
        // Arrange
        Species anySpecies = Any.Species().Generate();
        Prediction prediction = new Prediction(anySpecies, madeAtHours: 10, horizonHours: 24, new Concentration(1e6), relativeUncertainty: 0.1);
        Observation anyObservation = Any.Observation()
                                        .WithSpecies(anySpecies)
                                        .WithElapsedHours(34)
                                        .WithMeasured(new Concentration(2e6))
                                        .WithRelativeUncertainty(0.1)
                                        .Generate();

        // Act
        bool confirmed = prediction.IsConfirmedBy(anyObservation);

        // Assert
        Assert.False(confirmed);
    }

}
