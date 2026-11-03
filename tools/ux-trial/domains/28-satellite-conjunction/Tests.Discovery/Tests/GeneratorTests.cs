using JustDummies;

using Xunit;

using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Discovery;

// The generators of this project are hand-completed code: these tests check that they draw what the domain accepts.
public sealed class GeneratorTests {

    private const int Draws = 200;

    [Fact]
    public void The_epoch_generator_draws_utc_instants_after_the_space_age() {
        for (int draw = 0; draw < Draws; draw++) {
            Epoch anyEpoch = Any.Epoch().Generate();

            Assert.Equal(TimeSpan.Zero, anyEpoch.Utc.Offset);
            Assert.True(anyEpoch.Utc >= Epoch.SpaceAgeStart);
        }
    }

    [Fact]
    public void The_covariance_generator_draws_positive_definite_covariances() {
        for (int draw = 0; draw < Draws; draw++) {
            PositionCovariance anyCovariance = Any.PositionCovariance().Generate();

            Assert.InRange(anyCovariance.MeanSigmaKm, 0, PositionCovariance.MaximumSigmaKm);
        }
    }

    [Fact]
    public void The_space_object_generator_draws_active_objects_only_among_payloads() {
        for (int draw = 0; draw < Draws; draw++) {
            SpaceObject anySpaceObject = Any.SpaceObject().Generate();

            Assert.True(!anySpaceObject.IsActive || anySpaceObject.Kind == ObjectKind.Payload);
        }
    }

    [Fact]
    public void The_orbital_state_generator_draws_states_above_the_surface() {
        for (int draw = 0; draw < Draws; draw++) {
            OrbitalState anyState = Any.OrbitalState().Generate();

            Assert.True(anyState.AltitudeKm > 0);
        }
    }

    [Fact]
    public void The_trajectory_generator_draws_trajectories_that_can_be_interpolated() {
        for (int draw = 0; draw < Draws; draw++) {
            Trajectory anyTrajectory = Any.Trajectory().Generate();

            OrbitalState middle = anyTrajectory.StateAt(anyTrajectory.Start.Add(TimeSpan.FromSeconds(anyTrajectory.End.SecondsSince(anyTrajectory.Start) / 2)));

            Assert.True(middle.AltitudeKm > 0);
        }
    }

    [Fact]
    public void The_conjunction_generator_draws_conjunctions_that_can_be_assessed() {
        for (int draw = 0; draw < Draws; draw++) {
            Conjunction anyConjunction = Any.Conjunction().Generate();

            RiskAssessment assessment = anyConjunction.Assess();

            Assert.Equal(ConjunctionStatus.Assessed, anyConjunction.Status);
            Assert.NotNull(assessment);
        }
    }
}
