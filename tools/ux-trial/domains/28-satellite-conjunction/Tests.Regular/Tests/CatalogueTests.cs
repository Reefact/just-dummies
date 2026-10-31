using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Regular.Tests;

public sealed class SpaceObjectTests {

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void Only_a_payload_can_be_active(ObjectKind inertKind) {
        // Arrange
        AnySpaceObject anyObject = Arbitrary.SpaceObject().WithKind(inertKind);

        // Act
        Action act = () => anyObject.WithIsActive(true).Generate();

        // Assert
        Assert.Throws<ArgumentException>("isActive", act);
    }

    [Fact]
    public void A_name_longer_than_24_characters_is_refused() {
        // Arrange
        string anyLongName = Any.String().Alpha().WithLength(SpaceObject.MaximumNameLength + 1).Generate();

        // Act
        Action act = () => Arbitrary.SpaceObject().WithName(anyLongName).Generate();

        // Assert
        Assert.Throws<ArgumentException>("name", act);
    }

}

public sealed class OrbitalStateTests {

    [Fact]
    public void A_position_inside_the_earth_is_refused() {
        // Arrange
        OrbitalState anyState      = Arbitrary.OrbitalState().Generate();
        Vector3      belowSurface  = new(6000, 0, 0);

        // Act
        Action act = () => new OrbitalState(anyState.Epoch, belowSurface, anyState.VelocityKmPerSecond, anyState.Covariance);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("positionKm", act);
    }

    [Fact]
    public void A_speed_above_12_km_per_second_is_refused() {
        // Arrange
        OrbitalState anyState   = Arbitrary.OrbitalState().Generate();
        Vector3      escapeSpeed = new(0, 12.5, 0);

        // Act
        Action act = () => new OrbitalState(anyState.Epoch, anyState.PositionKm, escapeSpeed, anyState.Covariance);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("velocityKmPerSecond", act);
    }

    [Fact]
    public void The_altitude_is_measured_from_the_earth_surface() {
        // Arrange
        OrbitalState anyState = Arbitrary.OrbitalState().WithPositionKm(new Vector3(0, 0, 7000)).Generate();

        // Act
        double altitude = anyState.AltitudeKm;

        // Assert
        Assert.Equal(621.863, altitude, 9);
    }

}

public sealed class TrajectoryTests {

    [Fact]
    public void A_trajectory_refuses_states_out_of_epoch_order() {
        // Arrange
        Epoch           anyEpoch = Arbitrary.Epoch().Generate();
        AnyOrbitalState anyState = Arbitrary.OrbitalState();
        OrbitalState[]  states   = [anyState.WithEpoch(anyEpoch.Add(TimeSpan.FromMinutes(1))).Generate(), anyState.WithEpoch(anyEpoch).Generate()];

        // Act
        Action act = () => Arbitrary.Trajectory().WithStates(states).Generate();

        // Assert
        Assert.Throws<ArgumentException>("states", act);
    }

    [Fact]
    public void A_trajectory_refuses_a_gap_longer_than_ten_minutes() {
        // Arrange
        Epoch           anyEpoch = Arbitrary.Epoch().Generate();
        AnyOrbitalState anyState = Arbitrary.OrbitalState();
        OrbitalState[]  states   = [anyState.WithEpoch(anyEpoch).Generate(), anyState.WithEpoch(anyEpoch.Add(TimeSpan.FromMinutes(11))).Generate()];

        // Act
        Action act = () => Arbitrary.Trajectory().WithStates(states).Generate();

        // Assert
        Assert.Throws<ArgumentException>("states", act);
    }

    [Fact]
    public void The_state_halfway_between_two_nodes_is_their_linear_interpolation() {
        // Arrange
        Epoch              anyEpoch      = Arbitrary.Epoch().Generate();
        PositionCovariance anyCovariance = Arbitrary.PositionCovariance().Generate();
        SpaceObject        anyObject     = Arbitrary.SpaceObject().Generate();
        Trajectory trajectory = new(anyObject, [
            new OrbitalState(anyEpoch, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), anyCovariance),
            new OrbitalState(anyEpoch.Add(TimeSpan.FromMinutes(2)), new Vector3(7000, 840, 0), new Vector3(0, 7.2, 0), anyCovariance),
        ]);

        // Act
        OrbitalState halfway = trajectory.StateAt(anyEpoch.Add(TimeSpan.FromMinutes(1)));

        // Assert
        Assert.Equal(new Vector3(7000, 420, 0), halfway.PositionKm);
        Assert.Equal(7.1, halfway.VelocityKmPerSecond.Y, 12);
    }

    [Fact]
    public void An_interpolated_state_keeps_the_covariance_of_the_nearer_node() {
        // Arrange
        Epoch              anyEpoch = Arbitrary.Epoch().Generate();
        PositionCovariance tight    = Encounters.Isotropic(0.1);
        PositionCovariance loose    = Encounters.Isotropic(2);
        // Both nodes share one drawn position: two independent positions may put the Earth between them.
        AnyOrbitalState anyNode = Arbitrary.OrbitalState().WithPositionKm(Arbitrary.OrbitalState().Generate().PositionKm);
        Trajectory trajectory = Arbitrary.Trajectory().WithStates([
            anyNode.WithEpoch(anyEpoch).WithCovariance(tight).Generate(),
            anyNode.WithEpoch(anyEpoch.Add(TimeSpan.FromMinutes(4))).WithCovariance(loose).Generate(),
        ]).Generate();

        // Act
        OrbitalState nearTheEnd = trajectory.StateAt(anyEpoch.Add(TimeSpan.FromMinutes(3)));

        // Assert
        Assert.Same(loose, nearTheEnd.Covariance);
    }

    [Fact]
    public void A_trajectory_refuses_to_predict_outside_its_span() {
        // Arrange
        Trajectory anyTrajectory = Arbitrary.Trajectory().Generate();
        Epoch      afterTheEnd   = anyTrajectory.End.Add(TimeSpan.FromSeconds(1));

        // Act
        Action act = () => anyTrajectory.StateAt(afterTheEnd);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("epoch", act);
    }

}
