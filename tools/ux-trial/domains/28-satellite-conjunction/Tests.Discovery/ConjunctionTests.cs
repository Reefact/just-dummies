using System;

using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class ConjunctionTests {

    // A 1 km miss across the encounter plane. With 10 m hard bodies on both sides:
    //   dispersed: each object 0.25 km² per axis → combined σ² = 0.5 → Pc ≈ 1.5e-4 (High)
    //   tight:     each object 0.005 km² per axis → combined σ² = 0.01 → Pc ≈ 1e-22 (Low)
    private static readonly PositionCovariance Dispersed = Encounter.Isotropic(0.25);
    private static readonly PositionCovariance Tight     = Encounter.Isotropic(0.005);

    [Fact]
    public void A_new_conjunction_is_screened_and_not_yet_assessed() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        ConjunctionStatus status = anyConjunction.Status;

        // Assert
        Assert.Equal(ConjunctionStatus.Screened, status);
        Assert.Null(anyConjunction.LatestAssessment);
    }

    [Fact]
    public void An_inert_primary_is_refused() {
        // Arrange
        Epoch      anyTimeOfClosestApproach = Any.Epoch().Generate();
        Trajectory anyDebris                = Any.Trajectory().WithSpaceObject(o => o.WithKind(ObjectKind.Debris)).Covering(anyTimeOfClosestApproach).Generate();
        Trajectory anySecondary             = Any.Trajectory().Covering(anyTimeOfClosestApproach).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.Conjunction().WithPrimary(anyDebris).WithSecondary(anySecondary).WithTimeOfClosestApproach(anyTimeOfClosestApproach).Generate());

        // Assert
        Assert.Equal("primary", refusal.ParamName);
    }

    [Fact]
    public void A_satellite_cannot_be_in_conjunction_with_itself() {
        // Arrange
        Epoch       anyTimeOfClosestApproach = Any.Epoch().Generate();
        SpaceObject anySatellite             = Any.SpaceObject().Active().Generate();
        Trajectory  anyTrajectory            = Any.Trajectory().WithSpaceObject(anySatellite).Covering(anyTimeOfClosestApproach).Generate();
        Trajectory  anyOtherTrajectory       = Any.Trajectory().WithSpaceObject(anySatellite).Covering(anyTimeOfClosestApproach).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.Conjunction().WithPrimary(anyTrajectory).WithSecondary(anyOtherTrajectory).WithTimeOfClosestApproach(anyTimeOfClosestApproach).Generate());

        // Assert
        Assert.Equal("secondary", refusal.ParamName);
    }

    [Fact]
    public void A_time_of_closest_approach_outside_the_secondary_trajectory_is_refused() {
        // Arrange
        Epoch      anyTimeOfClosestApproach = Any.Epoch().Generate();
        Trajectory anyPrimary               = Any.Trajectory().WithSpaceObject(o => o.Active()).Covering(anyTimeOfClosestApproach).Generate();
        Trajectory anyStaleSecondary        = Any.Trajectory().Covering(anyTimeOfClosestApproach.Add(TimeSpan.FromHours(-6))).Generate();

        // Act
        Action creation = () => Any.Conjunction().WithPrimary(anyPrimary).WithSecondary(anyStaleSecondary).WithTimeOfClosestApproach(anyTimeOfClosestApproach).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(creation);
    }

    [Fact]
    public void Assessing_reports_the_miss_distance_and_relative_speed_at_closest_approach() {
        // Arrange
        // 3 km apart along x and 4 km along z: 5 km. Velocities cross at right angles: 7.5·√2 km/s.
        // The covariances only weigh on the probability, not on the geometry: they are dummies here.
        // WORKAROUND (domain defect, see NOTES.discovery.md): Combine() re-applies the single-object cap of
        // 1000² km² to the sum, so two valid covariances can make Assess() throw. Each variance is kept at
        // most half the cap until the domain is fixed; this is not an invariant of a covariance.
        Func<AnyPositionCovariance, AnyPositionCovariance> halfTheCap = c => c.WithXx(v => v.LessThanOrEqualTo(500_000))
                                                                              .WithYy(v => v.LessThanOrEqualTo(500_000))
                                                                              .WithZz(v => v.LessThanOrEqualTo(500_000));
        PositionCovariance anyPrimaryCovariance   = halfTheCap(Any.PositionCovariance()).Generate();
        PositionCovariance anySecondaryCovariance = halfTheCap(Any.PositionCovariance()).Generate();
        Conjunction conjunction = new Encounter()
                                  .PrimaryAt(new Vector3(7000, 0, 0), new Vector3(0, 7.5, 0))
                                  .SecondaryAt(new Vector3(7003, 0, 4), new Vector3(0, 0, 7.5))
                                  .WithCovariances(anyPrimaryCovariance, anySecondaryCovariance)
                                  .Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(5, assessment.MissDistanceKm, precision: 9);
        Assert.Equal(7.5 * Math.Sqrt(2), assessment.RelativeSpeedKmPerSecond, precision: 9);
    }

    [Fact]
    public void The_same_miss_distance_is_high_risk_with_a_dispersed_covariance_and_low_risk_with_a_tight_one() {
        // Arrange
        Conjunction dispersed = new Encounter().WithCovariances(Dispersed, Dispersed).WithHardBodyRadii(10, 10).Build();
        Conjunction tight     = new Encounter().WithCovariances(Tight, Tight).WithHardBodyRadii(10, 10).Build();

        // Act
        RiskAssessment dispersedAssessment = dispersed.Assess();
        RiskAssessment tightAssessment     = tight.Assess();

        // Assert
        Assert.Equal(dispersedAssessment.MissDistanceKm, tightAssessment.MissDistanceKm, precision: 12);
        Assert.Equal(RiskLevel.High, dispersedAssessment.Level);
        Assert.Equal(RiskLevel.Low, tightAssessment.Level);
    }

    [Fact]
    public void The_collision_probability_follows_the_encounter_plane_gaussian() {
        // Arrange
        // Pc = R² / (2σ²) · exp(-d² / 2σ²) with R = 20 m, σ² = 0.5 km², d = 1 km.
        Conjunction conjunction = new Encounter().WithCovariances(Dispersed, Dispersed).WithHardBodyRadii(10, 10).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0.02 * 0.02 / 1.0 * Math.Exp(-1), assessment.Probability.Value, precision: 12);
    }

    [Fact]
    public void Co_moving_objects_cannot_be_assessed_in_an_encounter_plane() {
        // Arrange
        Conjunction conjunction = new Encounter()
                                  .PrimaryAt(new Vector3(7000, 0, 0), new Vector3(0, 7.5, 0))
                                  .SecondaryAt(new Vector3(7000.2, 0, 0), new Vector3(0, 7.5, 0))
                                  .Build();

        // Act
        Action assessment = () => conjunction.Assess();

        // Assert
        Assert.Throws<InvalidOperationException>(assessment);
    }

    [Fact]
    public void A_first_assessment_moves_the_conjunction_to_assessed() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Tight, Tight).Build();

        // Act
        conjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
    }

    [Fact]
    public void Each_assessment_is_kept_in_the_history_and_the_latest_is_the_last() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Tight, Tight).Build();
        conjunction.Assess();

        // Act
        RiskAssessment second = conjunction.Assess();

        // Assert
        Assert.Equal(2, conjunction.History.Count);
        Assert.Same(second, conjunction.LatestAssessment);
    }

    [Fact]
    public void A_manoeuvre_is_not_warranted_for_a_low_risk() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Tight, Tight).Build();
        conjunction.Assess();

        // Act
        Action planning = () => conjunction.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(planning);
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
    }

    [Fact]
    public void A_high_risk_assessment_allows_a_manoeuvre_to_be_planned() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Dispersed, Dispersed).WithHardBodyRadii(10, 10).Build();
        conjunction.Assess();

        // Act
        conjunction.PlanManeuver();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, conjunction.Status);
    }

    [Fact]
    public void A_manoeuvre_cannot_be_planned_before_any_assessment() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        Action planning = () => anyConjunction.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(planning);
    }

    [Fact]
    public void A_high_risk_cannot_be_closed_without_a_planned_manoeuvre() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Dispersed, Dispersed).WithHardBodyRadii(10, 10).Build();
        conjunction.Assess();
        string anyReason = Any.String().NotBlank().WithMaxLength(200).Generate();

        // Act
        Action closing = () => conjunction.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(closing);
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
    }

    [Fact]
    public void A_high_risk_with_a_planned_manoeuvre_can_be_closed() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Dispersed, Dispersed).WithHardBodyRadii(10, 10).Build();
        conjunction.Assess();
        conjunction.PlanManeuver();
        string anyReason = Any.String().NotBlank().WithMaxLength(200).Generate();

        // Act
        conjunction.Close(anyReason);

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
    }

    [Fact]
    public void A_low_risk_conjunction_is_closed_with_its_reason() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Tight, Tight).Build();
        conjunction.Assess();

        // Act
        conjunction.Close("Miss distance well outside the dispersion");

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
        Assert.Equal("Miss distance well outside the dispersion", conjunction.ClosureReason);
    }

    [Fact]
    public void A_screened_conjunction_cannot_be_closed() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();
        string      anyReason      = Any.String().NotBlank().WithMaxLength(200).Generate();

        // Act
        Action closing = () => anyConjunction.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(closing);
    }

    [Fact]
    public void A_closed_conjunction_cannot_be_assessed_again() {
        // Arrange
        Conjunction conjunction = new Encounter().WithCovariances(Tight, Tight).Build();
        conjunction.Assess();
        conjunction.Close(Any.String().NotBlank().WithMaxLength(200).Generate());

        // Act
        Action reassessment = () => conjunction.Assess();

        // Assert
        Assert.Throws<InvalidOperationException>(reassessment);
    }

}
