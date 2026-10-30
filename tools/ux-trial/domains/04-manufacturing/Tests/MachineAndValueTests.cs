using JustDummies;
using JustDummies.Xunit;
using Manufacturing.Domain;
using Xunit;

namespace Manufacturing.Tests;

[Reproducible]
public sealed class MachineAndValueTests {

    [Fact]
    public void An_idle_machine_set_up_then_started_is_running() {
        // Arrange
        Machine anyIdleMachine = Arbitrary.Machine().WithInitialState(MachineState.Idle).Generate();

        // Act
        anyIdleMachine.Fire(MachineEvent.BeginSetup);
        anyIdleMachine.Fire(MachineEvent.Start);

        // Assert
        Assert.Equal(MachineState.Running, anyIdleMachine.State);
    }

    [Fact]
    public void A_machine_under_maintenance_cannot_fault() {
        // Arrange
        Machine anyMachineInMaintenance = Arbitrary.Machine().WithInitialState(MachineState.Maintenance).Generate();

        // Act
        Action faulting = () => anyMachineInMaintenance.Fire(MachineEvent.Fault);

        // Assert
        Assert.Throws<InvalidOperationException>(faulting);
    }

    [Fact]
    public void A_machine_cannot_be_commissioned_running() {
        // Arrange
        string anyMachineId = Any.StringMatching(@"M-\d{3}").Generate();

        // Act
        Action commissioning = () => new Machine(anyMachineId, MachineState.Running);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("initialState", commissioning);
    }

    [Fact]
    public void Producing_one_unit_at_seven_an_hour_takes_nine_whole_minutes() {
        // Arrange
        Cadence sevenAnHour = new Cadence(7);

        // Act
        TimeSpan time = sevenAnHour.TimeToProduce(1);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(9), time);
    }

    [Fact]
    public void A_45_second_cycle_makes_80_parts_an_hour() {
        // Arrange
        CycleTime fortyFiveSeconds = new CycleTime(TimeSpan.FromSeconds(45));

        // Act
        int partsPerHour = fortyFiveSeconds.PartsPerHour;

        // Assert
        Assert.Equal(80, partsPerHour);
    }

    [Fact]
    public void A_batch_code_exposes_its_plant_and_week() {
        // Arrange
        BatchCode batch = new BatchCode("LYN-2614-0042");

        // Act
        (string plant, int week) = (batch.Plant, batch.Week);

        // Assert
        Assert.Equal(("LYN", 14), (plant, week));
    }

    [Fact]
    public void A_minus_deviation_reaching_the_nominal_is_refused() {
        // Arrange
        decimal anyPlusDeviation = Any.Decimal().Between(0m, 1m).WithScale(3).Generate();

        // Act
        Action creating = () => new Tolerance(1.000m, 1.000m, anyPlusDeviation);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

    [Fact]
    public void A_cycle_with_a_fraction_of_a_second_is_refused() {
        // Arrange
        TimeSpan anyWholeSeconds = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(59)).WithGranularity(TimeSpan.FromSeconds(1)).Generate();

        // Act
        Action creating = () => new CycleTime(anyWholeSeconds + TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.Throws<ArgumentException>("duration", creating);
    }

    [Fact]
    public void Operators_drawn_from_the_roster_do_not_change_the_estimate() {
        // Arrange
        string[]      roster       = ["B-1042", "B-1077", "B-2210", "B-3001"];
        List<string>  anyOperators = Any.ListOf(Any.ElementOf(roster)).WithCountBetween(1, 3).Distinct().Generate();
        WorkOrder     anyOrder     = Any.WorkOrder()
                                        .WithOperators(anyOperators)
                                        .WithPlannedQuantity(300)
                                        .WithCadence(new Cadence(100))
                                        .Generate();

        // Act
        TimeSpan estimate = anyOrder.EstimatedDuration;

        // Assert
        Assert.Equal(TimeSpan.FromHours(3), estimate);
    }

}
