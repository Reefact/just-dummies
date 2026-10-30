using JustDummies;
using JustDummies.Xunit;
using Manufacturing.Domain;
using Xunit;

namespace Manufacturing.Tests;

[Reproducible]
public sealed class WorkOrderTests {

    private static readonly Tolerance TenMillimetresPlusOrMinusFiftyMicrons = new Tolerance(10.000m, 0.050m, 0.050m);

    private static Machine ARunningMachine() {
        Machine machine = Arbitrary.Machine().WithInitialState(MachineState.Idle).Generate();
        machine.Fire(MachineEvent.BeginSetup);
        machine.Fire(MachineEvent.Start);

        return machine;
    }

    [Fact]
    public void Planned_quantity_over_cadence_gives_the_estimated_duration() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder()
                                .WithPlannedQuantity(1_200)
                                .WithCadence(new Cadence(600))
                                .Generate();

        // Act
        TimeSpan estimate = anyOrder.EstimatedDuration;

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), estimate);
    }

    [Fact]
    public void An_order_fits_a_window_exactly_as_long_as_its_estimate() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder()
                                .WithPlannedQuantity(1_200)
                                .WithCadence(new Cadence(600))
                                .WithWindow(w => w.WithLength(TimeSpan.FromHours(2)))
                                .Generate();

        // Act
        bool fits = anyOrder.FitsInWindow;

        // Assert
        Assert.True(fits);
    }

    [Fact]
    public void One_unit_more_than_the_window_holds_no_longer_fits() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder()
                                .WithPlannedQuantity(1_201)
                                .WithCadence(new Cadence(600))
                                .WithWindow(w => w.WithLength(TimeSpan.FromHours(2)))
                                .Generate();

        // Act
        bool fits = anyOrder.FitsInWindow;

        // Assert
        Assert.False(fits);
    }

    [Fact]
    public void A_part_measured_inside_the_tolerance_counts_as_good() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().WithTolerance(TenMillimetresPlusOrMinusFiftyMicrons).Generate();

        // Act
        anyOrder.RecordPart(ARunningMachine(), 10.020m);

        // Assert
        Assert.Equal((1, 0), (anyOrder.GoodCount, anyOrder.ScrapCount));
    }

    [Fact]
    public void A_part_measured_outside_the_tolerance_is_scrapped() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().WithTolerance(TenMillimetresPlusOrMinusFiftyMicrons).Generate();

        // Act
        anyOrder.RecordPart(ARunningMachine(), 9.949m);

        // Assert
        Assert.Equal((0, 1), (anyOrder.GoodCount, anyOrder.ScrapCount));
    }

    [Fact]
    public void A_part_on_the_upper_limit_is_still_good() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().WithTolerance(TenMillimetresPlusOrMinusFiftyMicrons).Generate();

        // Act
        anyOrder.RecordPart(ARunningMachine(), 10.050m);

        // Assert
        Assert.Equal(1, anyOrder.GoodCount);
    }

    [Fact]
    public void Two_good_parts_out_of_three_yield_66_7_percent() {
        // Arrange
        WorkOrder anyOrder   = Any.WorkOrder().WithTolerance(TenMillimetresPlusOrMinusFiftyMicrons).Generate();
        Machine   runningOne = ARunningMachine();

        // Act
        anyOrder.RecordPart(runningOne, 10.000m);
        anyOrder.RecordPart(runningOne, 10.010m);
        anyOrder.RecordPart(runningOne, 10.500m);

        // Assert
        Assert.Equal(66.7m, anyOrder.YieldPercent);
    }

    [Fact]
    public void An_order_reaching_its_planned_good_count_is_complete() {
        // Arrange
        WorkOrder anyOrder   = Any.WorkOrder().WithPlannedQuantity(2).WithTolerance(TenMillimetresPlusOrMinusFiftyMicrons).Generate();
        Machine   runningOne = ARunningMachine();

        // Act
        anyOrder.RecordPart(runningOne, 10.000m);
        anyOrder.RecordPart(runningOne, 10.000m);

        // Assert
        Assert.True(anyOrder.IsComplete);
    }

    [Fact]
    public void A_machine_that_is_not_running_cannot_record_a_part() {
        // Arrange
        WorkOrder anyOrder         = Any.WorkOrder().Generate();
        Machine   anyStoppedMachine = Arbitrary.Machine().WithInitialState(MachineState.Stopped).Generate();
        decimal   anyMeasurement   = Any.Decimal().Between(0.001m, 5_000m).WithScale(3).Generate();

        // Act
        Action recording = () => anyOrder.RecordPart(anyStoppedMachine, anyMeasurement);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void A_work_order_planning_zero_units_is_refused() {
        // Arrange
        AnyWorkOrder anyOrderOfNothing = Any.WorkOrder().WithPlannedQuantity(0);

        // Act
        Func<WorkOrder> creating = anyOrderOfNothing.Generate;

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("plannedQuantity", () => creating());
    }

}
