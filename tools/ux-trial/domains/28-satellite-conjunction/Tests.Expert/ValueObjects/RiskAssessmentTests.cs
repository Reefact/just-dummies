using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.ValueObjects;

public sealed class RiskAssessmentTests {

    // Twice the 12 km/s of the fastest orbital state: two objects meeting head-on.
    private const double FastestRelativeSpeed = 24;

    [Fact]
    public void An_assessment_exposes_what_it_was_built_from() {
        // Arrange
        Epoch                tca         = Any.Epoch().Generate();
        CollisionProbability probability = new(3.2e-5);

        // Act
        RiskAssessment assessment = new(tca, 12.5, 14.75, 0.8, probability);

        // Assert
        Assert.Same(tca, assessment.TimeOfClosestApproach);
        Assert.Equal(12.5, assessment.MissDistanceKm);
        Assert.Equal(14.75, assessment.RelativeSpeedKmPerSecond);
        Assert.Equal(0.8, assessment.CombinedSigmaKm);
        Assert.Same(probability, assessment.Probability);
    }

    [Theory]
    [InlineData(5e-4, RiskLevel.High)]
    [InlineData(5e-5, RiskLevel.Elevated)]
    [InlineData(5e-6, RiskLevel.Low)]
    public void The_level_of_an_assessment_is_that_of_its_probability(double value, RiskLevel expected) {
        // Arrange
        RiskAssessment assessment = Valid(probability: new CollisionProbability(value));

        // Act & Assert
        Assert.Equal(expected, assessment.Level);
    }

    [Fact]
    public void A_zero_miss_distance_is_a_direct_hit_and_is_accepted() {
        // Act
        RiskAssessment assessment = Valid(missDistanceKm: 0);

        // Assert
        Assert.Equal(0, assessment.MissDistanceKm);
    }

    [Fact]
    public void A_miss_distance_just_below_zero_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Valid(missDistanceKm: Math.BitDecrement(0)));

        // Assert
        Assert.Equal("missDistanceKm", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_miss_distance_that_is_not_finite_is_refused(double nonFinite) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Valid(missDistanceKm: nonFinite));

        // Assert
        Assert.Equal("missDistanceKm", refusal.ParamName);
    }

    [Fact]
    public void The_fastest_relative_speed_is_twice_the_fastest_orbital_speed_and_is_accepted() {
        // Act
        RiskAssessment assessment = Valid(relativeSpeedKmPerSecond: FastestRelativeSpeed);

        // Assert
        Assert.Equal(FastestRelativeSpeed, assessment.RelativeSpeedKmPerSecond);
    }

    [Fact]
    public void A_relative_speed_just_above_twice_the_fastest_orbital_speed_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Valid(relativeSpeedKmPerSecond: Math.BitIncrement(FastestRelativeSpeed)));

        // Assert
        Assert.Equal("relativeSpeedKmPerSecond", refusal.ParamName);
    }

    [Fact]
    public void A_relative_speed_of_zero_is_refused_because_co_moving_objects_have_no_encounter() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Valid(relativeSpeedKmPerSecond: 0));

        // Assert
        Assert.Equal("relativeSpeedKmPerSecond", refusal.ParamName);
    }

    [Fact]
    public void A_negative_relative_speed_is_refused() {
        // Arrange
        double anyNegativeSpeed = Any.Double().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Valid(relativeSpeedKmPerSecond: anyNegativeSpeed));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_relative_speed_that_is_not_finite_is_refused(double nonFinite) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Valid(relativeSpeedKmPerSecond: nonFinite));
    }

    [Fact]
    public void A_combined_sigma_of_zero_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Valid(combinedSigmaKm: 0));

        // Assert
        Assert.Equal("combinedSigmaKm", refusal.ParamName);
    }

    [Fact]
    public void A_negative_combined_sigma_is_refused() {
        // Arrange
        double anyNegativeSigma = Any.Double().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Valid(combinedSigmaKm: anyNegativeSigma));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_combined_sigma_that_is_not_finite_is_refused(double nonFinite) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Valid(combinedSigmaKm: nonFinite));
    }

    [Fact]
    public void An_assessment_without_a_time_of_closest_approach_is_refused() {
        // Arrange
        CollisionProbability anyProbability = Any.CollisionProbability().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RiskAssessment(null!, 1, 1, 1, anyProbability));
    }

    [Fact]
    public void An_assessment_without_a_probability_is_refused() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RiskAssessment(anyEpoch, 1, 1, 1, null!));
    }

    // Each refusal above overrides ONE argument; the others are dummies that the domain accepts.
    private static RiskAssessment Valid(double? missDistanceKm = null, double? relativeSpeedKmPerSecond = null, double? combinedSigmaKm = null, CollisionProbability? probability = null) {
        return new RiskAssessment(
            Any.Epoch().Generate(),
            missDistanceKm ?? Any.Double().Between(0, 10_000).Generate(),
            relativeSpeedKmPerSecond ?? Any.Double().Between(0.1, FastestRelativeSpeed).Generate(),
            combinedSigmaKm ?? Any.Double().Between(0.001, 1_000).Generate(),
            probability ?? Any.CollisionProbability().Generate());
    }

}
