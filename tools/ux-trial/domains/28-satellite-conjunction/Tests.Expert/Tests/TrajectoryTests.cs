using JustDummies;
using SatelliteConjunction.Domain;
using SatelliteConjunction.Tests.Expert.Arbitrary;

namespace SatelliteConjunction.Tests.Expert.Tests;

public sealed class TrajectoryTests {

    [Fact]
    public void A_single_state_cannot_be_interpolated() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        OrbitalState anyState  = Any.OrbitalState().Generate();

        // Act
        Action creating = () => new Trajectory(anyObject, [anyState]);

        // Assert
        Assert.Throws<ArgumentException>("states", creating);
    }

    [Fact]
    public void Two_states_at_the_same_epoch_are_refused() {
        // Arrange
        SpaceObject  anyObject      = Any.SpaceObject().Generate();
        Epoch        anyEpoch       = Any.Epoch().Generate();
        OrbitalState anyState       = Any.OrbitalState().WithEpoch(anyEpoch).Generate();
        OrbitalState anyOtherState  = Any.OrbitalState().WithEpoch(anyEpoch).Generate();

        // Act
        Action creating = () => new Trajectory(anyObject, [anyState, anyOtherState]);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>("states", creating);
        Assert.Contains("strictly increase", refusal.Message);
    }

    [Fact]
    public void States_eleven_minutes_apart_are_too_far_apart_to_interpolate() {
        // Arrange
        SpaceObject  anyObject     = Any.SpaceObject().Generate();
        Epoch        anyEpoch      = Any.Epoch().Generate();
        OrbitalState anyFirst      = Any.OrbitalState().WithEpoch(anyEpoch).Generate();
        OrbitalState anyElevenLater = Any.OrbitalState().WithEpoch(anyEpoch.Add(TimeSpan.FromMinutes(11))).Generate();

        // Act
        Action creating = () => new Trajectory(anyObject, [anyFirst, anyElevenLater]);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>("states", creating);
        Assert.Contains("more than 10 minutes apart", refusal.Message);
    }

    [Fact]
    public void Halfway_between_two_nodes_the_position_and_velocity_are_halfway_too() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().Generate();
        Trajectory trajectory = new(Any.SpaceObject().Generate(), [
            Any.OrbitalState().WithEpoch(anyStart).WithPositionKm(new Vector3(7_000, 0, 0)).WithVelocityKmPerSecond(new Vector3(0, 7, 0)).Generate(),
            Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromSeconds(60))).WithPositionKm(new Vector3(7_000, 420, 0)).WithVelocityKmPerSecond(new Vector3(0, 7, 1)).Generate(),
        ]);

        // Act
        OrbitalState halfway = trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(30)));

        // Assert
        Assert.Equal(new Vector3(7_000, 210, 0), halfway.PositionKm);
        Assert.Equal(new Vector3(0, 7, 0.5), halfway.VelocityKmPerSecond);
    }

    [Fact]
    public void An_interpolated_state_keeps_the_covariance_of_the_nearer_node() {
        // Arrange
        Epoch              anyStart           = Any.Epoch().Generate();
        PositionCovariance anyEarlyCovariance = Any.PositionCovariance().Generate();
        PositionCovariance anyLateCovariance  = Any.PositionCovariance().Generate();
        Trajectory trajectory = Any.Trajectory().WithStates([
            Any.OrbitalState().WithEpoch(anyStart).WithCovariance(anyEarlyCovariance).WithVelocityKmPerSecond(new Vector3(0, 7, 0)).WithPositionKm(new Vector3(7_000, 0, 0)).Generate(),
            Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromSeconds(60))).WithCovariance(anyLateCovariance).WithVelocityKmPerSecond(new Vector3(0, 7, 0)).WithPositionKm(new Vector3(7_000, 420, 0)).Generate(),
        ]).Generate();

        // Act
        OrbitalState nearTheEnd = trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(45)));

        // Assert
        Assert.Same(anyLateCovariance, nearTheEnd.Covariance);
    }

    [Fact]
    public void A_trajectory_covers_its_first_and_last_epochs_and_nothing_beyond() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act
        bool[] covered = [
            anyTrajectory.Covers(anyTrajectory.Start),
            anyTrajectory.Covers(anyTrajectory.End),
            anyTrajectory.Covers(anyTrajectory.End.Add(TimeSpan.FromSeconds(1))),
        ];

        // Assert
        Assert.Equal([true, true, false], covered);
    }

    [Fact]
    public void A_state_after_the_last_node_is_not_extrapolated() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act
        Action interpolating = () => anyTrajectory.StateAt(anyTrajectory.End.Add(TimeSpan.FromSeconds(1)));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("epoch", interpolating);
    }

}
