using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class ObservationTests {

    [Fact]
    public void An_observation_keeps_what_it_was_given() {
        // Arrange
        CultureId     anyCultureId = Any.CultureId().Generate();
        Species       anySpecies   = Any.Species().Generate();
        Concentration measured     = new Concentration(2.5e6);

        // Act
        Observation observation = new Observation(anyCultureId, anySpecies, 12.5, measured, 0.2, ObservationMethod.FlowCytometry);

        // Assert
        Assert.Same(anyCultureId, observation.CultureId);
        Assert.Same(anySpecies, observation.Species);
        Assert.Equal(12.5, observation.ElapsedHours);
        Assert.Same(measured, observation.Measured);
        Assert.Equal(0.2, observation.RelativeUncertainty);
        Assert.Equal(ObservationMethod.FlowCytometry, observation.Method);
    }

    [Fact]
    public void The_one_sigma_bounds_are_the_measure_less_and_plus_its_relative_uncertainty() {
        // Arrange
        Observation observation = Any.Observation()
                                     .WithMeasured(new Concentration(1e6))
                                     .WithRelativeUncertainty(0.25)
                                     .Generate();

        // Assert
        Assert.Equal(7.5e5, observation.LowerBoundCellsPerMillilitre, precision: 3);
        Assert.Equal(1.25e6, observation.UpperBoundCellsPerMillilitre, precision: 3);
    }

    [Theory]
    [InlineData(ObservationMethod.PlateCount, false)]
    [InlineData(ObservationMethod.FlowCytometry, false)]
    [InlineData(ObservationMethod.OpticalDensity, true)]
    public void Only_an_optical_density_reading_is_indirect(ObservationMethod method, bool expectedIndirect) {
        // Arrange
        Observation observation = Any.Observation().WithMethod(method).Generate();

        // Assert
        Assert.Equal(expectedIndirect, observation.IsIndirect);
    }

    // ---- the indirect-method rule ----------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0999)]
    [InlineData(0.05)]
    [InlineData(0.001)]
    public void An_optical_density_reading_cannot_claim_less_than_ten_percent_uncertainty(double relativeUncertainty) {
        // Arrange
        AnyObservation anyObservation = Any.Observation().WithMethod(ObservationMethod.OpticalDensity);

        // Assert
        Assert.Throws<ArgumentException>(() => anyObservation.WithRelativeUncertainty(relativeUncertainty).Generate());
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1d)]
    public void An_optical_density_reading_may_claim_ten_percent_uncertainty_or_more(double relativeUncertainty) {
        // Act
        Observation observation = Any.Observation().WithMethod(ObservationMethod.OpticalDensity).WithRelativeUncertainty(relativeUncertainty).Generate();

        // Assert
        Assert.Equal(relativeUncertainty, observation.RelativeUncertainty);
    }

    [Theory]
    [InlineData(ObservationMethod.PlateCount)]
    [InlineData(ObservationMethod.FlowCytometry)]
    public void A_direct_method_may_claim_less_than_ten_percent_uncertainty(ObservationMethod method) {
        // Act
        Observation observation = Any.Observation().WithMethod(method).WithRelativeUncertainty(0.01).Generate();

        // Assert
        Assert.Equal(0.01, observation.RelativeUncertainty);
    }

    // ---- refusals --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void An_elapsed_time_that_is_negative_or_not_finite_is_refused(double elapsedHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Observation().WithElapsedHours(elapsedHours).Generate());
    }

    [Fact]
    public void An_observation_may_be_taken_at_time_zero() {
        Assert.Equal(0d, Any.Observation().WithElapsedHours(0d).Generate().ElapsedHours);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.1)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    public void A_relative_uncertainty_outside_zero_exclusive_to_one_is_refused(double relativeUncertainty) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Observation().WithRelativeUncertainty(relativeUncertainty).Generate());
    }

    [Fact]
    public void A_method_the_enumeration_does_not_declare_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Observation().WithMethod((ObservationMethod)42).Generate());
    }

    [Fact]
    public void An_observation_needs_a_culture_a_species_and_a_measure() {
        Assert.Throws<ArgumentNullException>(() => Any.Observation().WithCultureId((CultureId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Observation().WithSpecies((Species)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Observation().WithMeasured((Concentration)null!).Generate());
    }

}
