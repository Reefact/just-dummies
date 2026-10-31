using System;

using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class TrajectoryTests {

    [Fact]
    public void A_single_state_cannot_be_interpolated() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        OrbitalState anyState  = Any.OrbitalState().Generate();

        // Act
        Action creation = () => _ = new Trajectory(anyObject, [anyState]);

        // Assert
        Assert.Throws<ArgumentException>(creation);
    }

    [Fact]
    public void States_eleven_minutes_apart_leave_too_wide_a_gap() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        Epoch        anyStart  = Any.Epoch().Generate();
        OrbitalState first     = Any.OrbitalState().WithEpoch(anyStart).Generate();
        OrbitalState second    = Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromMinutes(11))).Generate();

        // Act
        Action creation = () => _ = new Trajectory(anyObject, [first, second]);

        // Assert
        Assert.Throws<ArgumentException>(creation);
    }

    [Fact]
    public void States_out_of_chronological_order_are_refused() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        Epoch        anyStart  = Any.Epoch().Generate();
        OrbitalState earlier   = Any.OrbitalState().WithEpoch(anyStart).Generate();
        OrbitalState later     = Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromMinutes(2))).Generate();

        // Act
        Action creation = () => _ = new Trajectory(anyObject, [later, earlier]);

        // Assert
        Assert.Throws<ArgumentException>(creation);
    }

    [Fact]
    public void Halfway_between_two_states_the_position_is_the_midpoint() {
        // Arrange
        Epoch        anyStart = Any.Epoch().Generate();
        OrbitalState first    = Any.OrbitalState().WithEpoch(anyStart).WithPositionKm(new Vector3(7000, 0, 0)).Generate();
        OrbitalState second   = Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromMinutes(4))).WithPositionKm(new Vector3(7000, 1800, 600)).Generate();
        Trajectory   trajectory = Any.Trajectory().WithStates([first, second]).Generate();

        // Act
        OrbitalState halfway = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(2)));

        // Assert
        Assert.Equal(new Vector3(7000, 900, 300), halfway.PositionKm);
    }

    [Fact]
    public void An_interpolated_state_keeps_the_covariance_of_the_nearer_node() {
        // Arrange
        PositionCovariance nearCovariance = Encounter.Isotropic(0.01);
        PositionCovariance farCovariance  = Encounter.Isotropic(4);
        Epoch              anyStart       = Any.Epoch().Generate();
        OrbitalState       first          = Any.OrbitalState().WithEpoch(anyStart).WithCovariance(nearCovariance).Generate();
        OrbitalState       second         = Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromMinutes(8))).WithCovariance(farCovariance).Generate();
        Trajectory         trajectory     = Any.Trajectory().WithStates([first, second]).Generate();

        // Act
        OrbitalState state = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(2)));

        // Assert
        Assert.Same(nearCovariance, state.Covariance);
    }

    [Fact]
    public void A_state_is_refused_after_the_end_of_the_trajectory() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act
        Action interpolation = () => anyTrajectory.StateAt(anyTrajectory.End.Add(TimeSpan.FromMinutes(1)));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(interpolation);
    }

}
