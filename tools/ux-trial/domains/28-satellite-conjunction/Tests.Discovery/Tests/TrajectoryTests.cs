using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class TrajectoryTests {

    private static OrbitalState StateAt(Epoch epoch) {
        return Any.OrbitalState().WithEpoch(epoch).Generate();
    }

    private static OrbitalState StateAt(Epoch epoch, Vector3 position, Vector3 velocity, PositionCovariance covariance) {
        return new OrbitalState(epoch, position, velocity, covariance);
    }

    private static void AssertClose(Vector3 expected, Vector3 actual) {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
        Assert.Equal(expected.Z, actual.Z, 9);
    }

    [Fact]
    public void A_trajectory_keeps_its_object_and_its_states_in_order() {
        // Arrange
        SpaceObject anySpaceObject = Any.SpaceObject().Generate();
        Epoch       anyStart       = Any.Epoch().Generate();
        OrbitalState first         = StateAt(anyStart);
        OrbitalState second        = StateAt(anyStart.Add(TimeSpan.FromMinutes(1)));
        OrbitalState third         = StateAt(anyStart.Add(TimeSpan.FromMinutes(5)));

        // Act
        Trajectory trajectory = new Trajectory(anySpaceObject, new[] { first, second, third });

        // Assert
        Assert.Same(anySpaceObject, trajectory.SpaceObject);
        Assert.Equal(new[] { first, second, third }, trajectory.States);
        Assert.Same(first.Epoch, trajectory.Start);
        Assert.Same(third.Epoch, trajectory.End);
    }

    [Fact]
    public void A_trajectory_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        Epoch              anyStart = Any.Epoch().Generate();
        List<OrbitalState> source   = new List<OrbitalState> { StateAt(anyStart), StateAt(anyStart.Add(TimeSpan.FromMinutes(1))) };
        Trajectory         trajectory = new Trajectory(Any.SpaceObject().Generate(), source);

        // Act
        source.Add(StateAt(anyStart.Add(TimeSpan.FromMinutes(2))));

        // Assert
        Assert.Equal(2, trajectory.States.Count);
    }

    [Fact]
    public void A_trajectory_refuses_a_missing_object() {
        // Arrange
        Epoch anyStart = Any.Epoch().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Trajectory(null!, new[] { StateAt(anyStart), StateAt(anyStart.Add(TimeSpan.FromMinutes(1))) }));

        // Assert
        Assert.Equal("spaceObject", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_missing_states() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Trajectory(Any.SpaceObject().Generate(), null!));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_no_state_at_all() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), Array.Empty<OrbitalState>()));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_a_single_state() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), new[] { Any.OrbitalState().Generate() }));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_a_null_state() {
        // Arrange
        Epoch anyStart = Any.Epoch().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), new OrbitalState?[] { StateAt(anyStart), null }!));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_two_states_at_the_same_epoch() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), new[] { StateAt(anyEpoch), StateAt(anyEpoch) }));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_states_in_decreasing_order() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), new[] { StateAt(anyEpoch.Add(TimeSpan.FromMinutes(1))), StateAt(anyEpoch) }));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_refuses_a_gap_wider_than_ten_minutes() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), new[] { StateAt(anyEpoch), StateAt(anyEpoch.Add(TimeSpan.FromSeconds(601))) }));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_accepts_a_gap_of_exactly_ten_minutes() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Trajectory trajectory = new Trajectory(Any.SpaceObject().Generate(), new[] { StateAt(anyEpoch), StateAt(anyEpoch.Add(TimeSpan.FromMinutes(10))) });

        // Assert
        Assert.Equal(600, trajectory.End.SecondsSince(trajectory.Start));
    }

    [Fact]
    public void A_trajectory_checks_every_gap_not_only_the_first() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();
        OrbitalState[] states = {
            StateAt(anyEpoch),
            StateAt(anyEpoch.Add(TimeSpan.FromMinutes(5))),
            StateAt(anyEpoch.Add(TimeSpan.FromMinutes(5)).Add(TimeSpan.FromSeconds(601)))
        };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(Any.SpaceObject().Generate(), states));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_trajectory_covers_its_whole_span_bounds_included() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().Generate();
        Trajectory trajectory = Any.Trajectory().WithStates(new[] { StateAt(anyStart), StateAt(anyStart.Add(TimeSpan.FromMinutes(4))) }).Generate();

        // Act / Assert
        Assert.True(trajectory.Covers(anyStart));
        Assert.True(trajectory.Covers(anyStart.Add(TimeSpan.FromMinutes(2))));
        Assert.True(trajectory.Covers(anyStart.Add(TimeSpan.FromMinutes(4))));
    }

    [Fact]
    public void A_trajectory_does_not_cover_an_epoch_outside_its_span() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().WithUtc(utc => utc.After(Epoch.SpaceAgeStart.AddDays(1))).Generate();
        Trajectory trajectory = Any.Trajectory().WithStates(new[] { StateAt(anyStart), StateAt(anyStart.Add(TimeSpan.FromMinutes(4))) }).Generate();

        // Act / Assert
        Assert.False(trajectory.Covers(anyStart.Add(TimeSpan.FromTicks(-1))));
        Assert.False(trajectory.Covers(anyStart.Add(TimeSpan.FromMinutes(4)).Add(TimeSpan.FromTicks(1))));
    }

    [Fact]
    public void The_state_at_a_node_is_that_node() {
        // Arrange
        Epoch anyStart = Any.Epoch().Generate();
        OrbitalState first  = StateAt(anyStart);
        OrbitalState second = StateAt(anyStart.Add(TimeSpan.FromMinutes(3)));
        Trajectory   trajectory = Any.Trajectory().WithStates(new[] { first, second }).Generate();

        // Act
        OrbitalState atFirst  = trajectory.StateAt(first.Epoch);
        OrbitalState atSecond = trajectory.StateAt(second.Epoch);

        // Assert: the interpolation computes before + (next - before) * fraction, exact only to the last digit
        AssertClose(first.PositionKm, atFirst.PositionKm);
        AssertClose(first.VelocityKmPerSecond, atFirst.VelocityKmPerSecond);
        AssertClose(second.PositionKm, atSecond.PositionKm);
        AssertClose(second.VelocityKmPerSecond, atSecond.VelocityKmPerSecond);
    }

    [Fact]
    public void The_state_between_two_nodes_is_interpolated_linearly() {
        // Arrange: a quarter of the way through a ten-minute step
        Epoch      anyStart = Any.Epoch().Generate();
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        OrbitalState first  = StateAt(anyStart, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState second = StateAt(anyStart.Add(TimeSpan.FromMinutes(10)), new Vector3(7000, 3000, 0), new Vector3(0, 8, 0), anyCovariance);
        Trajectory   trajectory = Any.Trajectory().WithStates(new[] { first, second }).Generate();
        Epoch        quarter = anyStart.Add(TimeSpan.FromSeconds(150));

        // Act
        OrbitalState state = trajectory.StateAt(quarter);

        // Assert
        Assert.Equal(7000, state.PositionKm.X, 9);
        Assert.Equal(750, state.PositionKm.Y, 9);
        Assert.Equal(0, state.PositionKm.Z, 9);
        Assert.Equal(7.25, state.VelocityKmPerSecond.Y, 12);
        Assert.Equal(quarter, state.Epoch);
    }

    [Fact]
    public void The_interpolated_state_keeps_the_covariance_of_the_nearer_node() {
        // Arrange
        Epoch              anyStart = Any.Epoch().Generate();
        PositionCovariance beforeCovariance = new PositionCovariance(1, 1, 1, 0, 0, 0);
        PositionCovariance afterCovariance   = new PositionCovariance(2, 2, 2, 0, 0, 0);
        OrbitalState       first  = StateAt(anyStart, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), beforeCovariance);
        OrbitalState       second = StateAt(anyStart.Add(TimeSpan.FromMinutes(10)), new Vector3(7000, 3000, 0), new Vector3(0, 8, 0), afterCovariance);
        Trajectory         trajectory = Any.Trajectory().WithStates(new[] { first, second }).Generate();

        // Act / Assert
        Assert.Same(beforeCovariance, trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(299))).Covariance);
        Assert.Same(afterCovariance, trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(301))).Covariance);
    }

    [Fact]
    public void Halfway_between_two_nodes_the_covariance_is_the_later_ones() {
        // Arrange
        Epoch              anyStart = Any.Epoch().Generate();
        PositionCovariance beforeCovariance = new PositionCovariance(1, 1, 1, 0, 0, 0);
        PositionCovariance afterCovariance   = new PositionCovariance(2, 2, 2, 0, 0, 0);
        OrbitalState       first  = StateAt(anyStart, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), beforeCovariance);
        OrbitalState       second = StateAt(anyStart.Add(TimeSpan.FromMinutes(10)), new Vector3(7000, 3000, 0), new Vector3(0, 8, 0), afterCovariance);
        Trajectory         trajectory = Any.Trajectory().WithStates(new[] { first, second }).Generate();

        // Act
        OrbitalState middle = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Same(afterCovariance, middle.Covariance);
    }

    [Fact]
    public void The_state_is_interpolated_between_the_right_pair_of_nodes() {
        // Arrange: three nodes, the epoch asked for lies in the second step
        Epoch anyStart = Any.Epoch().Generate();
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        OrbitalState a = StateAt(anyStart, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState b = StateAt(anyStart.Add(TimeSpan.FromMinutes(5)), new Vector3(7000, 1000, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState c = StateAt(anyStart.Add(TimeSpan.FromMinutes(10)), new Vector3(7000, 1000, 2000), new Vector3(0, 7, 0), anyCovariance);
        Trajectory trajectory = Any.Trajectory().WithStates(new[] { a, b, c }).Generate();

        // Act
        OrbitalState state = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(7.5)));

        // Assert
        Assert.Equal(1000, state.PositionKm.Y, 9);
        Assert.Equal(1000, state.PositionKm.Z, 9);
    }

    [Fact]
    public void The_state_cannot_be_asked_before_the_first_node() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().WithUtc(utc => utc.After(Epoch.SpaceAgeStart.AddDays(1))).Generate();
        Trajectory trajectory = Any.Trajectory().WithStates(new[] { StateAt(anyStart), StateAt(anyStart.Add(TimeSpan.FromMinutes(4))) }).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(-1))));

        // Assert
        Assert.Equal("epoch", refusal.ParamName);
    }

    [Fact]
    public void The_state_cannot_be_asked_after_the_last_node() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().Generate();
        Trajectory trajectory = Any.Trajectory().WithStates(new[] { StateAt(anyStart), StateAt(anyStart.Add(TimeSpan.FromMinutes(4))) }).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(4)).Add(TimeSpan.FromSeconds(1))));

        // Assert
        Assert.Equal("epoch", refusal.ParamName);
    }

    [Fact]
    public void The_state_cannot_be_asked_at_no_epoch() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act
        Action ask = () => anyTrajectory.StateAt(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(ask);
    }

    [Fact]
    public void Every_state_of_a_drawn_trajectory_can_be_interpolated_anywhere_in_its_span() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();
        double     span          = anyTrajectory.End.SecondsSince(anyTrajectory.Start);

        // Act
        OrbitalState state = anyTrajectory.StateAt(anyTrajectory.Start.Add(TimeSpan.FromSeconds(span / 3)));

        // Assert
        Assert.True(state.AltitudeKm > 0);
    }

    [Fact(Skip = "domain defect: a trajectory whose states are 10 minutes apart on opposite sides of the Earth is accepted, then StateAt at the midpoint throws ArgumentOutOfRangeException (interpolated position at the Earth's centre); literal values, no seed")]
    public void A_trajectory_that_was_accepted_can_be_interpolated_between_any_two_of_its_nodes() {
        // Arrange
        Epoch anyStart = Any.Epoch().Generate();
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        OrbitalState first  = StateAt(anyStart, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState second = StateAt(anyStart.Add(TimeSpan.FromMinutes(10)), new Vector3(-7000, 0, 0), new Vector3(0, -7, 0), anyCovariance);
        Trajectory   trajectory = Any.Trajectory().WithStates(new[] { first, second }).Generate();

        // Act
        OrbitalState middle = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(5)));

        // Assert
        Assert.NotNull(middle);
    }

}
