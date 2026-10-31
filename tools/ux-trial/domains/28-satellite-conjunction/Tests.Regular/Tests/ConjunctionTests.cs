using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Regular.Tests;

public sealed class ConjunctionScreeningTests {

    [Fact]
    public void A_new_conjunction_starts_screened() {
        // Arrange
        AnyConjunction anyConjunction = Arbitrary.Conjunction();

        // Act
        Conjunction conjunction = anyConjunction.Generate();

        // Assert
        Assert.Equal(ConjunctionStatus.Screened, conjunction.Status);
        Assert.Null(conjunction.LatestAssessment);
    }

    [Fact]
    public void The_primary_of_a_conjunction_must_be_an_active_satellite() {
        // Arrange
        AnyConjunction anyConjunction = Arbitrary.Conjunction().WithPrimary(t => t.WithSpaceObject(o => o.WithIsActive(false)));

        // Act
        Action act = () => anyConjunction.Generate();

        // Assert
        Assert.Throws<ArgumentException>("primary", act);
    }

    [Fact]
    public void An_object_cannot_be_in_conjunction_with_itself() {
        // Arrange
        int            anyCatalogNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber).Generate();
        AnyConjunction anyConjunction   = Arbitrary.Conjunction()
            // Refining the primary's object restarts from AnyTrajectory's base, which drops the
            // "active payload" rule AnyConjunction's base had set: it has to be restated here.
            .WithPrimary(t => t.WithSpaceObject(o => o.WithKind(ObjectKind.Payload).WithIsActive(true).WithCatalogNumber(anyCatalogNumber)))
            .WithSecondary(t => t.WithSpaceObject(o => o.WithCatalogNumber(anyCatalogNumber)));

        // Act
        Action act = () => anyConjunction.Generate();

        // Assert
        Assert.Throws<ArgumentException>("secondary", act);
    }

    [Fact]
    public void Both_trajectories_must_cover_the_time_of_closest_approach() {
        // Arrange
        Trajectory anySecondary = Arbitrary.Trajectory().Generate();
        Epoch      afterItsEnd  = anySecondary.End.Add(TimeSpan.FromMinutes(1));

        // Act
        Action act = () => Arbitrary.Conjunction().WithSecondary(anySecondary).WithTimeOfClosestApproach(afterItsEnd).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("timeOfClosestApproach", act);
    }

}

public sealed class ConjunctionAssessmentTests {

    [Fact]
    public void Assessing_measures_the_miss_distance_and_the_relative_speed_at_closest_approach() {
        // Arrange
        Conjunction conjunction = Encounters.Crossing(missDistanceKm: 1.5, sigmaKm: 0.1);

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(1.5, assessment.MissDistanceKm, 9);
        Assert.Equal(7.5 * Math.Sqrt(2), assessment.RelativeSpeedKmPerSecond, 9);
    }

    [Fact]
    public void A_direct_hit_with_a_tight_covariance_is_a_high_risk() {
        // Arrange
        Conjunction conjunction = Encounters.HighRisk();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        // R = 10 m, combined sigma 141 m per axis: Pc = R² / (2 · 0.02 km²) = 2.5e-3.
        Assert.Equal(2.5e-3, assessment.Probability.Value, 9);
        Assert.Equal(RiskLevel.High, assessment.Level);
    }

    [Fact]
    public void The_same_miss_distance_carries_more_risk_when_the_positions_are_more_dispersed() {
        // Arrange
        Conjunction wellTracked   = Encounters.Crossing(missDistanceKm: 1, sigmaKm: 0.1);
        Conjunction poorlyTracked = Encounters.Crossing(missDistanceKm: 1, sigmaKm: 1);

        // Act
        RiskAssessment wellTrackedRisk   = wellTracked.Assess();
        RiskAssessment poorlyTrackedRisk = poorlyTracked.Assess();

        // Assert
        Assert.Equal(RiskLevel.Low, wellTrackedRisk.Level);
        Assert.Equal(RiskLevel.Elevated, poorlyTrackedRisk.Level);
    }

    [Fact]
    public void Assessing_moves_a_screened_conjunction_to_assessed_and_records_the_assessment() {
        // Arrange
        Conjunction conjunction = Encounters.LowRisk();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
        Assert.Same(assessment, Assert.Single(conjunction.History));
    }

    [Fact]
    public void A_conjunction_can_be_reassessed_and_the_latest_assessment_is_the_last_one() {
        // Arrange
        Conjunction conjunction = Encounters.LowRisk();
        conjunction.Assess();

        // Act
        RiskAssessment second = conjunction.Assess();

        // Assert
        Assert.Equal(2, conjunction.History.Count);
        Assert.Same(second, conjunction.LatestAssessment);
    }

    [Fact]
    public void Co_moving_objects_cannot_be_assessed_in_an_encounter_plane() {
        // Arrange
        Conjunction conjunction = Encounters.Crossing(missDistanceKm: 1, sigmaKm: 0.1, secondaryVelocity: Encounters.PrimaryVelocity);

        // Act
        Action act = () => conjunction.Assess();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

}

public sealed class ConjunctionLifecycleTests {

    [Fact]
    public void A_high_risk_conjunction_gets_a_manoeuvre_planned() {
        // Arrange
        Conjunction conjunction = Encounters.HighRisk();
        conjunction.Assess();

        // Act
        conjunction.PlanManeuver();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, conjunction.Status);
    }

    [Fact]
    public void A_manoeuvre_is_not_planned_for_a_low_risk() {
        // Arrange
        Conjunction conjunction = Encounters.LowRisk();
        conjunction.Assess();

        // Act
        Action act = () => conjunction.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_manoeuvre_is_not_planned_before_any_assessment() {
        // Arrange
        Conjunction anyConjunction = Arbitrary.Conjunction().Generate();

        // Act
        Action act = () => anyConjunction.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_screened_conjunction_cannot_be_closed() {
        // Arrange
        Conjunction anyConjunction = Arbitrary.Conjunction().Generate();
        string      anyReason      = Any.String().NotBlank().WithMaxLength(80).Generate();

        // Act
        Action act = () => anyConjunction.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_low_risk_conjunction_is_closed_with_its_reason() {
        // Arrange
        Conjunction conjunction = Encounters.LowRisk();
        conjunction.Assess();

        // Act
        conjunction.Close("Miss distance comfortably above the screening volume");

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
        Assert.Equal("Miss distance comfortably above the screening volume", conjunction.ClosureReason);
    }

    [Fact]
    public void A_high_risk_conjunction_cannot_be_closed_without_a_planned_manoeuvre() {
        // Arrange
        Conjunction conjunction = Encounters.HighRisk();
        conjunction.Assess();
        string anyReason = Any.String().NotBlank().WithMaxLength(80).Generate();

        // Act
        Action act = () => conjunction.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_high_risk_conjunction_can_be_closed_once_a_manoeuvre_is_planned() {
        // Arrange
        Conjunction conjunction = Encounters.HighRisk();
        conjunction.Assess();
        conjunction.PlanManeuver();
        string anyReason = Any.String().NotBlank().WithMaxLength(80).Generate();

        // Act
        conjunction.Close(anyReason);

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
    }

    [Fact]
    public void A_closed_conjunction_cannot_be_assessed_again() {
        // Arrange
        Conjunction conjunction = Encounters.LowRisk();
        conjunction.Assess();
        conjunction.Close(Any.String().NotBlank().WithMaxLength(80).Generate());

        // Act
        Action act = () => conjunction.Assess();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

}
