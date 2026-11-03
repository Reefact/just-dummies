using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class RiskAssessmentTests {

    [Fact]
    public void An_assessment_keeps_what_it_is_built_from() {
        // Arrange
        Epoch                anyEpoch    = Any.Epoch().Generate();
        CollisionProbability probability = new CollisionProbability(3e-5);

        // Act
        RiskAssessment assessment = new RiskAssessment(anyEpoch, 0.48, 15, 0.14, probability);

        // Assert
        Assert.Same(anyEpoch, assessment.TimeOfClosestApproach);
        Assert.Equal(0.48, assessment.MissDistanceKm);
        Assert.Equal(15, assessment.RelativeSpeedKmPerSecond);
        Assert.Equal(0.14, assessment.CombinedSigmaKm);
        Assert.Same(probability, assessment.Probability);
    }

    [Theory]
    [InlineData(1e-6, RiskLevel.Low)]
    [InlineData(5e-5, RiskLevel.Elevated)]
    [InlineData(2e-4, RiskLevel.High)]
    public void An_assessment_takes_the_level_of_its_probability(double value, RiskLevel expected) {
        // Arrange
        RiskAssessment assessment = Any.RiskAssessment().WithProbability(new CollisionProbability(value)).Generate();

        // Act / Assert
        Assert.Equal(expected, assessment.Level);
    }

    [Fact]
    public void An_assessment_refuses_a_missing_time_of_closest_approach() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.RiskAssessment().WithTimeOfClosestApproach((Epoch)null!).Generate());

        // Assert
        Assert.Equal("timeOfClosestApproach", refusal.ParamName);
    }

    [Fact]
    public void An_assessment_refuses_a_missing_probability() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.RiskAssessment().WithProbability((CollisionProbability)null!).Generate());

        // Assert
        Assert.Equal("probability", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_assessment_refuses_a_miss_distance_that_is_negative_or_not_finite(double missDistance) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.RiskAssessment().WithMissDistanceKm(missDistance).Generate());

        // Assert
        Assert.Equal("missDistanceKm", refusal.ParamName);
    }

    [Fact]
    public void An_assessment_accepts_a_zero_miss_distance() {
        // Act
        RiskAssessment assessment = Any.RiskAssessment().WithMissDistanceKm(0).Generate();

        // Assert
        Assert.Equal(0, assessment.MissDistanceKm);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(24.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_assessment_refuses_a_relative_speed_outside_zero_to_twenty_four(double speed) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.RiskAssessment().WithRelativeSpeedKmPerSecond(speed).Generate());

        // Assert
        Assert.Equal("relativeSpeedKmPerSecond", refusal.ParamName);
    }

    [Theory]
    [InlineData(1e-9)]
    [InlineData(24.0)]
    public void An_assessment_accepts_the_edges_of_the_relative_speed_range(double speed) {
        // Act
        RiskAssessment assessment = Any.RiskAssessment().WithRelativeSpeedKmPerSecond(speed).Generate();

        // Assert
        Assert.Equal(speed, assessment.RelativeSpeedKmPerSecond);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_assessment_refuses_a_combined_sigma_that_is_not_positive_and_finite(double sigma) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.RiskAssessment().WithCombinedSigmaKm(sigma).Generate());

        // Assert
        Assert.Equal("combinedSigmaKm", refusal.ParamName);
    }

}
