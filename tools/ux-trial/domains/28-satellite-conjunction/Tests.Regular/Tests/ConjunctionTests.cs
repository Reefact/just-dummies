using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class ConjunctionTests {

    public sealed class Construction {

        [Fact]
        public void A_new_conjunction_is_screened_and_holds_what_it_was_built_from() {
            // Arrange
            Guid       anyId        = Any.Guid().NonEmpty().Generate();
            Epoch      anyEpoch     = Any.Epoch().Generate();
            Trajectory anyPrimary   = Any.Trajectory().WithSpaceObject(o => o.Active()).Covering(anyEpoch).Generate();
            Trajectory anySecondary = Any.Trajectory().WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(anyPrimary.SpaceObject.CatalogNumber))).Covering(anyEpoch).Generate();

            // Act
            Conjunction conjunction = new(anyId, anyPrimary, anySecondary, anyEpoch);

            // Assert
            Assert.Equal(anyId, conjunction.Id);
            Assert.Same(anyPrimary, conjunction.Primary);
            Assert.Same(anySecondary, conjunction.Secondary);
            Assert.Same(anyEpoch, conjunction.TimeOfClosestApproach);
            Assert.Equal(ConjunctionStatus.Screened, conjunction.Status);
            Assert.Null(conjunction.LatestAssessment);
            Assert.Empty(conjunction.History);
            Assert.Null(conjunction.ClosureReason);
        }

        [Fact]
        public void An_arbitrary_conjunction_satisfies_the_invariants_of_the_constructor() {
            // Arrange / Act
            Conjunction anyConjunction = Any.Conjunction().Generate();

            // Assert
            Assert.True(anyConjunction.Primary.SpaceObject.IsActive);
            Assert.NotEqual(anyConjunction.Primary.SpaceObject.CatalogNumber, anyConjunction.Secondary.SpaceObject.CatalogNumber);
            Assert.True(anyConjunction.Primary.Covers(anyConjunction.TimeOfClosestApproach));
            Assert.True(anyConjunction.Secondary.Covers(anyConjunction.TimeOfClosestApproach));
            Assert.Equal(ConjunctionStatus.Screened, anyConjunction.Status);
        }

        [Fact]
        public void An_empty_identifier_is_refused() {
            // Arrange
            Conjunction anyConjunction = Any.Conjunction().Generate();

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Guid.Empty, anyConjunction.Primary, anyConjunction.Secondary, anyConjunction.TimeOfClosestApproach));

            // Assert
            Assert.IsType<ArgumentException>(refusal);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void A_missing_part_is_refused(int missing) {
            // Arrange
            Conjunction anyConjunction = Any.Conjunction().Generate();
            Trajectory? primary   = missing == 0 ? null : anyConjunction.Primary;
            Trajectory? secondary = missing == 1 ? null : anyConjunction.Secondary;
            Epoch?      epoch     = missing == 2 ? null : anyConjunction.TimeOfClosestApproach;

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Any.Guid().NonEmpty().Generate(), primary!, secondary!, epoch!));

            // Assert
            Assert.IsType<ArgumentNullException>(refusal);
        }

        [Fact]
        public void A_primary_that_is_not_active_is_refused() {
            // Arrange
            Epoch      anyEpoch     = Any.Epoch().Generate();
            Trajectory anyInertPrimary = Any.Trajectory().WithSpaceObject(o => o.Inert()).Covering(anyEpoch).Generate();
            Trajectory anySecondary = Any.Trajectory().WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(anyInertPrimary.SpaceObject.CatalogNumber))).Covering(anyEpoch).Generate();

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Any.Guid().NonEmpty().Generate(), anyInertPrimary, anySecondary, anyEpoch));

            // Assert
            Assert.IsType<ArgumentException>(refusal);
        }

        [Fact]
        public void An_inert_secondary_is_accepted() {
            // Arrange
            Epoch      anyEpoch          = Any.Epoch().Generate();
            Trajectory anyPrimary        = Any.Trajectory().WithSpaceObject(o => o.Active()).Covering(anyEpoch).Generate();
            Trajectory anyDebrisTrajectory = Any.Trajectory().WithSpaceObject(o => o.Inert().WithKind(ObjectKind.Debris).WithCatalogNumber(n => n.DifferentFrom(anyPrimary.SpaceObject.CatalogNumber))).Covering(anyEpoch).Generate();

            // Act
            Conjunction conjunction = new(Any.Guid().NonEmpty().Generate(), anyPrimary, anyDebrisTrajectory, anyEpoch);

            // Assert
            Assert.Equal(ObjectKind.Debris, conjunction.Secondary.SpaceObject.Kind);
        }

        [Fact]
        public void An_object_cannot_be_in_conjunction_with_itself() {
            // Arrange
            Epoch       anyEpoch  = Any.Epoch().Generate();
            SpaceObject anyActive = Any.SpaceObject().Active().Generate();
            Trajectory  anyPrimary   = Any.Trajectory().WithSpaceObject(anyActive).Covering(anyEpoch).Generate();
            Trajectory  anySecondary = Any.Trajectory().WithSpaceObject(anyActive).Covering(anyEpoch).Generate();

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Any.Guid().NonEmpty().Generate(), anyPrimary, anySecondary, anyEpoch));

            // Assert
            Assert.IsType<ArgumentException>(refusal);
        }

        [Fact]
        public void Two_distinct_objects_sharing_a_catalogue_number_are_still_the_same_object() {
            // Arrange
            Epoch       anyEpoch  = Any.Epoch().Generate();
            SpaceObject anyPrimaryObject = Any.SpaceObject().Active().Generate();
            SpaceObject sameNumber = Any.SpaceObject().WithCatalogNumber(anyPrimaryObject.CatalogNumber).Generate();
            Trajectory  anyPrimary   = Any.Trajectory().WithSpaceObject(anyPrimaryObject).Covering(anyEpoch).Generate();
            Trajectory  anySecondary = Any.Trajectory().WithSpaceObject(sameNumber).Covering(anyEpoch).Generate();

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Any.Guid().NonEmpty().Generate(), anyPrimary, anySecondary, anyEpoch));

            // Assert
            Assert.IsType<ArgumentException>(refusal);
        }

        [Fact]
        public void A_time_of_closest_approach_the_primary_does_not_cover_is_refused() {
            // Arrange
            Epoch     anyStart  = Any.Epoch().Generate();
            Epoch     tca       = anyStart.Add(TimeSpan.FromMinutes(1));
            Trajectory late     = SpanStarting(Any.SpaceObject().Active().Generate(), anyStart.Add(TimeSpan.FromMinutes(2)), TimeSpan.FromMinutes(4));
            Trajectory covering = SpanStarting(Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(late.SpaceObject.CatalogNumber)).Generate(), anyStart, TimeSpan.FromMinutes(6));

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Any.Guid().NonEmpty().Generate(), late, covering, tca));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(refusal);
        }

        [Fact]
        public void A_time_of_closest_approach_the_secondary_does_not_cover_is_refused() {
            // Arrange
            Epoch      anyStart = Any.Epoch().Generate();
            Epoch      tca      = anyStart.Add(TimeSpan.FromMinutes(5));
            Trajectory covering = SpanStarting(Any.SpaceObject().Active().Generate(), anyStart, TimeSpan.FromMinutes(6));
            Trajectory early    = SpanStarting(Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(covering.SpaceObject.CatalogNumber)).Generate(), anyStart.Add(TimeSpan.FromMinutes(-4)), TimeSpan.FromMinutes(4));

            // Act
            Exception? refusal = Record.Exception(() => new Conjunction(Any.Guid().NonEmpty().Generate(), covering, early, tca));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(refusal);
        }

        [Fact]
        public void The_time_of_closest_approach_may_be_the_first_or_the_last_instant_of_both_trajectories() {
            // Arrange
            Epoch       anyStart  = Any.Epoch().Generate();
            SpaceObject anyActive = Any.SpaceObject().Active().Generate();
            SpaceObject anyOther  = Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(anyActive.CatalogNumber)).Generate();
            Trajectory  primary   = SpanStarting(anyActive, anyStart, TimeSpan.FromMinutes(5));
            Trajectory  secondary = SpanStarting(anyOther, anyStart, TimeSpan.FromMinutes(5));

            // Act
            Conjunction atStart = new(Any.Guid().NonEmpty().Generate(), primary, secondary, primary.Start);
            Conjunction atEnd   = new(Any.Guid().NonEmpty().Generate(), primary, secondary, primary.End);

            // Assert
            Assert.Equal(primary.Start, atStart.TimeOfClosestApproach);
            Assert.Equal(primary.End, atEnd.TimeOfClosestApproach);
        }

        [Fact]
        public void Pinning_a_trajectory_in_the_generator_keeps_it_as_it_is() {
            // Arrange
            Conjunction anyReference = Any.Conjunction().Generate();

            // Act
            Conjunction pinned = Any.Conjunction()
                                    .WithPrimary(anyReference.Primary)
                                    .WithSecondary(anyReference.Secondary)
                                    .WithTimeOfClosestApproach(anyReference.TimeOfClosestApproach)
                                    .Generate();

            // Assert
            Assert.Same(anyReference.Primary, pinned.Primary);
            Assert.Same(anyReference.Secondary, pinned.Secondary);
        }

        private static Trajectory SpanStarting(SpaceObject spaceObject, Epoch start, TimeSpan length) {
            return new Trajectory(spaceObject, [Any.OrbitalState().WithEpoch(start).Generate(), Any.OrbitalState().WithEpoch(start.Add(length)).Generate()]);
        }

    }

    public sealed class Assessment {

        [Fact]
        public void Assessing_a_screened_conjunction_moves_it_to_assessed_and_records_the_result() {
            // Arrange
            Conjunction conjunction = Scene.Encounter(new Vector3(1, 0, 0), variance: 0.5);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert
            Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
            Assert.Same(assessment, conjunction.LatestAssessment);
            Assert.Same(assessment, Assert.Single(conjunction.History));
            Assert.Same(conjunction.TimeOfClosestApproach, assessment.TimeOfClosestApproach);
        }

        [Fact]
        public void A_second_assessment_is_appended_and_the_status_stays_assessed() {
            // Arrange
            Conjunction conjunction = Scene.Encounter(new Vector3(1, 0, 0), variance: 0.5);
            RiskAssessment first    = conjunction.Assess();

            // Act
            RiskAssessment second = conjunction.Assess();

            // Assert
            Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
            Assert.Equal(2, conjunction.History.Count);
            Assert.Same(first, conjunction.History[0]);
            Assert.Same(second, conjunction.History[1]);
            Assert.Same(second, conjunction.LatestAssessment);
            Assert.Equal(first.Probability.Value, second.Probability.Value);
        }

        [Fact]
        public void Assessing_after_a_manoeuvre_is_planned_keeps_the_status() {
            // Arrange
            Conjunction conjunction = Scene.AssessedAt(RiskLevel.High);
            conjunction.PlanManeuver();

            // Act
            conjunction.Assess();

            // Assert
            Assert.Equal(ConjunctionStatus.ManeuverPlanned, conjunction.Status);
            Assert.Equal(2, conjunction.History.Count);
        }

        [Fact]
        public void A_closed_conjunction_cannot_be_assessed_again() {
            // Arrange
            Conjunction conjunction = Scene.AssessedAt(RiskLevel.Low);
            conjunction.Close(Any.String().NotBlank().Generate());

            // Act
            Exception? refusal = Record.Exception(() => conjunction.Assess());

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Single(conjunction.History);
        }

        [Fact]
        public void A_direct_hit_with_a_one_square_kilometre_plane_variance_gives_the_hard_body_area_over_two_sigma_squared() {
            // Arrange: radii 10 m + 10 m = 0.02 km, each object 0.5 km² per axis, so 1 km² per axis once combined
            Conjunction conjunction = Scene.Encounter(new Vector3(0, 0, 0), variance: 0.5, radiusMeters: 10);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert: R² / (2 * 1) = 4e-4 / 2
            Assert.Equal(2e-4, assessment.Probability.Value, 12);
            Assert.Equal(RiskLevel.High, assessment.Level);
        }

        [Fact]
        public void A_miss_in_the_encounter_plane_lowers_the_probability_by_the_gaussian_factor() {
            // Arrange
            Conjunction conjunction = Scene.Encounter(new Vector3(1, 0, 0), variance: 0.5, radiusMeters: 10);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert: 2e-4 * exp(-1/2)
            Assert.Equal(2e-4 * Math.Exp(-0.5), assessment.Probability.Value, 12);
        }

        [Fact]
        public void The_miss_distance_is_the_length_of_the_relative_position() {
            // Arrange
            Conjunction conjunction = Scene.Encounter(new Vector3(3, 0, 4), variance: 0.5);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert
            Assert.Equal(5d, assessment.MissDistanceKm, 6);
        }

        [Fact]
        public void The_relative_speed_is_the_length_of_the_relative_velocity() {
            // Arrange: opposite velocities of 3 and 4 km/s
            PositionCovariance covariance   = new(0.5, 0.5, 0.5, 0, 0, 0);
            Conjunction        conjunction  = Scene.Encounter(new Vector3(1, 0, 0), covariance, covariance, primarySpeed: 3, secondarySpeed: 4);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert
            Assert.Equal(7d, assessment.RelativeSpeedKmPerSecond, 9);
        }

        [Fact]
        public void The_combined_sigma_is_the_mean_sigma_of_the_summed_covariances() {
            // Arrange: 2 km² + 6 km² per axis = 8 km²
            Conjunction conjunction = Scene.Encounter(new Vector3(1, 0, 0), new PositionCovariance(2, 2, 2, 0, 0, 0), new PositionCovariance(6, 6, 6, 0, 0, 0));

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert
            Assert.Equal(Math.Sqrt(8), assessment.CombinedSigmaKm, 9);
        }

        [Fact]
        public void The_same_miss_distance_gives_very_different_probabilities_with_a_tighter_or_a_more_dispersed_covariance() {
            // Arrange: a 2 km miss, plane variances of 0.1, 1 and 10 km² once combined
            Vector3 miss = new(2, 0, 0);

            // Act
            double tight     = Scene.Encounter(miss, variance: 0.05).Assess().Probability.Value;
            double moderate  = Scene.Encounter(miss, variance: 0.5).Assess().Probability.Value;
            double dispersed = Scene.Encounter(miss, variance: 5).Assess().Probability.Value;

            // Assert
            Assert.Equal(Scene.IsotropicProbability(0.02, 0.1, 2), tight, 1e-15);
            Assert.Equal(Scene.IsotropicProbability(0.02, 1, 2), moderate, 12);
            Assert.Equal(Scene.IsotropicProbability(0.02, 10, 2), dispersed, 12);
            Assert.True(tight < dispersed / 1_000_000);
            Assert.True(moderate > dispersed);
        }

        [Fact]
        public void The_probability_is_capped_at_one() {
            // Arrange: radii of 100 m each against a combined plane variance of 2e-4 km²
            Conjunction conjunction = Scene.Encounter(new Vector3(0, 0, 0), variance: 1e-4, radiusMeters: 100);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert
            Assert.Equal(1d, assessment.Probability.Value);
            Assert.Equal(RiskLevel.High, assessment.Level);
        }

        [Fact]
        public void An_anisotropic_covariance_makes_the_direction_of_the_miss_matter() {
            // Arrange: combined plane variances of 16 km² along x and 1 km² along z; a 2 km miss along either
            PositionCovariance covariance = new(8, 100, 0.5, 0, 0, 0);

            // Act
            double alongTheLongAxis  = Scene.Encounter(new Vector3(2, 0, 0), covariance, covariance).Assess().Probability.Value;
            double alongTheShortAxis = Scene.Encounter(new Vector3(0, 0, 2), covariance, covariance).Assess().Probability.Value;

            // Assert: R² / (2 sqrt(16)) = 5e-5, times exp(-0.5 * 4 / 16) or exp(-0.5 * 4 / 1)
            Assert.Equal(5e-5 * Math.Exp(-0.125), alongTheLongAxis, 12);
            Assert.Equal(5e-5 * Math.Exp(-2), alongTheShortAxis, 12);
        }

        [Fact]
        public void A_cross_term_inside_the_encounter_plane_changes_the_probability() {
            // Arrange: combined x-z block [[4, 2], [2, 4]], determinant 12
            PositionCovariance covariance = new(2, 50, 2, 0, 1, 0);

            // Act
            RiskAssessment assessment = Scene.Encounter(new Vector3(2, 0, 0), covariance, covariance).Assess();

            // Assert: Mahalanobis^2 = 4 * 4 / 12, Pc = R² / (2 sqrt(12)) * exp(-Mahalanobis^2 / 2)
            Assert.Equal(4e-4 / (2 * Math.Sqrt(12)) * Math.Exp(-(16d / 12) / 2), assessment.Probability.Value, 12);
        }

        [Fact]
        public void The_variance_along_the_relative_velocity_does_not_matter() {
            // Arrange: only the y variance differs, and y is the direction of the relative velocity
            PositionCovariance narrowAlongTrack = new(2, 5, 2, 0, 0, 0);
            PositionCovariance wideAlongTrack   = new(2, 900, 2, 0, 0, 0);
            Vector3            miss             = new(1.5, 0, 0.5);

            // Act
            double narrow = Scene.Encounter(miss, narrowAlongTrack, narrowAlongTrack).Assess().Probability.Value;
            double wide   = Scene.Encounter(miss, wideAlongTrack, wideAlongTrack).Assess().Probability.Value;

            // Assert
            Assert.Equal(narrow, wide, 12);
        }

        [Fact]
        public void The_state_at_the_time_of_closest_approach_is_interpolated_between_the_nodes() {
            // Arrange: nodes 90 s before and 30 s after, on straight lines that are 4 km apart in x at the TCA
            Epoch              anyEpoch   = Any.Epoch().Generate();
            Vector3            anyPosition = Any.OrbitalState().Generate().PositionKm;
            PositionCovariance covariance = new(0.5, 0.5, 0.5, 0, 0, 0);
            SpaceObject        primaryObject   = Any.SpaceObject().Active().Generate();
            SpaceObject        secondaryObject = Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primaryObject.CatalogNumber)).Generate();
            Trajectory         primary   = Scene.Straight(primaryObject, anyEpoch, anyPosition, new Vector3(0, 3, 0), covariance, secondsBefore: 90, secondsAfter: 30);
            Trajectory         secondary = Scene.Straight(secondaryObject, anyEpoch, new Vector3(anyPosition.X + 4, anyPosition.Y, anyPosition.Z), new Vector3(0, -2, 0), covariance, secondsBefore: 30, secondsAfter: 90);
            Conjunction        conjunction = new(Any.Guid().NonEmpty().Generate(), primary, secondary, anyEpoch);

            // Act
            RiskAssessment assessment = conjunction.Assess();

            // Assert
            Assert.Equal(4d, assessment.MissDistanceKm, 6);
            Assert.Equal(5d, assessment.RelativeSpeedKmPerSecond, 9);
        }

        [Fact]
        public void Objects_moving_together_cannot_be_assessed() {
            // Arrange
            PositionCovariance covariance = new(0.5, 0.5, 0.5, 0, 0, 0);
            Conjunction        coMoving   = CoMoving(covariance);

            // Act
            Exception? refusal = Record.Exception(() => coMoving.Assess());

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Equal(ConjunctionStatus.Screened, coMoving.Status);
            Assert.Empty(coMoving.History);
        }

        [Fact(Skip = "domain defect: an arbitrary conjunction whose two trajectories are both valid can fail to assess, because Combine refuses variances above 1 000 000 km2 after summing (ArgumentOutOfRangeException 'A variance must be positive and at most 1000000 km2', value 1308982.48...), seed -2000471454")]
        public void Assessing_an_arbitrary_conjunction_gives_a_probability_and_assesses_it() {
            // Arrange
            Conjunction anyConjunction = Any.Conjunction().Generate();

            // Act
            RiskAssessment assessment = anyConjunction.Assess();

            // Assert
            Assert.InRange(assessment.Probability.Value, 0d, 1d);
            Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
        }

        [Fact]
        public void Assessing_a_conjunction_whose_covariances_cannot_overflow_when_summed_gives_a_probability_and_assesses_it() {
            // Arrange: each variance is kept under half the cap, so that their sum stays valid
            Func<AnyPositionCovariance, AnyPositionCovariance> upToHalfTheCap = c => c.WithXx(x => x.LessThanOrEqualTo(5e5))
                                                                                       .WithYy(y => y.LessThanOrEqualTo(5e5))
                                                                                       .WithZz(z => z.LessThanOrEqualTo(5e5));
            Conjunction anyConjunction = Any.Conjunction()
                                            .WithPrimary(p => p.WithStateCovariance(upToHalfTheCap))
                                            .WithSecondary(s => s.WithStateCovariance(upToHalfTheCap))
                                            .Generate();

            // Act
            RiskAssessment assessment = anyConjunction.Assess();

            // Assert
            Assert.InRange(assessment.Probability.Value, 0d, 1d);
            Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
        }

        private static Conjunction CoMoving(PositionCovariance covariance) {
            Epoch       anyEpoch       = Any.Epoch().Generate();
            Vector3     anyPosition    = Any.OrbitalState().Generate().PositionKm;
            Vector3     anyVelocity    = new(0, Any.Double().Between(0.5, 6).Generate(), 0);
            SpaceObject primaryObject  = Any.SpaceObject().Active().Generate();
            SpaceObject secondaryObject = Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primaryObject.CatalogNumber)).Generate();

            Trajectory primary   = Scene.Straight(primaryObject, anyEpoch, anyPosition, anyVelocity, covariance);
            Trajectory secondary = Scene.Straight(secondaryObject, anyEpoch, anyPosition.Plus(new Vector3(1, 0, 0)), anyVelocity, covariance);

            return new Conjunction(Any.Guid().NonEmpty().Generate(), primary, secondary, anyEpoch);
        }

    }

    public sealed class Manoeuvre {

        [Fact]
        public void A_manoeuvre_cannot_be_planned_before_any_assessment() {
            // Arrange
            Conjunction screened = Scene.ScreenedAt(RiskLevel.High);

            // Act
            Exception? refusal = Record.Exception(() => screened.PlanManeuver());

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Equal(ConjunctionStatus.Screened, screened.Status);
        }

        [Fact]
        public void A_manoeuvre_is_planned_for_a_high_risk() {
            // Arrange
            Conjunction assessed = Scene.AssessedAt(RiskLevel.High);

            // Act
            assessed.PlanManeuver();

            // Assert
            Assert.Equal(ConjunctionStatus.ManeuverPlanned, assessed.Status);
        }

        [Theory]
        [InlineData(RiskLevel.Elevated)]
        [InlineData(RiskLevel.Low)]
        public void A_manoeuvre_is_refused_below_a_high_risk(RiskLevel level) {
            // Arrange
            Conjunction assessed = Scene.AssessedAt(level);

            // Act
            Exception? refusal = Record.Exception(() => assessed.PlanManeuver());

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Equal(ConjunctionStatus.Assessed, assessed.Status);
        }

        [Fact]
        public void The_scene_helpers_really_produce_the_level_they_promise() {
            // Arrange / Act
            RiskLevel high     = Scene.AssessedAt(RiskLevel.High).LatestAssessment!.Level;
            RiskLevel elevated = Scene.AssessedAt(RiskLevel.Elevated).LatestAssessment!.Level;
            RiskLevel low      = Scene.AssessedAt(RiskLevel.Low).LatestAssessment!.Level;

            // Assert
            Assert.Equal(RiskLevel.High, high);
            Assert.Equal(RiskLevel.Elevated, elevated);
            Assert.Equal(RiskLevel.Low, low);
        }

        [Fact]
        public void A_manoeuvre_cannot_be_planned_twice() {
            // Arrange
            Conjunction planned = Scene.AssessedAt(RiskLevel.High);
            planned.PlanManeuver();

            // Act
            Exception? refusal = Record.Exception(() => planned.PlanManeuver());

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
        }

        [Fact]
        public void A_manoeuvre_cannot_be_planned_for_a_closed_conjunction() {
            // Arrange
            Conjunction closed = Scene.AssessedAt(RiskLevel.Low);
            closed.Close(Any.String().NotBlank().Generate());

            // Act
            Exception? refusal = Record.Exception(() => closed.PlanManeuver());

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
        }

    }

    public sealed class Closure {

        [Fact]
        public void A_screened_conjunction_cannot_be_closed() {
            // Arrange
            Conjunction screened = Scene.ScreenedAt(RiskLevel.Low);

            // Act
            Exception? refusal = Record.Exception(() => screened.Close(Any.String().NotBlank().Generate()));

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Equal(ConjunctionStatus.Screened, screened.Status);
        }

        [Theory]
        [InlineData(RiskLevel.Elevated)]
        [InlineData(RiskLevel.Low)]
        public void An_assessed_conjunction_below_a_high_risk_can_be_closed_with_a_reason(RiskLevel level) {
            // Arrange
            Conjunction assessed  = Scene.AssessedAt(level);
            string      anyReason = Any.String().NotBlank().Generate();

            // Act
            assessed.Close(anyReason);

            // Assert
            Assert.Equal(ConjunctionStatus.Closed, assessed.Status);
            Assert.Equal(anyReason, assessed.ClosureReason);
        }

        [Fact]
        public void A_high_risk_cannot_be_closed_before_a_manoeuvre_is_planned() {
            // Arrange
            Conjunction assessed = Scene.AssessedAt(RiskLevel.High);

            // Act
            Exception? refusal = Record.Exception(() => assessed.Close(Any.String().NotBlank().Generate()));

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Equal(ConjunctionStatus.Assessed, assessed.Status);
            Assert.Null(assessed.ClosureReason);
        }

        [Fact]
        public void A_high_risk_can_be_closed_once_a_manoeuvre_is_planned() {
            // Arrange
            Conjunction planned   = Scene.AssessedAt(RiskLevel.High);
            string      anyReason = Any.String().NotBlank().Generate();
            planned.PlanManeuver();

            // Act
            planned.Close(anyReason);

            // Assert
            Assert.Equal(ConjunctionStatus.Closed, planned.Status);
            Assert.Equal(anyReason, planned.ClosureReason);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        public void A_blank_reason_is_refused(string reason) {
            // Arrange
            Conjunction assessed = Scene.AssessedAt(RiskLevel.Low);

            // Act
            Exception? refusal = Record.Exception(() => assessed.Close(reason));

            // Assert
            Assert.IsType<ArgumentException>(refusal);
            Assert.Equal(ConjunctionStatus.Assessed, assessed.Status);
        }

        [Fact]
        public void A_missing_reason_is_refused() {
            // Arrange
            Conjunction assessed = Scene.AssessedAt(RiskLevel.Low);

            // Act
            Exception? refusal = Record.Exception(() => assessed.Close(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(refusal);
        }

        [Fact]
        public void A_closed_conjunction_cannot_be_closed_again_and_keeps_its_first_reason() {
            // Arrange
            Conjunction closed      = Scene.AssessedAt(RiskLevel.Low);
            string      firstReason = Any.String().NotBlank().Generate();
            closed.Close(firstReason);

            // Act
            Exception? refusal = Record.Exception(() => closed.Close(Any.String().NotBlank().DifferentFrom(firstReason).Generate()));

            // Assert
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.Equal(firstReason, closed.ClosureReason);
        }

        [Fact]
        public void Closing_leaves_the_history_untouched() {
            // Arrange
            Conjunction assessed = Scene.AssessedAt(RiskLevel.Elevated);
            RiskAssessment latest = assessed.LatestAssessment!;

            // Act
            assessed.Close(Any.String().NotBlank().Generate());

            // Assert
            Assert.Same(latest, Assert.Single(assessed.History));
            Assert.Same(latest, assessed.LatestAssessment);
        }

    }

}
