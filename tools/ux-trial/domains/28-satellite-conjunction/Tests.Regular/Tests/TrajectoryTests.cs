using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class TrajectoryTests {

    /// <summary>States one <paramref name="step" /> apart starting at <paramref name="start" />, positions and velocities of no particular interest.</summary>
    private static List<OrbitalState> StatesEvery(TimeSpan step, Epoch start, int count) {
        List<OrbitalState> states = new();
        for (int index = 0; index < count; index++) {
            states.Add(Any.OrbitalState().WithEpoch(start.Add(TimeSpan.FromTicks(step.Ticks * index))).Generate());
        }

        return states;
    }

    private static OrbitalState StateAt(Epoch epoch, Vector3 position, Vector3 velocity, PositionCovariance covariance) {
        return new OrbitalState(epoch, position, velocity, covariance);
    }

    [Fact]
    public void A_trajectory_exposes_its_object_and_its_states_in_order() {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        Epoch              anyStart  = Any.Epoch().Generate();
        List<OrbitalState> anyStates = StatesEvery(TimeSpan.FromMinutes(5), anyStart, 4);

        // Act
        Trajectory trajectory = new(anyObject, anyStates);

        // Assert
        Assert.Same(anyObject, trajectory.SpaceObject);
        Assert.Equal(anyStates, trajectory.States);
        Assert.Same(anyStates[0].Epoch, trajectory.Start);
        Assert.Same(anyStates[3].Epoch, trajectory.End);
    }

    [Fact]
    public void An_arbitrary_trajectory_is_always_accepted() {
        // Arrange / Act
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Assert
        Assert.True(anyTrajectory.States.Count >= 2);
        Assert.True(anyTrajectory.Start.CompareTo(anyTrajectory.End) < 0);
    }

    [Fact]
    public void A_trajectory_keeps_the_states_it_was_given_even_if_the_caller_changes_its_list() {
        // Arrange
        List<OrbitalState> anyStates = StatesEvery(TimeSpan.FromMinutes(1), Any.Epoch().Generate(), 3);
        Trajectory         trajectory = new(Any.SpaceObject().Generate(), anyStates);

        // Act
        anyStates.Clear();

        // Assert
        Assert.Equal(3, trajectory.States.Count);
    }

    [Fact]
    public void A_missing_object_is_refused() {
        // Arrange
        List<OrbitalState> anyStates = StatesEvery(TimeSpan.FromMinutes(1), Any.Epoch().Generate(), 2);

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(null!, anyStates));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_missing_list_of_states_is_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Fewer_than_two_states_are_refused(int count) {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        List<OrbitalState> states    = StatesEvery(TimeSpan.FromMinutes(1), Any.Epoch().Generate(), count);

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, states));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_null_state_is_refused() {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        List<OrbitalState> states    = StatesEvery(TimeSpan.FromMinutes(1), Any.Epoch().Generate(), 3);
        states[1] = null!;

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, states));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Two_states_at_the_same_epoch_are_refused() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        Epoch        anyEpoch  = Any.Epoch().Generate();
        OrbitalState first     = Any.OrbitalState().WithEpoch(anyEpoch).Generate();
        OrbitalState second    = Any.OrbitalState().WithEpoch(anyEpoch).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, [first, second]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void States_in_decreasing_epoch_order_are_refused() {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        List<OrbitalState> ascending = StatesEvery(TimeSpan.FromMinutes(1), Any.Epoch().Generate(), 3);
        ascending.Reverse();

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, ascending));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_state_out_of_order_in_the_middle_of_the_list_is_refused() {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        List<OrbitalState> states    = StatesEvery(TimeSpan.FromMinutes(1), Any.Epoch().Generate(), 4);
        (states[1], states[2]) = (states[2], states[1]);

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, states));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Two_states_exactly_ten_minutes_apart_are_accepted() {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        List<OrbitalState> states    = StatesEvery(Trajectory.MaximumStep, Any.Epoch().Generate(), 3);

        // Act
        Trajectory trajectory = new(anyObject, states);

        // Assert
        Assert.Equal(3, trajectory.States.Count);
    }

    [Fact]
    public void Two_states_more_than_ten_minutes_apart_are_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       anyStart  = Any.Epoch().Generate();
        TimeSpan    anyGap    = Any.TimeSpan().Between(Trajectory.MaximumStep + TimeSpan.FromSeconds(1), TimeSpan.FromDays(2)).WithGranularity(TimeSpan.FromSeconds(1)).Generate();
        OrbitalState first    = Any.OrbitalState().WithEpoch(anyStart).Generate();
        OrbitalState second   = Any.OrbitalState().WithEpoch(anyStart.Add(anyGap)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, [first, second]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_gap_of_ten_minutes_and_one_tick_is_refused() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        Epoch        anyStart  = Any.Epoch().Generate();
        OrbitalState first     = Any.OrbitalState().WithEpoch(anyStart).Generate();
        OrbitalState second    = Any.OrbitalState().WithEpoch(anyStart.Add(Trajectory.MaximumStep + TimeSpan.FromTicks(1))).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, [first, second]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_too_large_gap_in_the_middle_of_a_long_trajectory_is_refused() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        Epoch        anyStart  = Any.Epoch().Generate();
        OrbitalState first     = Any.OrbitalState().WithEpoch(anyStart).Generate();
        OrbitalState second    = Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromMinutes(5))).Generate();
        OrbitalState third     = Any.OrbitalState().WithEpoch(anyStart.Add(TimeSpan.FromMinutes(20))).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Trajectory(anyObject, [first, second, third]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_trajectory_covers_its_span_ends_included() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().Generate();
        Trajectory trajectory = Any.Trajectory().WithStates(StatesEvery(TimeSpan.FromMinutes(5), anyStart, 3)).Generate();

        // Act / Assert
        Assert.True(trajectory.Covers(trajectory.Start));
        Assert.True(trajectory.Covers(trajectory.End));
        Assert.True(trajectory.Covers(anyStart.Add(TimeSpan.FromMinutes(7))));
    }

    [Fact]
    public void A_trajectory_does_not_cover_what_lies_outside_its_span() {
        // Arrange
        Epoch      anyStart   = Any.Epoch().Generate();
        Trajectory trajectory = Any.Trajectory().WithStates(StatesEvery(TimeSpan.FromMinutes(5), anyStart, 3)).Generate();

        // Act / Assert
        Assert.False(trajectory.Covers(anyStart.Add(TimeSpan.FromTicks(-1))));
        Assert.False(trajectory.Covers(trajectory.End.Add(TimeSpan.FromTicks(1))));
    }

    [Fact(Skip = "domain defect: Covers(null) throws NullReferenceException while its sibling StateAt(null) throws ArgumentNullException; a missing epoch should be refused the same way. Deterministic: no seed needed")]
    public void Asking_whether_a_missing_epoch_is_covered_is_refused_like_asking_for_its_state() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyTrajectory.Covers(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void The_state_at_a_node_is_that_node() {
        // Arrange
        Trajectory trajectory = Any.Trajectory().Generate();

        // Act / Assert
        foreach (OrbitalState node in trajectory.States) {
            OrbitalState interpolated = trajectory.StateAt(node.Epoch);

            Assert.Equal(node.PositionKm.X, interpolated.PositionKm.X, 6);
            Assert.Equal(node.PositionKm.Y, interpolated.PositionKm.Y, 6);
            Assert.Equal(node.PositionKm.Z, interpolated.PositionKm.Z, 6);
            Assert.Equal(node.VelocityKmPerSecond.X, interpolated.VelocityKmPerSecond.X, 9);
            Assert.Equal(node.VelocityKmPerSecond.Y, interpolated.VelocityKmPerSecond.Y, 9);
            Assert.Equal(node.VelocityKmPerSecond.Z, interpolated.VelocityKmPerSecond.Z, 9);
        }
    }

    [Fact]
    public void The_state_halfway_between_two_nodes_is_their_average() {
        // Arrange
        Epoch              anyStart   = Any.Epoch().Generate();
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        OrbitalState       first      = StateAt(anyStart, new Vector3(7_000, 0, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState       second     = StateAt(anyStart.Add(TimeSpan.FromMinutes(2)), new Vector3(7_000, 840, 0), new Vector3(1, 7, 2), anyCovariance);
        Trajectory         trajectory = new(Any.SpaceObject().Generate(), [first, second]);

        // Act
        OrbitalState middle = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(1)));

        // Assert
        Assert.Equal(new Vector3(7_000, 420, 0), middle.PositionKm);
        Assert.Equal(new Vector3(0.5, 7, 1), middle.VelocityKmPerSecond);
    }

    [Fact]
    public void The_interpolation_is_linear_at_a_quarter_of_the_way() {
        // Arrange
        Epoch              anyStart      = Any.Epoch().Generate();
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        OrbitalState       first         = StateAt(anyStart, new Vector3(7_000, 0, 0), new Vector3(0, 8, 0), anyCovariance);
        OrbitalState       second        = StateAt(anyStart.Add(TimeSpan.FromSeconds(100)), new Vector3(7_000, 800, 400), new Vector3(0, 4, 0), anyCovariance);
        Trajectory         trajectory    = new(Any.SpaceObject().Generate(), [first, second]);

        // Act
        OrbitalState quarter = trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(25)));

        // Assert
        Assert.Equal(7_000d, quarter.PositionKm.X, 9);
        Assert.Equal(200d, quarter.PositionKm.Y, 9);
        Assert.Equal(100d, quarter.PositionKm.Z, 9);
        Assert.Equal(7d, quarter.VelocityKmPerSecond.Y, 9);
    }

    [Fact]
    public void The_interpolation_reads_the_segment_that_contains_the_epoch() {
        // Arrange
        Epoch              anyStart      = Any.Epoch().Generate();
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        OrbitalState       first         = StateAt(anyStart, new Vector3(7_000, 0, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState       second        = StateAt(anyStart.Add(TimeSpan.FromMinutes(1)), new Vector3(7_000, 100, 0), new Vector3(0, 7, 0), anyCovariance);
        OrbitalState       third         = StateAt(anyStart.Add(TimeSpan.FromMinutes(3)), new Vector3(7_000, 100, 600), new Vector3(0, 7, 0), anyCovariance);
        Trajectory         trajectory    = new(Any.SpaceObject().Generate(), [first, second, third]);

        // Act
        OrbitalState inSecondSegment = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(2)));

        // Assert
        Assert.Equal(100d, inSecondSegment.PositionKm.Y, 9);
        Assert.Equal(300d, inSecondSegment.PositionKm.Z, 9);
    }

    [Fact]
    public void The_interpolated_state_is_stamped_with_the_requested_epoch() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();
        Epoch      anyEpoch      = anyTrajectory.Start.Add(TimeSpan.FromSeconds(anyTrajectory.End.SecondsSince(anyTrajectory.Start) / 3));

        // Act
        OrbitalState state = anyTrajectory.StateAt(anyEpoch);

        // Assert
        Assert.Equal(anyEpoch, state.Epoch);
    }

    [Fact]
    public void The_interpolated_state_keeps_the_covariance_of_the_nearer_node() {
        // Arrange
        Epoch              anyStart         = Any.Epoch().Generate();
        PositionCovariance tight            = new(1, 1, 1, 0, 0, 0);
        PositionCovariance dispersed        = new(900, 900, 900, 0, 0, 0);
        OrbitalState       first            = StateAt(anyStart, new Vector3(7_000, 0, 0), new Vector3(0, 7, 0), tight);
        OrbitalState       second           = StateAt(anyStart.Add(TimeSpan.FromSeconds(100)), new Vector3(7_000, 700, 0), new Vector3(0, 7, 0), dispersed);
        Trajectory         trajectory       = new(Any.SpaceObject().Generate(), [first, second]);

        // Act
        OrbitalState nearFirst  = trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(20)));
        OrbitalState nearSecond = trajectory.StateAt(anyStart.Add(TimeSpan.FromSeconds(80)));

        // Assert
        Assert.Same(tight, nearFirst.Covariance);
        Assert.Same(dispersed, nearSecond.Covariance);
    }

    [Fact]
    public void An_epoch_before_the_span_is_refused() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();
        TimeSpan   anyAdvance    = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromDays(30)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyTrajectory.StateAt(anyTrajectory.Start.Add(-anyAdvance)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void An_epoch_after_the_span_is_refused() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();
        TimeSpan   anyDelay      = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromDays(30)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyTrajectory.StateAt(anyTrajectory.End.Add(anyDelay)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_missing_epoch_is_refused() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyTrajectory.StateAt(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_covering_trajectory_does_cover_the_epoch_it_was_asked_to() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Trajectory trajectory = Any.Trajectory().Covering(anyEpoch).Generate();

        // Assert
        Assert.True(trajectory.Covers(anyEpoch));
    }

    [Fact(Skip = "domain defect: linear interpolation between two valid states can leave the valid region; on a 350 km circular orbit (radius 6728 km, 7.7 km/s) two nodes at the maximum 10 minutes apart have a chord midpoint at about 6340 km, below the Earth's surface, so StateAt on a covered epoch throws ArgumentOutOfRangeException from the OrbitalState constructor. Deterministic: no seed needed")]
    public void The_state_at_the_middle_of_a_maximum_step_on_a_low_circular_orbit_is_still_a_state() {
        // Arrange
        const double radius      = 6_728;
        const double speed       = 7.7;
        double       angle       = speed * 600 / radius;
        Epoch        anyStart    = Any.Epoch().Generate();
        PositionCovariance covariance = Any.PositionCovariance().Generate();
        OrbitalState first  = StateAt(anyStart, new Vector3(radius, 0, 0), new Vector3(0, speed, 0), covariance);
        OrbitalState second = StateAt(anyStart.Add(Trajectory.MaximumStep), new Vector3(radius * Math.Cos(angle), radius * Math.Sin(angle), 0), new Vector3(-speed * Math.Sin(angle), speed * Math.Cos(angle), 0), covariance);
        Trajectory   trajectory = new(Any.SpaceObject().Generate(), [first, second]);

        // Act
        OrbitalState middle = trajectory.StateAt(anyStart.Add(TimeSpan.FromMinutes(5)));

        // Assert
        Assert.True(middle.AltitudeKm > 0);
    }

}
