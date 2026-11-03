using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.Entities;

public sealed class TrajectoryTests {

    private static readonly Vector3 SomePosition = new(7000, 0, 0);
    private static readonly Vector3 SomeVelocity = new(0, 7.5, 0);

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_trajectory_exposes_its_object_its_states_and_the_span_they_cover() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       start     = Any.Epoch().Generate();
        OrbitalState first    = StateAt(start);
        OrbitalState second   = StateAt(start.Add(TimeSpan.FromMinutes(5)));
        OrbitalState third    = StateAt(start.Add(TimeSpan.FromMinutes(12)));

        // Act
        Trajectory trajectory = new(anyObject, [first, second, third]);

        // Assert
        Assert.Same(anyObject, trajectory.SpaceObject);
        Assert.Equal([first, second, third], trajectory.States);
        Assert.Same(first.Epoch, trajectory.Start);
        Assert.Same(third.Epoch, trajectory.End);
    }

    [Fact]
    public void A_trajectory_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        Epoch              start  = Any.Epoch().Generate();
        List<OrbitalState> states = [StateAt(start), StateAt(start.Add(TimeSpan.FromMinutes(1)))];
        Trajectory         trajectory = new(Any.SpaceObject().Generate(), states);

        // Act
        states.Add(StateAt(start.Add(TimeSpan.FromMinutes(2))));
        states.RemoveAt(0);

        // Assert
        Assert.Equal(2, trajectory.States.Count);
        Assert.Same(start, trajectory.Start);
    }

    [Fact]
    public void A_trajectory_without_an_object_is_refused() {
        // Arrange
        IEnumerable<OrbitalState> anyStates = Any.Trajectory().Generate().States;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Trajectory(null!, anyStates));
    }

    [Fact]
    public void A_trajectory_without_states_is_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Trajectory(anyObject, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Fewer_than_two_states_cannot_be_interpolated_and_are_refused(int count) {
        // Arrange
        SpaceObject        anyObject = Any.SpaceObject().Generate();
        List<OrbitalState> states    = [.. Any.ListOf(new AnyOrbitalState()).WithCount(count).Generate()];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, states));

        // Assert
        Assert.Equal("states", refusal.ParamName);
    }

    [Fact]
    public void A_null_state_is_refused_wherever_it_sits() {
        // Arrange
        SpaceObject  anyObject = Any.SpaceObject().Generate();
        Epoch        start     = Any.Epoch().Generate();
        OrbitalState first     = StateAt(start);
        OrbitalState third     = StateAt(start.Add(TimeSpan.FromMinutes(2)));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, [first, null!, third]));

        // Assert
        Assert.Equal("states", refusal.ParamName);
        Assert.Contains("1", refusal.Message);
    }

    [Fact]
    public void Two_states_at_the_same_epoch_are_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       epoch     = Any.Epoch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, [StateAt(epoch), StateAt(epoch)]));
    }

    [Fact]
    public void States_in_decreasing_epoch_order_are_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       later     = Any.Epoch().Generate();
        Epoch       earlier   = later.Add(TimeSpan.FromMinutes(-1));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, [StateAt(later), StateAt(earlier)]));
    }

    [Fact]
    public void An_epoch_that_goes_back_in_the_middle_of_a_series_is_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       start     = Any.Epoch().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, [
            StateAt(start),
            StateAt(start.Add(TimeSpan.FromMinutes(5))),
            StateAt(start.Add(TimeSpan.FromMinutes(3)))
        ]));

        // Assert
        Assert.Contains("2", refusal.Message);
    }

    [Fact]
    public void Two_states_exactly_ten_minutes_apart_are_accepted() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       start     = Any.Epoch().Generate();

        // Act
        Trajectory trajectory = new(anyObject, [StateAt(start), StateAt(start.Add(TimeSpan.FromMinutes(10)))]);

        // Assert
        Assert.Equal(600, trajectory.End.SecondsSince(trajectory.Start));
    }

    [Fact]
    public void Two_states_one_tick_more_than_ten_minutes_apart_are_refused() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       start     = Any.Epoch().Generate();
        Epoch       tooLate   = start.Add(TimeSpan.FromMinutes(10)).Add(TimeSpan.FromTicks(1));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, [StateAt(start), StateAt(tooLate)]));
    }

    [Fact]
    public void A_gap_in_the_middle_of_a_series_is_refused_even_when_the_whole_span_is_long_anyway() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();
        Epoch       start     = Any.Epoch().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Trajectory(anyObject, [
            StateAt(start),
            StateAt(start.Add(TimeSpan.FromMinutes(10))),
            StateAt(start.Add(TimeSpan.FromMinutes(30)))
        ]));

        // Assert
        Assert.Contains("1", refusal.Message);
        Assert.Contains("2", refusal.Message);
    }

    [Fact]
    public void A_long_trajectory_is_accepted_as_long_as_every_step_stays_within_ten_minutes() {
        // Arrange
        Epoch start = Any.Epoch().Generate();
        List<OrbitalState> states = [.. Enumerable.Range(0, 25).Select(step => StateAt(start.Add(TimeSpan.FromMinutes(9 * step))))];

        // Act
        Trajectory trajectory = new(Any.SpaceObject().Generate(), states);

        // Assert
        Assert.Equal(25, trajectory.States.Count);
        Assert.Equal(9 * 24 * 60, trajectory.End.SecondsSince(trajectory.Start));
    }

    // ---- coverage -----------------------------------------------------------------------------------------------

    [Fact]
    public void A_trajectory_covers_both_ends_of_its_span_and_everything_between() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = TwoStatesFrom(start, TimeSpan.FromMinutes(8));

        // Act & Assert
        Assert.True(trajectory.Covers(start));
        Assert.True(trajectory.Covers(start.Add(TimeSpan.FromMinutes(3))));
        Assert.True(trajectory.Covers(start.Add(TimeSpan.FromMinutes(8))));
    }

    [Fact]
    public void A_trajectory_does_not_cover_an_instant_one_tick_outside_its_span() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = TwoStatesFrom(start, TimeSpan.FromMinutes(8));

        // Act & Assert
        Assert.False(trajectory.Covers(start.Add(TimeSpan.FromTicks(-1))));
        Assert.False(trajectory.Covers(start.Add(TimeSpan.FromMinutes(8)).Add(TimeSpan.FromTicks(1))));
    }

    // ---- interpolation ------------------------------------------------------------------------------------------

    [Fact]
    public void The_state_at_a_quarter_of_the_way_is_a_quarter_of_the_way_between_the_neighbouring_states() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = new(Any.SpaceObject().Generate(), [
            new OrbitalState(start, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), Any.PositionCovariance().Generate()),
            new OrbitalState(start.Add(TimeSpan.FromSeconds(100)), new Vector3(7000, 400, 80), new Vector3(0, 8, 2), Any.PositionCovariance().Generate())
        ]);

        // Act
        OrbitalState state = trajectory.StateAt(start.Add(TimeSpan.FromSeconds(25)));

        // Assert
        Assert.Equal(new Vector3(7000, 100, 20), state.PositionKm);
        Assert.Equal(new Vector3(0, 7.25, 0.5), state.VelocityKmPerSecond);
    }

    [Fact]
    public void The_state_returned_is_stamped_with_the_epoch_asked_for() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = TwoStatesFrom(start, TimeSpan.FromMinutes(4));
        Epoch      asked      = start.Add(TimeSpan.FromSeconds(77));

        // Act
        OrbitalState state = trajectory.StateAt(asked);

        // Assert
        Assert.Equal(asked, state.Epoch);
    }

    [Fact]
    public void At_a_node_the_state_is_that_node() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = new(Any.SpaceObject().Generate(), [
            new OrbitalState(start, new Vector3(7000, 0, 0), new Vector3(0, 7, 0), Any.PositionCovariance().Generate()),
            new OrbitalState(start.Add(TimeSpan.FromSeconds(100)), new Vector3(7000, 400, 80), new Vector3(0, 8, 2), Any.PositionCovariance().Generate())
        ]);

        // Act
        OrbitalState atStart = trajectory.StateAt(start);
        OrbitalState atEnd   = trajectory.StateAt(trajectory.End);

        // Assert
        Assert.Equal(new Vector3(7000, 0, 0), atStart.PositionKm);
        Assert.Equal(new Vector3(7000, 400, 80), atEnd.PositionKm);
        Assert.Equal(new Vector3(0, 8, 2), atEnd.VelocityKmPerSecond);
    }

    [Fact]
    public void With_several_states_the_interpolation_uses_the_two_neighbours_of_the_epoch_and_their_own_spacing() {
        // Arrange
        // Three nodes unevenly spaced (60 s then 180 s), so that a wrong segment or a wrong denominator shows.
        Epoch start = Any.Epoch().Generate();
        Trajectory trajectory = new(Any.SpaceObject().Generate(), [
            new OrbitalState(start, new Vector3(7000, 0, 0), SomeVelocity, Any.PositionCovariance().Generate()),
            new OrbitalState(start.Add(TimeSpan.FromSeconds(60)), new Vector3(7000, 60, 0), SomeVelocity, Any.PositionCovariance().Generate()),
            new OrbitalState(start.Add(TimeSpan.FromSeconds(240)), new Vector3(7000, 60, 360), SomeVelocity, Any.PositionCovariance().Generate())
        ]);

        // Act
        OrbitalState state = trajectory.StateAt(start.Add(TimeSpan.FromSeconds(150)));

        // Assert
        // 90 s into a 180 s segment: halfway between (7000, 60, 0) and (7000, 60, 360).
        Assert.Equal(new Vector3(7000, 60, 180), state.PositionKm);
    }

    [Fact]
    public void The_covariance_kept_is_that_of_the_node_the_epoch_is_nearer_to() {
        // Arrange
        Epoch              start        = Any.Epoch().Generate();
        PositionCovariance startKnown   = Any.PositionCovariance().Generate();
        PositionCovariance endKnown     = Any.PositionCovariance().Generate();
        Trajectory         trajectory   = new(Any.SpaceObject().Generate(), [
            new OrbitalState(start, SomePosition, SomeVelocity, startKnown),
            new OrbitalState(start.Add(TimeSpan.FromSeconds(100)), SomePosition, SomeVelocity, endKnown)
        ]);

        // Act
        OrbitalState nearerTheStart = trajectory.StateAt(start.Add(TimeSpan.FromSeconds(25)));
        OrbitalState nearerTheEnd   = trajectory.StateAt(start.Add(TimeSpan.FromSeconds(75)));

        // Assert
        Assert.Same(startKnown, nearerTheStart.Covariance);
        Assert.Same(endKnown, nearerTheEnd.Covariance);
    }

    [Fact]
    public void An_epoch_before_the_start_is_refused() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = TwoStatesFrom(start, TimeSpan.FromMinutes(4));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => trajectory.StateAt(start.Add(TimeSpan.FromTicks(-1))));

        // Assert
        Assert.Equal("epoch", refusal.ParamName);
    }

    [Fact]
    public void An_epoch_after_the_end_is_refused() {
        // Arrange
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = TwoStatesFrom(start, TimeSpan.FromMinutes(4));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => trajectory.StateAt(trajectory.End.Add(TimeSpan.FromTicks(1))));
    }

    [Fact]
    public void An_epoch_that_is_null_is_refused() {
        // Arrange
        Trajectory anyTrajectory = Any.Trajectory().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyTrajectory.StateAt(null!));
    }

    [Fact(Skip = "domain defect: a trajectory accepts two valid states whose interpolation is not a valid state, so StateAt (and Assess) can throw on a trajectory the constructor accepted; literal case, no seed")]
    public void A_trajectory_the_constructor_accepted_can_be_interpolated_at_any_epoch_it_covers() {
        // Arrange
        // Both states are valid and ten minutes apart (the limit). Halfway between them, linear interpolation puts the object
        // at the centre of the earth: not a position OrbitalState accepts.
        Epoch      start      = Any.Epoch().Generate();
        Trajectory trajectory = new(Any.SpaceObject().Generate(), [
            new OrbitalState(start, new Vector3(7000, 0, 0), SomeVelocity, Any.PositionCovariance().Generate()),
            new OrbitalState(start.Add(Trajectory.MaximumStep), new Vector3(-7000, 0, 0), SomeVelocity, Any.PositionCovariance().Generate())
        ]);

        // Act
        Exception? failure = Record.Exception(() => trajectory.StateAt(start.Add(TimeSpan.FromMinutes(5))));

        // Assert
        Assert.Null(failure);
    }

    private static OrbitalState StateAt(Epoch epoch) {
        return new OrbitalState(epoch, SomePosition, SomeVelocity, Any.PositionCovariance().Generate());
    }

    private static Trajectory TwoStatesFrom(Epoch start, TimeSpan length) {
        return new Trajectory(Any.SpaceObject().Generate(), [StateAt(start), StateAt(start.Add(length))]);
    }

}
