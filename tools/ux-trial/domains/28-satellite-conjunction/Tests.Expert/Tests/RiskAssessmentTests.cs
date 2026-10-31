using JustDummies;
using SatelliteConjunction.Domain;
using SatelliteConjunction.Tests.Expert.Arbitrary;

namespace SatelliteConjunction.Tests.Expert.Tests;

/// <summary>
///     The 2D Gaussian collision probability in the encounter plane. Every value a risk depends on —
///     miss vector, relative velocity, covariances, hard-body radii — is a literal here; where and when the
///     encounter happens, and between which objects, is drawn.
/// </summary>
public sealed class RiskAssessmentTests {

    /// <summary>Two 10 m objects crossing at 7 km/s, the secondary passing 1 km from the primary.</summary>
    private static AnyConjunction OneKilometreCrossing() {
        return Any.Conjunction()
                  .WithHardBodyRadiiMeters(10, 10)
                  .WithMissVector(new Vector3(1, 0, 0))
                  .WithRelativeVelocity(new Vector3(0, 7, 0));
    }

    [Fact]
    public void The_miss_distance_and_relative_speed_come_from_the_states_at_closest_approach() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction()
                                        .WithMissVector(new Vector3(3, 4, 0))
                                        .WithRelativeVelocity(new Vector3(0, 0, 7))
                                        .Generate();

        // Act
        RiskAssessment assessment = anyConjunction.Assess();

        // Assert
        Assert.Equal(5, assessment.MissDistanceKm, tolerance: 1e-6);
        Assert.Equal(7, assessment.RelativeSpeedKmPerSecond, tolerance: 1e-9);
        Assert.Equal(anyConjunction.TimeOfClosestApproach, assessment.TimeOfClosestApproach);
    }

    [Fact]
    public void A_reference_encounter_gives_the_textbook_probability() {
        // Arrange: combined variance 0.5 km² per axis, so Pc = R² / (2 · 0.5) · exp(-1 / (2 · 0.5)) = 4e-4 · e⁻¹.
        Conjunction anyConjunction = OneKilometreCrossing().WithCovariances(Covariances.Isotropic(0.5), Covariances.Isotropic(0.5))
                                                           .Generate();

        // Act
        RiskAssessment assessment = anyConjunction.Assess();

        // Assert
        Assert.Equal(1.4715e-4, assessment.Probability.Value, tolerance: 1e-8);
        Assert.Equal(Math.Sqrt(0.5), assessment.CombinedSigmaKm, tolerance: 1e-9);   // the root of the mean combined variance
    }

    [Theory]
    [InlineData(0.1, RiskLevel.Low)]       // tight: 1 km is ten sigmas away, the objects cannot meet
    [InlineData(0.5, RiskLevel.High)]      // the miss lies within the dispersion
    [InlineData(1.0, RiskLevel.Elevated)]  // the probability starts to dilute
    [InlineData(5.0, RiskLevel.Low)]       // so dispersed that the cross-section is lost in it
    public void The_same_miss_distance_carries_a_different_risk_depending_on_the_dispersion(double sigmaKm, RiskLevel expected) {
        // Arrange
        Conjunction anyConjunction = OneKilometreCrossing().WithCovariances(Covariances.Isotropic(sigmaKm), Covariances.Isotropic(sigmaKm))
                                                           .Generate();

        // Act
        RiskAssessment assessment = anyConjunction.Assess();

        // Assert
        Assert.Equal(expected, assessment.Level);
    }

    [Fact]
    public void Larger_objects_present_a_larger_cross_section_and_a_higher_probability() {
        // Arrange
        PositionCovariance kilometreSigma = Covariances.Isotropic(1);
        Conjunction anySmallPair = OneKilometreCrossing().WithCovariances(kilometreSigma, kilometreSigma).Generate();
        Conjunction anyLargePair = OneKilometreCrossing().WithCovariances(kilometreSigma, kilometreSigma).WithHardBodyRadiiMeters(20, 20).Generate();

        // Act
        double small = anySmallPair.Assess().Probability.Value;
        double large = anyLargePair.Assess().Probability.Value;

        // Assert: Pc scales with the square of the combined radius.
        Assert.Equal(4, large / small, tolerance: 1e-6);
    }

    [Fact]
    public void Co_moving_objects_have_no_encounter_plane() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().WithRelativeVelocity(Vector3.Zero).Generate();

        // Act
        Func<RiskAssessment> assessing = anyConjunction.Assess;

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(assessing);
        Assert.Contains("co-moving", refusal.Message);
    }

}
