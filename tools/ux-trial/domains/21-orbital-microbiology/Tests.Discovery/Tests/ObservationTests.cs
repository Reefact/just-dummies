using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class ObservationTests {

    private static Observation Build(double elapsed = 10, double measured = 1e6, double uncertainty = 0.2, ObservationMethod method = ObservationMethod.PlateCount) {
        return new Observation(Any.CultureId().Generate(), Any.Species().Generate(), elapsed, new Concentration(measured), uncertainty, method);
    }

    [Fact]
    public void An_observation_keeps_what_it_was_given() {
        // Arrange
        CultureId     anyCultureId = Any.CultureId().Generate();
        Species       anySpecies   = Any.Species().Generate();
        Concentration anyMeasured  = Any.Concentration().Generate();

        // Act
        Observation observation = new Observation(anyCultureId, anySpecies, 12.5, anyMeasured, 0.15, ObservationMethod.FlowCytometry);

        // Assert
        Assert.Same(anyCultureId, observation.CultureId);
        Assert.Same(anySpecies, observation.Species);
        Assert.Same(anyMeasured, observation.Measured);
        Assert.Equal(12.5, observation.ElapsedHours);
        Assert.Equal(0.15, observation.RelativeUncertainty);
        Assert.Equal(ObservationMethod.FlowCytometry, observation.Method);
    }

    [Fact]
    public void An_arbitrary_observation_is_a_well_formed_one() {
        // Arrange / Act
        Observation anyObservation = Any.Observation().Generate();

        // Assert: an optical-density reading is indirect
        if (anyObservation.Method == ObservationMethod.OpticalDensity) {
            Assert.True(anyObservation.RelativeUncertainty >= 0.1);
        }
    }

    [Fact]
    public void An_observation_without_a_culture_a_species_or_a_measure_is_refused() {
        // Arrange
        CultureId     anyCultureId = Any.CultureId().Generate();
        Species       anySpecies   = Any.Species().Generate();
        Concentration anyMeasured  = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Observation(null!, anySpecies, 1, anyMeasured, 0.1, ObservationMethod.PlateCount));
        Assert.Throws<ArgumentNullException>(() => new Observation(anyCultureId, null!, 1, anyMeasured, 0.1, ObservationMethod.PlateCount));
        Assert.Throws<ArgumentNullException>(() => new Observation(anyCultureId, anySpecies, 1, null!, 0.1, ObservationMethod.PlateCount));
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_elapsed_time_that_is_negative_or_not_finite_is_refused(double elapsed) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(elapsed: elapsed));
    }

    [Fact]
    public void An_observation_at_time_zero_is_accepted() {
        // Act
        Observation observation = Build(elapsed: 0);

        // Assert
        Assert.Equal(0, observation.ElapsedHours);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.001)]
    [InlineData(double.NaN)]
    public void An_uncertainty_outside_zero_exclusive_to_one_is_refused(double uncertainty) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(uncertainty: uncertainty));
    }

    [Fact]
    public void An_unknown_method_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(method: (ObservationMethod)99));
    }

    [Theory]
    [InlineData(ObservationMethod.PlateCount)]
    [InlineData(ObservationMethod.FlowCytometry)]
    public void A_direct_method_may_claim_a_very_small_uncertainty(ObservationMethod method) {
        // Act
        Observation observation = Build(uncertainty: 0.001, method: method);

        // Assert
        Assert.Equal(0.001, observation.RelativeUncertainty);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(0.05)]
    [InlineData(0.001)]
    public void An_optical_density_reading_cannot_claim_less_than_ten_percent(double uncertainty) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(uncertainty: uncertainty, method: ObservationMethod.OpticalDensity));

        // Assert
        Assert.Equal("relativeUncertainty", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void An_optical_density_reading_may_claim_ten_percent_or_more(double uncertainty) {
        // Act
        Observation observation = Build(uncertainty: uncertainty, method: ObservationMethod.OpticalDensity);

        // Assert
        Assert.Equal(uncertainty, observation.RelativeUncertainty);
    }

    [Theory]
    [InlineData(ObservationMethod.OpticalDensity, true)]
    [InlineData(ObservationMethod.PlateCount, false)]
    [InlineData(ObservationMethod.FlowCytometry, false)]
    public void Only_optical_density_is_indirect(ObservationMethod method, bool expected) {
        // Arrange
        Observation observation = Build(uncertainty: 0.5, method: method);

        // Act / Assert
        Assert.Equal(expected, observation.IsIndirect);
    }

    [Fact]
    public void The_bounds_lie_one_sigma_either_side_of_the_measure() {
        // Arrange
        Observation observation = Build(measured: 1e6, uncertainty: 0.2);

        // Act / Assert
        Assert.Equal(8e5, observation.LowerBoundCellsPerMillilitre, precision: 6);
        Assert.Equal(1.2e6, observation.UpperBoundCellsPerMillilitre, precision: 6);
    }

    [Fact]
    public void With_total_uncertainty_the_lower_bound_reaches_zero() {
        // Arrange
        Observation observation = Build(measured: 1e6, uncertainty: 1.0);

        // Act / Assert
        Assert.Equal(0, observation.LowerBoundCellsPerMillilitre);
        Assert.Equal(2e6, observation.UpperBoundCellsPerMillilitre);
    }

    [Fact]
    public void An_empty_measure_has_empty_bounds_whatever_the_uncertainty() {
        // Arrange
        double      anyUncertainty = Any.Double().Positive().LessThanOrEqualTo(1).Generate();
        Observation observation    = Build(measured: 0, uncertainty: anyUncertainty);

        // Act / Assert
        Assert.Equal(0, observation.LowerBoundCellsPerMillilitre);
        Assert.Equal(0, observation.UpperBoundCellsPerMillilitre);
    }

}
