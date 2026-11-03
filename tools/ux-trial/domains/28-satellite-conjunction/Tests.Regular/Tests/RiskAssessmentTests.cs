using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class RiskAssessmentTests {

    [Fact]
    public void An_assessment_keeps_what_it_was_built_from() {
        // Arrange
        Epoch                anyEpoch       = Any.Epoch().Generate();
        double               anyMiss        = Any.Double().Between(0, 1_000).Generate();
        double               anySpeed       = Any.Double().Between(0.1, 24).Generate();
        double               anySigma       = Any.Double().Between(0.01, 1_000).Generate();
        CollisionProbability anyProbability = Any.CollisionProbability().Generate();

        // Act
        RiskAssessment assessment = new(anyEpoch, anyMiss, anySpeed, anySigma, anyProbability);

        // Assert
        Assert.Same(anyEpoch, assessment.TimeOfClosestApproach);
        Assert.Equal(anyMiss, assessment.MissDistanceKm);
        Assert.Equal(anySpeed, assessment.RelativeSpeedKmPerSecond);
        Assert.Equal(anySigma, assessment.CombinedSigmaKm);
        Assert.Same(anyProbability, assessment.Probability);
    }

    [Fact]
    public void The_level_of_an_assessment_is_the_level_of_its_probability() {
        // Arrange
        CollisionProbability anyHighProbability = Any.CollisionProbability().WithValue(v => v.GreaterThanOrEqualTo(CollisionProbability.HighThreshold)).Generate();

        // Act
        RiskAssessment assessment = Any.RiskAssessment().WithProbability(anyHighProbability).Generate();

        // Assert
        Assert.Equal(RiskLevel.High, assessment.Level);
    }

    [Fact]
    public void A_missing_epoch_is_refused() {
        // Arrange
        CollisionProbability anyProbability = Any.CollisionProbability().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new RiskAssessment(null!, 1, 1, 1, anyProbability));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_missing_probability_is_refused() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new RiskAssessment(anyEpoch, 1, 1, 1, null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_negative_miss_distance_is_refused() {
        // Arrange
        double anyNegativeMiss = Any.Double().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.RiskAssessment().WithMissDistanceKm(anyNegativeMiss).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_miss_distance_of_zero_is_accepted() {
        // Arrange / Act
        RiskAssessment assessment = Any.RiskAssessment().WithMissDistanceKm(0).Generate();

        // Assert
        Assert.Equal(0d, assessment.MissDistanceKm);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_miss_distance_that_is_not_finite_is_refused(double miss) {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => Any.RiskAssessment().WithMissDistanceKm(miss).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_relative_speed_of_zero_or_less_is_refused() {
        // Arrange
        double anyNonPositiveSpeed = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.RiskAssessment().WithRelativeSpeedKmPerSecond(anyNonPositiveSpeed).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_relative_speed_above_twice_the_orbital_maximum_is_refused() {
        // Arrange
        double anyExcessiveSpeed = Any.Double().GreaterThan(24).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.RiskAssessment().WithRelativeSpeedKmPerSecond(anyExcessiveSpeed).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_relative_speed_of_exactly_twice_the_orbital_maximum_is_accepted() {
        // Arrange / Act
        RiskAssessment assessment = Any.RiskAssessment().WithRelativeSpeedKmPerSecond(24).Generate();

        // Assert
        Assert.Equal(24d, assessment.RelativeSpeedKmPerSecond);
    }

    [Fact]
    public void A_combined_sigma_of_zero_or_less_is_refused() {
        // Arrange
        double anyNonPositiveSigma = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.RiskAssessment().WithCombinedSigmaKm(anyNonPositiveSigma).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_combined_sigma_that_is_not_finite_is_refused() {
        // Arrange
        double anyUnboundedSigma = Any.OneOf(double.NaN, double.PositiveInfinity).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.RiskAssessment().WithCombinedSigmaKm(anyUnboundedSigma).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
