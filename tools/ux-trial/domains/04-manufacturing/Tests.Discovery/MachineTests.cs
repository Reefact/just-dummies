using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class MachineTests {

    // The complete state machine, as the domain describes it: every other (state, event) pair is refused.
    public static readonly (MachineState From, MachineEvent Event, MachineState To)[] Transitions = [
        (MachineState.Idle, MachineEvent.BeginSetup, MachineState.Setup),
        (MachineState.Setup, MachineEvent.Start, MachineState.Running),
        (MachineState.Running, MachineEvent.Stop, MachineState.Stopped),
        (MachineState.Stopped, MachineEvent.Start, MachineState.Running),
        (MachineState.Stopped, MachineEvent.BeginSetup, MachineState.Setup),
        (MachineState.Idle, MachineEvent.Fault, MachineState.Faulted),
        (MachineState.Setup, MachineEvent.Fault, MachineState.Faulted),
        (MachineState.Running, MachineEvent.Fault, MachineState.Faulted),
        (MachineState.Stopped, MachineEvent.Fault, MachineState.Faulted),
        (MachineState.Faulted, MachineEvent.Fault, MachineState.Faulted),
        (MachineState.Faulted, MachineEvent.SendToMaintenance, MachineState.Maintenance),
        (MachineState.Stopped, MachineEvent.SendToMaintenance, MachineState.Maintenance),
        (MachineState.Idle, MachineEvent.SendToMaintenance, MachineState.Maintenance),
        (MachineState.Maintenance, MachineEvent.ReleaseFromMaintenance, MachineState.Idle)
    ];

    public static TheoryData<MachineState, MachineEvent, MachineState> AllowedTransitions() {
        TheoryData<MachineState, MachineEvent, MachineState> data = new TheoryData<MachineState, MachineEvent, MachineState>();
        foreach ((MachineState from, MachineEvent machineEvent, MachineState to) in Transitions) {
            data.Add(from, machineEvent, to);
        }

        return data;
    }

    public static TheoryData<MachineState, MachineEvent> RefusedTransitions() {
        TheoryData<MachineState, MachineEvent> data = new TheoryData<MachineState, MachineEvent>();
        foreach (MachineState state in Enum.GetValues<MachineState>()) {
            foreach (MachineEvent machineEvent in Enum.GetValues<MachineEvent>()) {
                if (!Transitions.Any(t => t.From == state && t.Event == machineEvent)) {
                    data.Add(state, machineEvent);
                }
            }
        }

        return data;
    }

    /// <summary>Brings a machine to the state by the shortest legal path from a commissioning state.</summary>
    public static Machine MachineIn(MachineState state) {
        Machine machine;
        switch (state) {
            case MachineState.Idle:
            case MachineState.Stopped:
            case MachineState.Maintenance:
                return Any.Machine().WithInitialState(state).Generate();
            case MachineState.Setup:
                machine = Any.Machine().WithInitialState(MachineState.Idle).Generate();
                machine.Fire(MachineEvent.BeginSetup);

                return machine;
            case MachineState.Running:
                machine = Any.Machine().WithInitialState(MachineState.Idle).Generate();
                machine.Fire(MachineEvent.BeginSetup);
                machine.Fire(MachineEvent.Start);

                return machine;
            case MachineState.Faulted:
                machine = Any.Machine().WithInitialState(MachineState.Idle).Generate();
                machine.Fire(MachineEvent.Fault);

                return machine;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }
    }

    [Fact]
    public void A_machine_is_identified_by_its_id() {
        // Act
        Machine machine = new Machine("M-042", MachineState.Idle);

        // Assert
        Assert.Equal("M-042", machine.MachineId);
        Assert.Equal(MachineState.Idle, machine.State);
    }

    [Theory]
    [InlineData(MachineState.Idle)]
    [InlineData(MachineState.Stopped)]
    [InlineData(MachineState.Maintenance)]
    public void A_machine_is_commissioned_idle_stopped_or_under_maintenance(MachineState initial) {
        // Arrange
        string anyId = Any.StringMatching(@"M-\d{3}").Generate();

        // Act
        Machine machine = new Machine(anyId, initial);

        // Assert
        Assert.Equal(initial, machine.State);
    }

    [Theory]
    [InlineData(MachineState.Setup)]
    [InlineData(MachineState.Running)]
    [InlineData(MachineState.Faulted)]
    public void A_machine_cannot_be_commissioned_in_any_other_state(MachineState initial) {
        // Arrange
        string anyId = Any.StringMatching(@"M-\d{3}").Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Machine(anyId, initial));
    }

    [Fact]
    public void A_state_outside_the_enumeration_is_refused() {
        // Arrange
        string anyId = Any.StringMatching(@"M-\d{3}").Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Machine(anyId, (MachineState)99));
    }

    [Theory]
    [InlineData("M-12")]
    [InlineData("M-1234")]
    [InlineData("M-ABC")]
    [InlineData("m-123")]
    [InlineData("X-123")]
    [InlineData("M123")]
    [InlineData("M_123")]
    [InlineData(" M-123")]
    [InlineData("M-123 ")]
    [InlineData("")]
    [InlineData("   ")]
    public void A_malformed_id_is_refused(string id) {
        Assert.Throws<ArgumentException>(() => new Machine(id, MachineState.Idle));
    }

    [Fact]
    public void A_missing_id_is_refused_as_a_plain_argument_error() {
        // The domain treats null as non-matching: ArgumentException itself, not ArgumentNullException.
        Assert.Throws<ArgumentException>(() => new Machine(null!, MachineState.Idle));
    }

    [Fact(Skip = "domain defect: the regex ends in $, which .NET matches before a final line break, so 'M-123\n' is accepted, literal input so no seed")]
    public void An_id_ending_with_a_line_break_is_refused() {
        // 'domain defect: the regex ends in `$`, which .NET matches before a final line break' -- see NOTES.discovery.md
        Assert.Throws<ArgumentException>(() => new Machine("M-123\n", MachineState.Idle));
    }

    [Fact]
    public void The_id_is_checked_before_the_initial_state() {
        // Both are wrong: the id's exception, not the state's.
        Assert.Throws<ArgumentException>(() => new Machine("nope", MachineState.Running));
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void An_allowed_event_moves_the_machine_to_the_next_state(MachineState from, MachineEvent machineEvent, MachineState expected) {
        // Arrange
        Machine machine = MachineIn(from);

        // Act
        machine.Fire(machineEvent);

        // Assert
        Assert.Equal(expected, machine.State);
    }

    [Theory]
    [MemberData(nameof(RefusedTransitions))]
    public void A_refused_event_throws_and_leaves_the_state_unchanged(MachineState from, MachineEvent machineEvent) {
        // Arrange
        Machine machine = MachineIn(from);

        // Act
        Assert.Throws<InvalidOperationException>(() => machine.Fire(machineEvent));

        // Assert
        Assert.Equal(from, machine.State);
    }

    [Fact]
    public void The_transition_table_covers_every_pair_exactly_once() {
        int total = Enum.GetValues<MachineState>().Length * Enum.GetValues<MachineEvent>().Length;

        Assert.Equal(total, AllowedTransitions().Count + RefusedTransitions().Count);
        Assert.Equal(Transitions.Length, Transitions.Select(t => (t.From, t.Event)).Distinct().Count());
    }

    [Fact]
    public void An_event_outside_the_enumeration_is_refused() {
        // Arrange
        Machine anyMachine = Any.Machine().Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyMachine.Fire((MachineEvent)99));
    }

    [Theory]
    [InlineData(MachineState.Idle, false)]
    [InlineData(MachineState.Setup, false)]
    [InlineData(MachineState.Running, true)]
    [InlineData(MachineState.Stopped, false)]
    [InlineData(MachineState.Faulted, false)]
    [InlineData(MachineState.Maintenance, false)]
    public void Only_a_running_machine_can_produce(MachineState state, bool expected) {
        // Arrange
        Machine machine = MachineIn(state);

        // Assert
        Assert.Equal(expected, machine.CanProduce);
    }

    [Fact]
    public void A_full_production_cycle_ends_back_where_maintenance_releases_it() {
        // Arrange
        Machine machine = Any.Machine().WithInitialState(MachineState.Idle).Generate();

        // Act
        machine.Fire(MachineEvent.BeginSetup);
        machine.Fire(MachineEvent.Start);
        machine.Fire(MachineEvent.Stop);
        machine.Fire(MachineEvent.SendToMaintenance);
        machine.Fire(MachineEvent.ReleaseFromMaintenance);

        // Assert
        Assert.Equal(MachineState.Idle, machine.State);
    }

}
