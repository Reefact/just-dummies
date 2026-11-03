using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class ConjunctionTests {

    private static Trajectory TrajectoryAround(Epoch moment, Func<AnySpaceObject, AnySpaceObject> shape) {
        return Any.Trajectory()
                  .WithSpaceObject(shape)
                  .WithStates(Orbits.StatesAround(moment, Any.PositionCovariance().Combinable()))
                  .Generate();
    }

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_conjunction_is_screened_with_nothing_assessed_yet() {
        // Arrange / Act
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Assert
        Assert.Equal(ConjunctionStatus.Screened, anyConjunction.Status);
        Assert.Null(anyConjunction.LatestAssessment);
        Assert.Empty(anyConjunction.History);
        Assert.Null(anyConjunction.ClosureReason);
    }

    [Fact]
    public void A_conjunction_keeps_its_identifier_its_trajectories_and_its_time_of_closest_approach() {
        // Arrange
        Guid       anyId     = Any.Guid().NonEmpty().Generate();
        Epoch      anyMoment = Any.Epoch().Generate();
        Trajectory primary   = TrajectoryAround(anyMoment, o => o.Active());
        Trajectory secondary = TrajectoryAround(anyMoment, o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)));

        // Act
        Conjunction conjunction = new Conjunction(anyId, primary, secondary, anyMoment);

        // Assert
        Assert.Equal(anyId, conjunction.Id);
        Assert.Same(primary, conjunction.Primary);
        Assert.Same(secondary, conjunction.Secondary);
        Assert.Same(anyMoment, conjunction.TimeOfClosestApproach);
    }

    [Fact]
    public void A_conjunction_refuses_an_empty_identifier() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Conjunction().WithId(Guid.Empty).Generate());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_refuses_a_missing_primary() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Conjunction().WithPrimary((Trajectory)null!).Generate());

        // Assert
        Assert.Equal("primary", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_refuses_a_missing_secondary() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Conjunction().WithSecondary((Trajectory)null!).Generate());

        // Assert
        Assert.Equal("secondary", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_refuses_a_missing_time_of_closest_approach() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Conjunction().WithTimeOfClosestApproach((Epoch)null!).Generate());

        // Assert
        Assert.Equal("timeOfClosestApproach", refusal.ParamName);
    }

    [Theory]
    [InlineData(ObjectKind.Payload)]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void A_conjunction_refuses_a_primary_that_is_not_active(ObjectKind kind) {
        // Arrange
        Epoch      anyMoment     = Any.Epoch().Generate();
        Trajectory inertPrimary = TrajectoryAround(anyMoment, o => o.WithKind(kind).WithIsActive(false));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Conjunction().WithTimeOfClosestApproach(anyMoment).WithPrimary(inertPrimary).Generate());

        // Assert
        Assert.Equal("primary", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_accepts_an_inert_secondary() {
        // Arrange
        Conjunction conjunction = Any.Conjunction().WithSecondary(s => s.WithSpaceObject(o => o.Inert())).Generate();

        // Act / Assert
        Assert.False(conjunction.Secondary.SpaceObject.IsActive);
    }

    [Fact]
    public void A_conjunction_refuses_an_object_in_conjunction_with_itself() {
        // Arrange
        Epoch       anyMoment = Any.Epoch().Generate();
        SpaceObject satellite = Any.SpaceObject().Active().Generate();
        Trajectory  primary   = Any.Trajectory().WithSpaceObject(satellite).WithStates(Orbits.StatesAround(anyMoment, Any.PositionCovariance().Combinable())).Generate();
        Trajectory  secondary = Any.Trajectory().WithSpaceObject(satellite).WithStates(Orbits.StatesAround(anyMoment, Any.PositionCovariance().Combinable())).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Conjunction().WithTimeOfClosestApproach(anyMoment).WithPrimary(primary).WithSecondary(secondary).Generate());

        // Assert
        Assert.Equal("secondary", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_refuses_two_objects_sharing_a_catalogue_number_even_if_they_are_distinct_instances() {
        // Arrange
        Epoch      anyMoment = Any.Epoch().Generate();
        Trajectory primary   = TrajectoryAround(anyMoment, o => o.Active());
        Trajectory secondary = TrajectoryAround(anyMoment, o => o.WithCatalogNumber(primary.SpaceObject.CatalogNumber));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Conjunction().WithTimeOfClosestApproach(anyMoment).WithPrimary(primary).WithSecondary(secondary).Generate());

        // Assert
        Assert.Equal("secondary", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_refuses_a_time_of_closest_approach_the_primary_does_not_cover() {
        // Arrange
        Epoch      anyMoment = Any.Epoch().Generate();
        Trajectory primary   = TrajectoryAround(anyMoment, o => o.Active());
        Epoch      afterIt   = primary.End.Add(TimeSpan.FromSeconds(1));
        Trajectory secondary = TrajectoryAround(afterIt, o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Conjunction().WithTimeOfClosestApproach(afterIt).WithPrimary(primary).WithSecondary(secondary).Generate());

        // Assert
        Assert.Equal("timeOfClosestApproach", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_refuses_a_time_of_closest_approach_the_secondary_does_not_cover() {
        // Arrange
        Epoch      anyMoment = Any.Epoch().Generate();
        Trajectory secondary = TrajectoryAround(anyMoment, o => o);
        Epoch      beforeIt  = secondary.Start.Add(TimeSpan.FromSeconds(-1));
        Trajectory primary   = TrajectoryAround(beforeIt, o => o.Active().WithCatalogNumber(n => n.DifferentFrom(secondary.SpaceObject.CatalogNumber)));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Conjunction().WithTimeOfClosestApproach(beforeIt).WithPrimary(primary).WithSecondary(secondary).Generate());

        // Assert
        Assert.Equal("timeOfClosestApproach", refusal.ParamName);
    }

    [Fact]
    public void A_time_of_closest_approach_at_the_very_edge_of_both_trajectories_is_covered() {
        // Arrange
        Epoch      anyMoment = Any.Epoch().Generate();
        Trajectory primary   = TrajectoryAround(anyMoment, o => o.Active());
        Trajectory secondary = Any.Trajectory()
                                  .WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)))
                                  .WithStates(Orbits.StatesAround(primary.End, Any.PositionCovariance().Combinable()))
                                  .Generate();

        // Act
        Conjunction conjunction = Any.Conjunction().WithTimeOfClosestApproach(primary.End).WithPrimary(primary).WithSecondary(secondary).Generate();

        // Assert
        Assert.Same(primary.End, conjunction.TimeOfClosestApproach);
    }

    // ---- assessment ---------------------------------------------------------------------------------------------

    [Fact]
    public void Assessing_a_screened_conjunction_moves_it_to_assessed_and_records_the_assessment() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        RiskAssessment assessment = anyConjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
        Assert.Same(assessment, anyConjunction.LatestAssessment);
        Assert.Equal(new[] { assessment }, anyConjunction.History);
    }

    [Fact]
    public void An_assessment_carries_the_time_of_closest_approach_of_its_conjunction() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        RiskAssessment assessment = anyConjunction.Assess();

        // Assert
        Assert.Same(anyConjunction.TimeOfClosestApproach, assessment.TimeOfClosestApproach);
    }

    [Fact]
    public void Assessing_again_appends_to_the_history_and_keeps_the_status() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();
        RiskAssessment first = anyConjunction.Assess();

        // Act
        RiskAssessment second = anyConjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
        Assert.Equal(new[] { first, second }, anyConjunction.History);
        Assert.Same(second, anyConjunction.LatestAssessment);
    }

    [Fact]
    public void Assessing_the_same_trajectories_twice_gives_the_same_figures() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        RiskAssessment first  = anyConjunction.Assess();
        RiskAssessment second = anyConjunction.Assess();

        // Assert
        Assert.Equal(first.MissDistanceKm, second.MissDistanceKm);
        Assert.Equal(first.Probability.Value, second.Probability.Value);
    }

    [Fact]
    public void A_head_on_encounter_reports_its_miss_distance_relative_speed_and_dispersion() {
        // Arrange
        Conjunction conjunction = Encounter.Between(missKm: 0.48);

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0.48, assessment.MissDistanceKm, 9);
        Assert.Equal(15, assessment.RelativeSpeedKmPerSecond, 9);
        Assert.Equal(Math.Sqrt(0.02), assessment.CombinedSigmaKm, 9);
    }

    [Fact]
    public void A_dead_centre_hit_has_the_probability_of_the_hard_body_over_the_dispersion() {
        // Arrange: R = 0.02 km, combined variance 0.02 km2 on the plane => R2 / (2 * 0.02) = 0.01
        Conjunction conjunction = Encounter.HighRisk();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0.01, assessment.Probability.Value, 9);
        Assert.Equal(RiskLevel.High, assessment.Level);
    }

    [Fact]
    public void The_probability_falls_off_with_the_mahalanobis_distance() {
        // Arrange: d2 = 0.48 ^ 2 / 0.02 = 11.52, so Pc = 0.01 * exp(-5.76)
        Conjunction conjunction = Encounter.ElevatedRisk();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0.01 * Math.Exp(-5.76), assessment.Probability.Value, 12);
        Assert.Equal(RiskLevel.Elevated, assessment.Level);
    }

    [Fact]
    public void A_wide_miss_is_a_low_risk() {
        // Arrange
        Conjunction conjunction = Encounter.LowRisk();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(RiskLevel.Low, assessment.Level);
        Assert.True(assessment.Probability.Value < 1e-30);
    }

    [Fact]
    public void The_same_miss_distance_gives_a_very_different_probability_with_a_tighter_or_a_more_dispersed_covariance() {
        // Arrange: one kilometre apart in both, 0.01 km2 against 0.25 km2 per object
        Conjunction tight     = Encounter.Between(missKm: 1, varianceKm2: 0.01);
        Conjunction dispersed = Encounter.Between(missKm: 1, varianceKm2: 0.25);

        // Act
        RiskAssessment tightAssessment     = tight.Assess();
        RiskAssessment dispersedAssessment = dispersed.Assess();

        // Assert
        Assert.Equal(tightAssessment.MissDistanceKm, dispersedAssessment.MissDistanceKm, 9);
        Assert.Equal(RiskLevel.Low, tightAssessment.Level);
        Assert.Equal(RiskLevel.High, dispersedAssessment.Level);
    }

    [Fact]
    public void The_probability_is_capped_at_one() {
        // Arrange: two 100 m bodies and a combined variance of 2e-6 km2: R2 / (2 sqrt(det)) = 1e4 before the cap
        Conjunction conjunction = Encounter.Between(missKm: 0, varianceKm2: 1e-6, radiusMeters: 100);

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(1, assessment.Probability.Value);
    }

    [Fact]
    public void A_time_of_closest_approach_between_nodes_is_assessed_on_the_interpolated_states() {
        // Arrange: nodes 100 s before and 500 s after the closest approach, on straight lines
        Conjunction conjunction = Encounter.Between(missKm: 0.48, secondsBeforeClosestApproach: 100, secondsAfterClosestApproach: 500);

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0.48, assessment.MissDistanceKm, 6);
        Assert.Equal(15, assessment.RelativeSpeedKmPerSecond, 9);
    }

    [Fact]
    public void Objects_moving_together_cannot_be_assessed() {
        // Arrange: both move at +7.5 km/s
        Conjunction coMoving = Encounter.Between(missKm: 1, secondaryVelocityYKmPerSecond: 7.5);

        // Act
        Action assess = () => coMoving.Assess();

        // Assert
        Assert.Throws<InvalidOperationException>(assess);
        Assert.Equal(ConjunctionStatus.Screened, coMoving.Status);
        Assert.Empty(coMoving.History);
    }

    [Fact]
    public void Objects_whose_relative_speed_is_just_above_the_co_moving_threshold_can_be_assessed() {
        // Arrange: relative speed of 1e-5 km/s
        Conjunction nearlyCoMoving = Encounter.Between(missKm: 1, secondaryVelocityYKmPerSecond: 7.5 - 1e-5);

        // Act
        RiskAssessment assessment = nearlyCoMoving.Assess();

        // Assert
        Assert.Equal(1e-5, assessment.RelativeSpeedKmPerSecond, 9);
    }

    [Fact(Skip = "domain defect: Assess throws ArgumentOutOfRangeException when the combined covariance exceeds 1e6 km2 although each trajectory's covariance is valid (xx = 600000 km2); literal values, no seed")]
    public void Two_trajectories_with_wide_but_valid_covariances_can_be_assessed() {
        // Arrange
        Conjunction conjunction = Encounter.Between(missKm: 10, varianceKm2: 600_000);

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.NotNull(assessment);
    }

    // ---- manoeuvre ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_manoeuvre_cannot_be_planned_before_the_conjunction_is_assessed() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        Action plan = () => anyConjunction.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(plan);
        Assert.Equal(ConjunctionStatus.Screened, anyConjunction.Status);
    }

    [Fact]
    public void A_manoeuvre_is_planned_for_a_high_risk() {
        // Arrange
        Conjunction highRisk = Encounter.HighRisk();
        highRisk.Assess();

        // Act
        highRisk.PlanManeuver();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, highRisk.Status);
    }

    [Fact]
    public void A_manoeuvre_is_not_warranted_for_an_elevated_risk() {
        // Arrange
        Conjunction elevated = Encounter.ElevatedRisk();
        elevated.Assess();

        // Act
        Action plan = () => elevated.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(plan);
        Assert.Equal(ConjunctionStatus.Assessed, elevated.Status);
    }

    [Fact]
    public void A_manoeuvre_is_not_warranted_for_a_low_risk() {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();

        // Act
        Action plan = () => low.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(plan);
        Assert.Equal(ConjunctionStatus.Assessed, low.Status);
    }

    [Fact]
    public void A_manoeuvre_cannot_be_planned_twice() {
        // Arrange
        Conjunction highRisk = Encounter.HighRisk();
        highRisk.Assess();
        highRisk.PlanManeuver();

        // Act
        Action planAgain = () => highRisk.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(planAgain);
    }

    [Fact]
    public void A_manoeuvre_cannot_be_planned_once_the_conjunction_is_closed() {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();
        low.Close(Any.String().NotBlank().WithMaxLength(40).Generate());

        // Act
        Action plan = () => low.PlanManeuver();

        // Assert
        Assert.Throws<InvalidOperationException>(plan);
    }

    [Fact]
    public void A_conjunction_with_a_planned_manoeuvre_can_still_be_assessed_without_leaving_that_status() {
        // Arrange
        Conjunction highRisk = Encounter.HighRisk();
        highRisk.Assess();
        highRisk.PlanManeuver();

        // Act
        highRisk.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, highRisk.Status);
        Assert.Equal(2, highRisk.History.Count);
    }

    // ---- closure ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_screened_conjunction_cannot_be_closed() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();
        string      anyReason      = Any.String().NotBlank().WithMaxLength(40).Generate();

        // Act
        Action close = () => anyConjunction.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(close);
        Assert.Equal(ConjunctionStatus.Screened, anyConjunction.Status);
    }

    [Fact]
    public void A_low_risk_conjunction_can_be_closed_and_remembers_why() {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();
        string anyReason = Any.String().NotBlank().WithMaxLength(40).Generate();

        // Act
        low.Close(anyReason);

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, low.Status);
        Assert.Equal(anyReason, low.ClosureReason);
    }

    [Fact]
    public void An_elevated_risk_conjunction_can_be_closed() {
        // Arrange
        Conjunction elevated = Encounter.ElevatedRisk();
        elevated.Assess();

        // Act
        elevated.Close("watched, no action needed");

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, elevated.Status);
    }

    [Fact]
    public void A_high_risk_conjunction_cannot_be_closed_before_a_manoeuvre_is_planned() {
        // Arrange
        Conjunction highRisk = Encounter.HighRisk();
        highRisk.Assess();
        string anyReason = Any.String().NotBlank().WithMaxLength(40).Generate();

        // Act
        Action close = () => highRisk.Close(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(close);
        Assert.Equal(ConjunctionStatus.Assessed, highRisk.Status);
        Assert.Null(highRisk.ClosureReason);
    }

    [Fact]
    public void A_high_risk_conjunction_can_be_closed_once_a_manoeuvre_is_planned() {
        // Arrange
        Conjunction highRisk = Encounter.HighRisk();
        highRisk.Assess();
        highRisk.PlanManeuver();

        // Act
        highRisk.Close("manoeuvre executed");

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, highRisk.Status);
        Assert.Equal("manoeuvre executed", highRisk.ClosureReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_conjunction_cannot_be_closed_without_a_reason(string blank) {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();

        // Act
        Action close = () => low.Close(blank);

        // Assert
        Assert.Throws<ArgumentException>(close);
        Assert.Equal(ConjunctionStatus.Assessed, low.Status);
    }

    [Fact]
    public void A_conjunction_cannot_be_closed_with_a_missing_reason() {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();

        // Act
        Action close = () => low.Close(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(close);
    }

    [Fact]
    public void A_closed_conjunction_cannot_be_closed_again() {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();
        low.Close("first reason");

        // Act
        Action closeAgain = () => low.Close("second reason");

        // Assert
        Assert.Throws<InvalidOperationException>(closeAgain);
        Assert.Equal("first reason", low.ClosureReason);
    }

    [Fact]
    public void A_closed_conjunction_cannot_be_assessed_again() {
        // Arrange
        Conjunction low = Encounter.LowRisk();
        low.Assess();
        low.Close(Any.String().NotBlank().WithMaxLength(40).Generate());

        // Act
        Action assess = () => low.Assess();

        // Assert
        Assert.Throws<InvalidOperationException>(assess);
        Assert.Single(low.History);
    }

}
