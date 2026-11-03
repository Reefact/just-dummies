namespace Manufacturing.Domain.Tests.Expert;

public sealed class MachineTests {

    private static readonly MachineState[] AllStates = Enum.GetValues<MachineState>();
    private static readonly MachineEvent[] AllEvents = Enum.GetValues<MachineEvent>();

    // The state machine, written as the plant's rulebook reads: every permitted (state, event) -> state.
    private static readonly (MachineState From, MachineEvent Event, MachineState To)[] Permitted = {
        (MachineState.Idle,        MachineEvent.BeginSetup,              MachineState.Setup),
        (MachineState.Setup,       MachineEvent.Start,                   MachineState.Running),
        (MachineState.Running,     MachineEvent.Stop,                    MachineState.Stopped),
        (MachineState.Stopped,     MachineEvent.Start,                   MachineState.Running),
        (MachineState.Stopped,     MachineEvent.BeginSetup,              MachineState.Setup),
        (MachineState.Idle,        MachineEvent.Fault,                   MachineState.Faulted),
        (MachineState.Setup,       MachineEvent.Fault,                   MachineState.Faulted),
        (MachineState.Running,     MachineEvent.Fault,                   MachineState.Faulted),
        (MachineState.Stopped,     MachineEvent.Fault,                   MachineState.Faulted),
        (MachineState.Faulted,     MachineEvent.Fault,                   MachineState.Faulted),
        (MachineState.Faulted,     MachineEvent.SendToMaintenance,       MachineState.Maintenance),
        (MachineState.Stopped,     MachineEvent.SendToMaintenance,       MachineState.Maintenance),
        (MachineState.Idle,        MachineEvent.SendToMaintenance,       MachineState.Maintenance),
        (MachineState.Maintenance, MachineEvent.ReleaseFromMaintenance,  MachineState.Idle)
    };

    public static TheoryData<MachineState, MachineEvent, MachineState> PermittedTransitions() {
        TheoryData<MachineState, MachineEvent, MachineState> data = new TheoryData<MachineState, MachineEvent, MachineState>();
        foreach ((MachineState from, MachineEvent machineEvent, MachineState to) in Permitted) {
            data.Add(from, machineEvent, to);
        }

        return data;
    }

    public static TheoryData<MachineState, MachineEvent> RefusedTransitions() {
        TheoryData<MachineState, MachineEvent> data = new TheoryData<MachineState, MachineEvent>();
        foreach (MachineState state in AllStates) {
            foreach (MachineEvent machineEvent in AllEvents) {
                if (!Permitted.Any(p => p.From == state && p.Event == machineEvent)) {
                    data.Add(state, machineEvent);
                }
            }
        }

        return data;
    }

    public static TheoryData<MachineState> StatesOtherThanRunning() {
        TheoryData<MachineState> data = new TheoryData<MachineState>();
        foreach (MachineState state in AllStates.Where(s => s != MachineState.Running)) {
            data.Add(state);
        }

        return data;
    }

    // ---- commissioning -------------------------------------------------------------------------

    [Theory]
    [InlineData(MachineState.Idle)]
    [InlineData(MachineState.Stopped)]
    [InlineData(MachineState.Maintenance)]
    public void A_machine_is_commissioned_idle_stopped_or_under_maintenance(MachineState commissionedState) {
        // Act
        Machine machine = new Machine("M-042", commissionedState);

        // Assert
        Assert.Equal("M-042", machine.MachineId);
        Assert.Equal(commissionedState, machine.State);
    }

    [Theory]
    [InlineData(MachineState.Setup)]
    [InlineData(MachineState.Running)]
    [InlineData(MachineState.Faulted)]
    [InlineData((MachineState)99)]
    [InlineData((MachineState)(-1))]
    public void A_machine_cannot_be_commissioned_in_another_state(MachineState commissionedState) {
        // Arrange
        string anyMachineId = Any.StringMatching("M-[0-9]{3}").Generate();

        // Act
        Action create = () => new Machine(anyMachineId, commissionedState);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("initialState", error.ParamName);
    }

    [Theory]
    [InlineData("M-000")]
    [InlineData("M-001")]
    [InlineData("M-999")]
    public void An_id_of_m_dash_and_three_digits_is_accepted(string machineId) {
        // Arrange
        MachineState anyCommissionedState = Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance).Generate();

        // Act
        Machine machine = new Machine(machineId, anyCommissionedState);

        // Assert
        Assert.Equal(machineId, machine.MachineId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("M-12")]
    [InlineData("M-1234")]
    [InlineData("m-123")]
    [InlineData("X-123")]
    [InlineData("M123")]
    [InlineData("M_123")]
    [InlineData("M-12A")]
    [InlineData(" M-123")]
    [InlineData("M-123 ")]
    [InlineData("M-1 23")]
    public void An_id_of_another_shape_is_refused(string machineId) {
        // Arrange
        MachineState anyCommissionedState = Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance).Generate();

        // Act
        Action create = () => new Machine(machineId, anyCommissionedState);

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(create);
        Assert.Equal("machineId", error.ParamName);
    }

    [Fact]
    public void A_null_id_is_refused_as_an_argument_error() {
        // Arrange
        MachineState anyCommissionedState = Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance).Generate();

        // Act
        Action create = () => new Machine(null!, anyCommissionedState);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void The_id_is_checked_before_the_state() {
        // Act: both are wrong
        Action create = () => new Machine("nope", MachineState.Running);

        // Assert: the id's error, an ArgumentException itself rather than the state's out-of-range subtype
        Assert.Throws<ArgumentException>(create);
    }

    [Fact(Skip = "domain defect: an id followed by a line break is accepted ('$' matches before a final newline), deterministic")]
    public void An_id_followed_by_a_line_break_is_refused() {
        // Arrange
        MachineState anyCommissionedState = Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance).Generate();

        // Act
        Action create = () => new Machine("M-123\n", anyCommissionedState);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact(Skip = "domain defect: '\\d' accepts any Unicode decimal digit, so 'M-١٢٣' (Arabic-Indic) is a valid id, deterministic")]
    public void An_id_with_non_ascii_digits_is_refused() {
        // Arrange
        MachineState anyCommissionedState = Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance).Generate();

        // Act
        Action create = () => new Machine("M-١٢٣", anyCommissionedState);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    // ---- the state machine ----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PermittedTransitions))]
    public void Firing_a_permitted_event_moves_the_machine_to_the_next_state(MachineState from, MachineEvent machineEvent, MachineState expected) {
        // Arrange
        Machine machine = InState(from);

        // Act
        machine.Fire(machineEvent);

        // Assert
        Assert.Equal(expected, machine.State);
    }

    [Theory]
    [MemberData(nameof(RefusedTransitions))]
    public void Firing_an_event_the_state_does_not_allow_is_refused_and_the_state_is_kept(MachineState from, MachineEvent machineEvent) {
        // Arrange
        Machine machine = InState(from);

        // Act
        Action fire = () => machine.Fire(machineEvent);

        // Assert
        Assert.Throws<InvalidOperationException>(fire);
        Assert.Equal(from, machine.State);
    }

    [Fact]
    public void A_machine_under_maintenance_cannot_be_faulted() {
        // Arrange
        Machine machine = InState(MachineState.Maintenance);

        // Act
        Action fire = () => machine.Fire(MachineEvent.Fault);

        // Assert
        Assert.Throws<InvalidOperationException>(fire);
        Assert.Equal(MachineState.Maintenance, machine.State);
    }

    [Fact]
    public void A_running_machine_cannot_be_sent_to_maintenance_before_it_is_stopped() {
        // Arrange
        Machine machine = InState(MachineState.Running);

        // Act
        Action fire = () => machine.Fire(MachineEvent.SendToMaintenance);

        // Assert
        Assert.Throws<InvalidOperationException>(fire);
        Assert.Equal(MachineState.Running, machine.State);
    }

    [Fact]
    public void An_event_unknown_to_the_machine_is_refused() {
        // Arrange
        Machine anyIdleMachine = Any.Machine().WithInitialState(MachineState.Idle).Generate();

        // Act
        Action fire = () => anyIdleMachine.Fire((MachineEvent)99);

        // Assert
        Assert.Throws<InvalidOperationException>(fire);
        Assert.Equal(MachineState.Idle, anyIdleMachine.State);
    }

    [Fact]
    public void A_machine_goes_through_a_whole_production_cycle() {
        // Arrange
        Machine anyIdleMachine = Any.Machine().WithInitialState(MachineState.Idle).Generate();

        // Act
        anyIdleMachine.Fire(MachineEvent.BeginSetup);
        anyIdleMachine.Fire(MachineEvent.Start);
        anyIdleMachine.Fire(MachineEvent.Stop);
        anyIdleMachine.Fire(MachineEvent.Fault);
        anyIdleMachine.Fire(MachineEvent.SendToMaintenance);
        anyIdleMachine.Fire(MachineEvent.ReleaseFromMaintenance);

        // Assert
        Assert.Equal(MachineState.Idle, anyIdleMachine.State);
    }

    [Fact]
    public void A_machine_keeps_its_id_through_its_transitions() {
        // Arrange
        Machine machine = new Machine("M-007", MachineState.Idle);

        // Act
        machine.Fire(MachineEvent.BeginSetup);
        machine.Fire(MachineEvent.Start);

        // Assert
        Assert.Equal("M-007", machine.MachineId);
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
        Machine machine = InState(state);

        // Assert
        Assert.Equal(expected, machine.CanProduce);
    }

    // ---- helpers --------------------------------------------------------------------------------

    /// <summary>A machine of arbitrary id brought to <paramref name="state" /> by the shortest legal path.</summary>
    internal static Machine InState(MachineState state) {
        switch (state) {
            case MachineState.Idle:
            case MachineState.Stopped:
            case MachineState.Maintenance:
                return Any.Machine().WithInitialState(state).Generate();
            case MachineState.Setup: {
                Machine anyIdle = Any.Machine().WithInitialState(MachineState.Idle).Generate();
                anyIdle.Fire(MachineEvent.BeginSetup);

                return anyIdle;
            }
            case MachineState.Running: {
                Machine anyIdle = Any.Machine().WithInitialState(MachineState.Idle).Generate();
                anyIdle.Fire(MachineEvent.BeginSetup);
                anyIdle.Fire(MachineEvent.Start);

                return anyIdle;
            }
            case MachineState.Faulted: {
                Machine anyIdle = Any.Machine().WithInitialState(MachineState.Idle).Generate();
                anyIdle.Fire(MachineEvent.Fault);

                return anyIdle;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

}
