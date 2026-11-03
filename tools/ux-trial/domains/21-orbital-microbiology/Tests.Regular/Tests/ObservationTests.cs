using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class ObservationTests {

    private static Observation Build(double elapsedHours, double relativeUncertainty, ObservationMethod method) {
        return new Observation(Any.CultureId().Generate(),
                               Any.Species().Generate(),
                               elapsedHours,
                               Any.Concentration().Generate(),
                               relativeUncertainty,
                               method);
    }

    [Fact]
    public void An_observation_keeps_what_it_was_given() {
        // Arrange
        CultureId     anyCultureId = Any.CultureId().Generate();
        Species       anySpecies   = Any.Species().Generate();
        Concentration measured     = new Concentration(2.5e6);

        // Act
        Observation observation = new Observation(anyCultureId, anySpecies, 12.5, measured, 0.15, ObservationMethod.FlowCytometry);

        // Assert
        Assert.Same(anyCultureId, observation.CultureId);
        Assert.Same(anySpecies, observation.Species);
        Assert.Equal(12.5, observation.ElapsedHours);
        Assert.Same(measured, observation.Measured);
        Assert.Equal(0.15, observation.RelativeUncertainty);
        Assert.Equal(ObservationMethod.FlowCytometry, observation.Method);
    }

    [Fact]
    public void Bounds_lie_one_sigma_on_either_side_of_the_measure() {
        // Arrange
        Observation observation = Any.Observation()
                                     .WithMeasured(new Concentration(1e6))
                                     .WithRelativeUncertainty(0.2)
                                     .Generate();

        // Assert
        Assert.Equal(8e5, observation.LowerBoundCellsPerMillilitre, precision: 6);
        Assert.Equal(1.2e6, observation.UpperBoundCellsPerMillilitre, precision: 6);
    }

    [Fact]
    public void An_observation_taken_at_the_start_is_accepted() {
        // Act
        Observation observation = Build(0, 0.5, ObservationMethod.PlateCount);

        // Assert
        Assert.Equal(0, observation.ElapsedHours);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_observation_cannot_be_taken_before_the_start(double elapsedHours) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(elapsedHours, 0.5, ObservationMethod.PlateCount));

        // Assert
        Assert.Equal("elapsedHours", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.001)]
    [InlineData(double.NaN)]
    public void An_uncertainty_outside_zero_exclusive_to_one_is_refused(double relativeUncertainty) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(1, relativeUncertainty, ObservationMethod.PlateCount));

        // Assert
        Assert.Equal("relativeUncertainty", refusal.ParamName);
    }

    [Theory]
    [InlineData(ObservationMethod.PlateCount, 0.0001)]
    [InlineData(ObservationMethod.FlowCytometry, 0.05)]
    [InlineData(ObservationMethod.PlateCount, 1)]
    public void A_direct_method_may_claim_a_fine_uncertainty(ObservationMethod method, double relativeUncertainty) {
        // Act
        Observation observation = Build(1, relativeUncertainty, method);

        // Assert
        Assert.Equal(relativeUncertainty, observation.RelativeUncertainty);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(0.05)]
    [InlineData(0.0001)]
    public void An_optical_density_reading_cannot_claim_less_than_ten_percent(double relativeUncertainty) {
        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => Build(1, relativeUncertainty, ObservationMethod.OpticalDensity));

        // Assert
        Assert.Equal("relativeUncertainty", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void An_optical_density_reading_may_claim_ten_percent_or_worse(double relativeUncertainty) {
        // Act
        Observation observation = Build(1, relativeUncertainty, ObservationMethod.OpticalDensity);

        // Assert
        Assert.Equal(relativeUncertainty, observation.RelativeUncertainty);
    }

    [Theory]
    [InlineData((ObservationMethod)3)]
    [InlineData((ObservationMethod)(-1))]
    public void An_unknown_method_is_refused(ObservationMethod method) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(1, 0.5, method));

        // Assert
        Assert.Equal("method", refusal.ParamName);
    }

    [Theory]
    [InlineData(ObservationMethod.OpticalDensity, true)]
    [InlineData(ObservationMethod.PlateCount, false)]
    [InlineData(ObservationMethod.FlowCytometry, false)]
    public void Only_optical_density_is_indirect(ObservationMethod method, bool expectedIndirect) {
        // Arrange
        Observation observation = Any.Observation().WithMethod(method).Generate();

        // Assert
        Assert.Equal(expectedIndirect, observation.IsIndirect);
    }

    [Fact]
    public void An_observation_needs_an_id_a_species_and_a_measure() {
        // Arrange
        CultureId     anyCultureId = Any.CultureId().Generate();
        Species       anySpecies   = Any.Species().Generate();
        Concentration anyMeasured  = Any.Concentration().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Observation(null!, anySpecies, 1, anyMeasured, 0.5, ObservationMethod.PlateCount));
        Assert.Throws<ArgumentNullException>(() => new Observation(anyCultureId, null!, 1, anyMeasured, 0.5, ObservationMethod.PlateCount));
        Assert.Throws<ArgumentNullException>(() => new Observation(anyCultureId, anySpecies, 1, null!, 0.5, ObservationMethod.PlateCount));
    }

}
