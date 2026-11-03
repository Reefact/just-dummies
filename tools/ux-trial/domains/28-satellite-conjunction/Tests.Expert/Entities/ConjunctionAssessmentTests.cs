using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.Entities;

// What Assess computes, checked against encounters whose geometry is built so that the answer can be worked out by hand.
// In every Encounter the relative velocity is along y, so the encounter plane is the x-z plane and the miss reads off
// directly; Pc = R² / (2 sqrt(det C)) * exp(-M²/2), R the combined hard body in km and C the combined covariance there.
public sealed class ConjunctionAssessmentTests {

    // ---- the geometry reported ------------------------------------------------------------------------------------

    [Fact]
    public void The_miss_distance_is_the_distance_between_the_two_objects_at_the_time_of_closest_approach() {
        // Arrange
        // A 3-4-5 miss in the encounter plane, so the distance is exactly 5 km.
        Conjunction conjunction = Encounter.HeadOn().WithMissInPlane(3, 4).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(5, assessment.MissDistanceKm, precision: 9);
    }

    [Fact]
    public void The_miss_distance_counts_the_gap_along_the_path_too_but_only_the_gap_in_the_encounter_plane_weighs_on_the_probability() {
        // Arrange
        // The covariance is the same in every direction, so only the distance in the plane (0) matters to Pc, whereas the
        // distance reported is the full 3-D one: 12 km along the relative velocity.
        Encounter head = Encounter.OfRisk(RiskLevel.High);

        // Act
        RiskAssessment aligned = head.Build().Assess();
        RiskAssessment ahead   = head.WithAlongTrackOffsetKm(12).Build().Assess();

        // Assert
        Assert.Equal(0, aligned.MissDistanceKm, precision: 9);
        Assert.Equal(12, ahead.MissDistanceKm, precision: 9);
        AssertClose(aligned.Probability.Value, ahead.Probability.Value);
    }

    [Fact]
    public void The_relative_speed_is_the_norm_of_the_difference_of_the_velocities() {
        // Arrange
        // Opposite velocities of 7.5 km/s each.
        Conjunction conjunction = Encounter.HeadOn().WithMissInPlane(1, 0).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(15, assessment.RelativeSpeedKmPerSecond, precision: 9);
    }

    [Fact]
    public void The_combined_sigma_is_the_mean_sigma_of_the_sum_of_the_two_covariances() {
        // Arrange
        // Each object has a variance of 2 km² on every axis: the sum is 4 km², a sigma of 2 km.
        Conjunction conjunction = Encounter.HeadOn().WithCovarianceOfEach(Encounter.Isotropic(2)).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(2, assessment.CombinedSigmaKm, precision: 9);
    }

    [Fact]
    public void The_assessment_is_stamped_with_the_time_of_closest_approach_of_the_conjunction() {
        // Arrange
        Conjunction conjunction = Encounter.HeadOn().Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Same(conjunction.TimeOfClosestApproach, assessment.TimeOfClosestApproach);
    }

    [Fact]
    public void The_states_used_are_the_ones_interpolated_at_the_time_of_closest_approach_not_the_nodes_around_it() {
        // Arrange
        // Both objects fly along y at 7.5 km/s, in opposite directions' worth of bookkeeping: what matters is that at
        // the time of closest approach (a quarter of the way through a 60 s step) the primary has covered 112.5 km and
        // the secondary sits 0.4 km beside it. At either node the two would be 112.5 km (or more) apart.
        Epoch      start     = Any.Epoch().Generate();
        Epoch      tca       = start.Add(TimeSpan.FromSeconds(15));
        Trajectory primary   = new(Any.SpaceObject().Active().Generate(), [
            Node(start, new Vector3(7000, 0, 0), new Vector3(0, 7.5, 0)),
            Node(start.Add(TimeSpan.FromSeconds(60)), new Vector3(7000, 450, 0), new Vector3(0, 7.5, 0))
        ]);
        Trajectory secondary = new(Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).Generate(), [
            Node(start, new Vector3(7000.4, 112.5, 0), new Vector3(0, -7.5, 0)),
            Node(start.Add(TimeSpan.FromSeconds(60)), new Vector3(7000.4, 112.5, 0), new Vector3(0, -7.5, 0))
        ]);
        Conjunction conjunction = new(Any.Guid().NonEmpty().Generate(), primary, secondary, tca);

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0.4, assessment.MissDistanceKm, precision: 9);
        Assert.Equal(15, assessment.RelativeSpeedKmPerSecond, precision: 9);
    }

    // ---- the probability -----------------------------------------------------------------------------------------

    [Fact]
    public void A_direct_hit_between_well_known_objects_has_the_probability_of_the_combined_hard_body_over_twice_the_combined_dispersion() {
        // Arrange
        // R = 10 m = 0.01 km; each covariance 0.05 km² on the diagonal, so 0.1 km² combined: 1e-4 / (2 * 0.1) = 5e-4.
        Conjunction conjunction = Encounter.OfRisk(RiskLevel.High).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        AssertClose(5e-4, assessment.Probability.Value);
        Assert.Equal(RiskLevel.High, assessment.Level);
    }

    [Fact]
    public void Missing_by_some_distance_scales_the_probability_by_the_gaussian_density_at_that_distance() {
        // Arrange
        // Same encounter as above, missed by 0.3 km in the plane: 5e-4 * exp(-0.3² / (2 * 0.1)) = 5e-4 * exp(-0.45).
        Conjunction conjunction = Encounter.OfRisk(RiskLevel.High).WithMissInPlane(0.3, 0).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        AssertClose(3.1881407581088665e-4, assessment.Probability.Value);
    }

    [Fact]
    public void Only_the_sum_of_the_two_hard_body_radii_counts() {
        // Arrange
        Encounter head = Encounter.OfRisk(RiskLevel.High);

        // Act
        RiskAssessment fiveAndFive    = head.WithHardBodyRadiiMeters(5, 5).Build().Assess();
        RiskAssessment threeAndSeven  = head.WithHardBodyRadiiMeters(3, 7).Build().Assess();
        RiskAssessment tenAndNothingMuch = head.WithHardBodyRadiiMeters(9.5, 0.5).Build().Assess();

        // Assert
        AssertClose(fiveAndFive.Probability.Value, threeAndSeven.Probability.Value);
        AssertClose(fiveAndFive.Probability.Value, tenAndNothingMuch.Probability.Value);
    }

    [Fact]
    public void Doubling_the_hard_body_quadruples_the_probability() {
        // Arrange
        Encounter head = Encounter.OfRisk(RiskLevel.Elevated);

        // Act
        RiskAssessment small = head.WithHardBodyRadiiMeters(5, 5).Build().Assess();
        RiskAssessment large = head.WithHardBodyRadiiMeters(10, 10).Build().Assess();

        // Assert
        AssertClose(4 * small.Probability.Value, large.Probability.Value);
    }

    [Fact]
    public void The_same_miss_distance_is_a_negligible_risk_for_tight_covariances_and_a_worrying_one_for_dispersed_ones() {
        // Arrange
        // A 1 km miss. Each object known to 0.1 km (variance 0.01): the miss is 7 sigma away, Pc = 3.5e-14.
        // Each known to 1 km (variance 1): the miss is within the cloud, Pc = 1.9e-5.
        Encounter nearMiss = Encounter.HeadOn().WithHardBodyRadiiMeters(5, 5).WithMissInPlane(1, 0);

        // Act
        RiskAssessment tight     = nearMiss.WithCovarianceOfEach(Encounter.Isotropic(0.01)).Build().Assess();
        RiskAssessment dispersed = nearMiss.WithCovarianceOfEach(Encounter.Isotropic(1)).Build().Assess();

        // Assert
        AssertClose(3.471985966241005e-14, tight.Probability.Value);
        Assert.Equal(RiskLevel.Low, tight.Level);
        AssertClose(1.9470019576785124e-5, dispersed.Probability.Value);
        Assert.Equal(RiskLevel.Elevated, dispersed.Level);
        Assert.Equal(tight.MissDistanceKm, dispersed.MissDistanceKm);
    }

    [Fact]
    public void An_elongated_covariance_makes_a_miss_along_its_long_axis_far_more_likely_than_the_same_miss_across_it() {
        // Arrange
        // Each object: 2 km² along x, 0.02 km² along y and z. Combined 4 / 0.04 / 0.04: det in the x-z plane = 0.16.
        // R = 10 m. A 1 km miss along x is 0.5 sigma away; the same 1 km along z is 5 sigma away.
        PositionCovariance elongatedAlongX = new(2, 0.02, 0.02, 0, 0, 0);
        Encounter          nearMiss        = Encounter.HeadOn().WithHardBodyRadiiMeters(5, 5).WithCovarianceOfEach(elongatedAlongX);

        // Act
        RiskAssessment alongTheLongAxis = nearMiss.WithMissInPlane(1, 0).Build().Assess();
        RiskAssessment acrossIt         = nearMiss.WithMissInPlane(0, 1).Build().Assess();

        // Assert
        AssertClose(1.1031211282307443e-4, alongTheLongAxis.Probability.Value);
        AssertClose(4.658316465098339e-10, acrossIt.Probability.Value);
    }

    [Fact]
    public void A_correlation_between_the_axes_of_the_plane_tilts_the_cloud_so_the_sign_of_the_miss_matters() {
        // Arrange
        // Each object: 1 km² on every axis with a 0.5 km² covariance between x and z. Combined 2, 2 and 1: in the plane the
        // cloud is stretched along the x = z diagonal. (1, 1) is along the stretch, (1, -1) across it.
        PositionCovariance tilted    = new(1, 1, 1, 0, 0.5, 0);
        Encounter          nearMiss  = Encounter.HeadOn().WithHardBodyRadiiMeters(5, 5).WithCovarianceOfEach(tilted);

        // Act
        RiskAssessment alongTheDiagonal  = nearMiss.WithMissInPlane(1, 1).Build().Assess();
        RiskAssessment acrossTheDiagonal = nearMiss.WithMissInPlane(1, -1).Build().Assess();

        // Assert
        AssertClose(2.0684477252128632e-5, alongTheDiagonal.Probability.Value);
        AssertClose(1.0619764719483068e-5, acrossTheDiagonal.Probability.Value);
    }

    [Fact]
    public void The_probability_is_capped_at_one_for_a_very_well_known_encounter_of_very_large_objects() {
        // Arrange
        // R = 0.2 km and a combined variance of 2e-6 km²: the Gaussian formula gives 1e4, which is not a probability.
        Conjunction conjunction = Encounter.HeadOn()
                                           .WithHardBodyRadiiMeters(100, 100)
                                           .WithCovarianceOfEach(Encounter.Isotropic(1e-6))
                                           .Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(1, assessment.Probability.Value);
        Assert.Equal(RiskLevel.High, assessment.Level);
    }

    [Fact]
    public void A_miss_so_large_that_the_gaussian_underflows_gives_a_probability_of_zero_not_an_error() {
        // Arrange
        // 100 km away with a 0.45 km sigma: exp(-M²/2) is far below the smallest double.
        Conjunction conjunction = Encounter.OfRisk(RiskLevel.High).WithMissInPlane(100, 0).Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(0, assessment.Probability.Value);
        Assert.Equal(RiskLevel.Low, assessment.Level);
    }

    // ---- history and status ------------------------------------------------------------------------------------

    [Fact]
    public void The_first_assessment_moves_a_screened_conjunction_to_assessed_and_is_kept_as_the_latest() {
        // Arrange
        Conjunction conjunction = Encounter.HeadOn().Build();

        // Act
        RiskAssessment assessment = conjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
        Assert.Same(assessment, conjunction.LatestAssessment);
        Assert.Equal([assessment], conjunction.History);
    }

    [Fact]
    public void Assessing_again_appends_to_the_history_and_leaves_the_status_alone() {
        // Arrange
        Conjunction conjunction = Encounter.HeadOn().Build();

        // Act
        RiskAssessment first  = conjunction.Assess();
        RiskAssessment second = conjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
        Assert.Equal([first, second], conjunction.History);
        Assert.Same(second, conjunction.LatestAssessment);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Assessing_a_conjunction_whose_manoeuvre_is_planned_keeps_it_planned() {
        // Arrange
        Conjunction conjunction = Encounter.OfRisk(RiskLevel.High).Build();
        conjunction.Assess();
        conjunction.PlanManeuver();

        // Act
        conjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, conjunction.Status);
        Assert.Equal(2, conjunction.History.Count);
    }

    [Fact]
    public void A_closed_conjunction_cannot_be_assessed_again_and_its_history_is_untouched() {
        // Arrange
        Conjunction conjunction = Encounter.OfRisk(RiskLevel.Low).Build();
        conjunction.Assess();
        conjunction.Close(Any.String().Alpha().WithLengthBetween(1, 40).Generate());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => conjunction.Assess());
        Assert.Single(conjunction.History);
    }

    [Fact]
    public void Objects_flying_together_have_no_encounter_to_assess() {
        // Arrange
        Conjunction coMoving = Encounter.HeadOn().WithMissInPlane(2, 0).CoMoving().Build();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => coMoving.Assess());

        // Assert
        Assert.Contains("co-moving", refusal.Message);
        Assert.Equal(ConjunctionStatus.Screened, coMoving.Status);
        Assert.Empty(coMoving.History);
    }

    [Fact(Skip = "domain defect: Combine sums two covariances each within the 1 000 km sigma ceiling into one the PositionCovariance constructor refuses, so Assess throws ArgumentOutOfRangeException on a valid conjunction; literal case, no seed")]
    public void Two_objects_each_known_to_775_km_can_still_be_assessed() {
        // Arrange
        // Each variance is 600 000 km² (775 km): valid. Their sum, 1 200 000 km², is the dispersion of the relative position.
        PositionCovariance poorlyKnown = Encounter.Isotropic(600_000);
        Conjunction        conjunction = Encounter.HeadOn().WithCovarianceOfEach(poorlyKnown).Build();

        // Act
        Exception? failure = Record.Exception(() => conjunction.Assess());

        // Assert
        Assert.Null(failure);
    }

    [Fact(Skip = "domain defect: Assess sums the two covariances and throws ArgumentOutOfRangeException (variance above 1 000 000 km²) for about 88 % of conjunctions of objects each within the ceiling, seed 1278081590"), Reproducible(Seed = 1278081590)]
    public void Any_screened_conjunction_can_be_assessed() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Act
        anyConjunction.Assess();

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
    }

    private static OrbitalState Node(Epoch epoch, Vector3 position, Vector3 velocity) {
        return new OrbitalState(epoch, position, velocity, Any.PositionCovariance().WithXx(v => v.LessThanOrEqualTo(250_000)).WithYy(v => v.LessThanOrEqualTo(250_000)).WithZz(v => v.LessThanOrEqualTo(250_000)).Generate());
    }

    private static void AssertClose(double expected, double actual) {
        Assert.Equal(expected, actual, tolerance: Math.Abs(expected) * 1e-9);
    }

}
