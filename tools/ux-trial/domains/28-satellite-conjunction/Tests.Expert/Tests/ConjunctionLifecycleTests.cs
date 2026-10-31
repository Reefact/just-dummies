using JustDummies;
using JustDummies.Xunit;
using SatelliteConjunction.Domain;
using SatelliteConjunction.Tests.Expert.Arbitrary;

namespace SatelliteConjunction.Tests.Expert.Tests;

/// <summary>Screened → Assessed → (ManeuverPlanned) → Closed.</summary>
public sealed class ConjunctionLifecycleTests {

    private static readonly PositionCovariance HundredMetreSigma = Covariances.Isotropic(0.1);

    /// <summary>A dead-centre pass of two 10 m objects known to 100 m: Pc 1e-2, High.</summary>
    private static AnyConjunction HighRisk() {
        return Any.Conjunction()
                  .WithHardBodyRadiiMeters(10, 10)
                  .WithMissVector(Vector3.Zero)
                  .WithCovariances(HundredMetreSigma, HundredMetreSigma);
    }

    /// <summary>A 10 km pass of the same objects, known to 100 m: Pc far below 1e-5, Low.</summary>
    private static AnyConjunction LowRisk() {
        return Any.Conjunction()
                  .WithHardBodyRadiiMeters(10, 10)
                  .WithMissVector(new Vector3(0, 0, 10))
                  .WithCovariances(HundredMetreSigma, HundredMetreSigma);
    }

    /// <summary>A 1 km pass known to 1 km: Pc 7.8e-5, Elevated.</summary>
    private static AnyConjunction ElevatedRisk() {
        PositionCovariance kilometreSigma = Covariances.Isotropic(1);
        return Any.Conjunction()
                  .WithHardBodyRadiiMeters(10, 10)
                  .WithMissVector(new Vector3(1, 0, 0))
                  .WithRelativeVelocity(new Vector3(0, 7, 0))
                  .WithCovariances(kilometreSigma, kilometreSigma);
    }

    [Fact]
    public void A_first_assessment_moves_the_conjunction_to_assessed_and_records_it() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        RiskAssessment assessment = anyConjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
        Assert.Same(assessment, anyConjunction.LatestAssessment);
    }

    [Fact]
    public void Each_reassessment_is_kept_in_the_history() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        anyConjunction.Assess();
        anyConjunction.Assess();
        RiskAssessment third = anyConjunction.Assess();

        // Assert
        Assert.Equal(3, anyConjunction.History.Count);
        Assert.Same(third, anyConjunction.LatestAssessment);
        Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
    }

    [Fact]
    public void A_high_risk_calls_for_a_manoeuvre() {
        // Arrange
        Conjunction anyHighRisk = HighRisk().Generate();
        anyHighRisk.Assess();

        // Act
        anyHighRisk.PlanManeuver();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, anyHighRisk.Status);
    }

    [Fact]
    public void An_elevated_risk_does_not_warrant_a_manoeuvre() {
        // Arrange
        Conjunction anyElevatedRisk = ElevatedRisk().Generate();
        anyElevatedRisk.Assess();

        // Act
        Action planning = anyElevatedRisk.PlanManeuver;

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(planning);
        Assert.Contains("latest is Elevated", refusal.Message);
    }

    [Fact]
    public void No_manoeuvre_is_planned_before_the_risk_is_assessed() {
        // Arrange
        Conjunction anyScreened = Any.Conjunction().Generate();

        // Act
        Action planning = anyScreened.PlanManeuver;

        // Assert
        Assert.Throws<InvalidOperationException>(planning);
        Assert.Equal(ConjunctionStatus.Screened, anyScreened.Status);
    }

    [Fact]
    public void A_low_risk_conjunction_is_closed_with_its_reason() {
        // Arrange
        Conjunction anyLowRisk = LowRisk().Generate();
        anyLowRisk.Assess();

        // Act
        anyLowRisk.Close("Pc below the elevated threshold");

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, anyLowRisk.Status);
        Assert.Equal("Pc below the elevated threshold", anyLowRisk.ClosureReason);
    }

    [Fact]
    public void A_high_risk_cannot_be_closed_without_a_planned_manoeuvre() {
        // Arrange
        Conjunction anyHighRisk = HighRisk().Generate();
        anyHighRisk.Assess();
        string anyReason = Any.String().NotBlank().WithMaxLength(80).Generate();

        // Act
        Action closing = () => anyHighRisk.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(closing);
        Assert.Equal(ConjunctionStatus.Assessed, anyHighRisk.Status);
    }

    [Fact]
    public void A_high_risk_is_closed_once_its_manoeuvre_is_planned() {
        // Arrange
        Conjunction anyHighRisk = HighRisk().Generate();
        anyHighRisk.Assess();
        anyHighRisk.PlanManeuver();
        string anyReason = Any.String().NotBlank().WithMaxLength(80).Generate();

        // Act
        anyHighRisk.Close(anyReason);

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, anyHighRisk.Status);
    }

    [Fact]
    public void A_screened_conjunction_cannot_be_closed_unassessed() {
        // Arrange
        Conjunction anyScreened = Any.Conjunction().Generate();
        string      anyReason   = Any.String().NotBlank().WithMaxLength(80).Generate();

        // Act
        Action closing = () => anyScreened.Close(anyReason);

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(closing);
        Assert.Contains("assessed before", refusal.Message);
    }

    [Fact]
    public void A_closed_conjunction_is_not_assessed_again() {
        // Arrange
        Conjunction anyLowRisk = LowRisk().Generate();
        anyLowRisk.Assess();
        anyLowRisk.Close(Any.String().NotBlank().WithMaxLength(80).Generate());

        // Act
        Func<RiskAssessment> reassessing = anyLowRisk.Assess;

        // Assert
        Assert.Throws<InvalidOperationException>(reassessing);
        Assert.Single(anyLowRisk.History);
    }

}
