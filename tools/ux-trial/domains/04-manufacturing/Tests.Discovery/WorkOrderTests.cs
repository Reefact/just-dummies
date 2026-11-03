using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class WorkOrderTests {

    private static Machine RunningMachine() {
        Machine machine = Any.Machine().WithInitialState(MachineState.Idle).Generate();
        machine.Fire(MachineEvent.BeginSetup);
        machine.Fire(MachineEvent.Start);

        return machine;
    }

    // Constructor arguments that are valid, each a dummy; the test breaks the one it is about.

    [Fact]
    public void A_new_order_keeps_what_it_was_given_and_has_produced_nothing() {
        // Arrange
        BatchCode   anyBatch     = Any.BatchCode().Generate();
        Cadence     anyCadence   = Any.Cadence().Generate();
        Tolerance   anyTolerance = Any.Tolerance().Generate();
        ShiftWindow anyWindow    = Any.ShiftWindow().Generate();

        // Act
        WorkOrder order = new WorkOrder(anyBatch, "BRK-77", 250, anyCadence, anyTolerance, anyWindow, ["Ines", "Omar"]);

        // Assert
        Assert.Same(anyBatch, order.Batch);
        Assert.Equal("BRK-77", order.ProductSku);
        Assert.Equal(250, order.PlannedQuantity);
        Assert.Same(anyCadence, order.Cadence);
        Assert.Same(anyTolerance, order.Tolerance);
        Assert.Same(anyWindow, order.Window);
        Assert.Equal(["Ines", "Omar"], order.Operators);
        Assert.Equal(0, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
        Assert.Equal(0m, order.YieldPercent);
        Assert.False(order.IsComplete);
    }

    [Fact]
    public void The_operators_are_copied() {
        // Arrange
        List<string> operators = ["Ines", "Omar"];

        WorkOrder order = Any.WorkOrder().WithOperators(operators).Generate();

        // Act
        operators.Add("Zoe");
        operators.Clear();

        // Assert
        Assert.Equal(["Ines", "Omar"], order.Operators);
    }

    [Fact]
    public void An_order_without_a_batch_is_refused() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();

        // Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new WorkOrder(null!, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators));
        Assert.Equal("batch", exception.ParamName);
    }

    [Fact]
    public void An_order_without_a_sku_is_refused() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new WorkOrder(anyOrder.Batch, null!, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators));
        Assert.Throws<ArgumentException>(() => new WorkOrder(anyOrder.Batch, "", anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators));
    }

    [Fact]
    public void A_sku_of_twenty_characters_is_accepted_and_one_more_is_refused() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();
        string    twenty   = new string('A', 20);
        string    twentyOne = new string('A', 21);

        // Act
        WorkOrder order = Any.WorkOrder().WithProductSku(twenty).Generate();

        // Assert
        Assert.Equal(twenty, order.ProductSku);
        Assert.Throws<ArgumentException>(() => Any.WorkOrder().WithProductSku(twentyOne).Generate());
    }

    [Fact]
    public void A_blank_sku_is_not_refused() {
        // Only null and empty are: a sku of spaces passes the guard.
        // Act
        WorkOrder order = Any.WorkOrder().WithProductSku("   ").Generate();

        // Assert
        Assert.Equal("   ", order.ProductSku);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100_000)]
    public void The_two_ends_of_the_planned_quantity_are_accepted(int quantity) {
        // Act
        WorkOrder order = Any.WorkOrder().WithPlannedQuantity(quantity).Generate();

        // Assert
        Assert.Equal(quantity, order.PlannedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100_001)]
    [InlineData(int.MaxValue)]
    public void A_planned_quantity_outside_the_range_is_refused(int quantity) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.WorkOrder().WithPlannedQuantity(quantity).Generate());
    }

    [Fact]
    public void An_order_without_a_cadence_a_tolerance_or_a_window_is_refused() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();

        // Assert
        Assert.Equal("cadence", Assert.Throws<ArgumentNullException>(() => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, null!, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators)).ParamName);
        Assert.Equal("tolerance", Assert.Throws<ArgumentNullException>(() => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, null!, anyOrder.Window, anyOrder.Operators)).ParamName);
        Assert.Equal("window", Assert.Throws<ArgumentNullException>(() => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, null!, anyOrder.Operators)).ParamName);
    }

    [Fact]
    public void An_order_without_any_operator_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.WorkOrder().WithOperators([]).Generate());
    }

    [Fact(Skip = "domain defect: a null operators list fails with NullReferenceException instead of ArgumentNullException, literal input so no seed")]
    public void A_missing_list_of_operators_is_refused_as_an_argument_error() {
        // 'domain defect: a null list fails with NullReferenceException, unguarded' -- see NOTES.discovery.md
        Assert.Throws<ArgumentNullException>(() => Any.WorkOrder().WithOperators((IReadOnlyList<string>)null!).Generate());
    }

    [Fact]
    public void Operator_names_are_not_validated() {
        // Act
        WorkOrder order = Any.WorkOrder().WithOperators(["", "  ", null!]).Generate();

        // Assert
        Assert.Equal(3, order.Operators.Count);
    }

    [Fact]
    public void Checks_run_in_the_documented_order() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();
        string    tooLong  = new string('B', 21);

        // Assert: a null batch wins over a null sku, a sku problem over a quantity problem, a quantity over a missing cadence...
        Assert.Equal("batch", Assert.Throws<ArgumentNullException>(() => new WorkOrder(null!, null!, 0, null!, null!, null!, null!)).ParamName);
        Assert.Equal("productSku", Assert.Throws<ArgumentNullException>(() => new WorkOrder(anyOrder.Batch, null!, 0, null!, null!, null!, null!)).ParamName);
        Assert.Equal("productSku", Assert.Throws<ArgumentException>(() => new WorkOrder(anyOrder.Batch, tooLong, 0, null!, null!, null!, null!)).ParamName);
        Assert.Equal("plannedQuantity", Assert.Throws<ArgumentOutOfRangeException>(() => new WorkOrder(anyOrder.Batch, "SKU", 0, null!, null!, null!, null!)).ParamName);
        Assert.Equal("cadence", Assert.Throws<ArgumentNullException>(() => new WorkOrder(anyOrder.Batch, "SKU", 1, null!, null!, null!, null!)).ParamName);
    }

    [Theory]
    [InlineData(60, 90, 90)]
    [InlineData(100, 800, 480)]
    [InlineData(7, 10, 86)]
    public void The_estimated_duration_is_the_time_the_cadence_needs_for_the_planned_quantity(int unitsPerHour, int quantity, int expectedMinutes) {
        // Arrange
        WorkOrder order = Any.WorkOrder()
                             .WithCadence(new Cadence(unitsPerHour))
                             .WithPlannedQuantity(quantity)
                             .Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), order.EstimatedDuration);
    }

    [Fact]
    public void An_order_fits_in_a_window_exactly_as_long_as_its_estimate() {
        // Arrange: 800 units at 100 an hour take eight hours.
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        WorkOrder order = Any.WorkOrder()
                             .WithCadence(new Cadence(100))
                             .WithPlannedQuantity(800)
                             .WithWindow(new ShiftWindow(anyStart, anyStart.AddHours(8)))
                             .Generate();

        // Assert
        Assert.True(order.FitsInWindow);
    }

    [Fact]
    public void An_order_does_not_fit_in_a_window_a_minute_too_short() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        WorkOrder order = Any.WorkOrder()
                             .WithCadence(new Cadence(100))
                             .WithPlannedQuantity(800)
                             .WithWindow(new ShiftWindow(anyStart, anyStart.AddHours(8).AddMinutes(-1)))
                             .Generate();

        // Assert
        Assert.False(order.FitsInWindow);
    }

    [Fact]
    public void An_order_fits_in_a_roomy_window() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        WorkOrder order = Any.WorkOrder()
                             .WithCadence(new Cadence(10_000))
                             .WithPlannedQuantity(10)
                             .WithWindow(new ShiftWindow(anyStart, anyStart.AddHours(12)))
                             .Generate();

        // Assert
        Assert.True(order.FitsInWindow);
    }

    [Fact]
    public void A_part_within_tolerance_is_counted_as_good() {
        // Arrange
        WorkOrder order = Any.WorkOrder().WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, 10.05m);

        // Assert
        Assert.Equal(1, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
    }

    [Fact]
    public void A_part_out_of_tolerance_is_counted_as_scrap() {
        // Arrange
        WorkOrder order = Any.WorkOrder().WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, 10.2m);
        order.RecordPart(machine, 9.8m);

        // Assert
        Assert.Equal(0, order.GoodCount);
        Assert.Equal(2, order.ScrapCount);
    }

    [Fact]
    public void A_part_on_either_limit_is_counted_as_good() {
        // Arrange
        WorkOrder order = Any.WorkOrder().WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, 9.9m);
        order.RecordPart(machine, 10.1m);

        // Assert
        Assert.Equal(2, order.GoodCount);
    }

    [Theory]
    [InlineData(MachineState.Idle)]
    [InlineData(MachineState.Setup)]
    [InlineData(MachineState.Stopped)]
    [InlineData(MachineState.Faulted)]
    [InlineData(MachineState.Maintenance)]
    public void A_part_is_refused_unless_the_machine_is_running(MachineState state) {
        // Arrange
        WorkOrder order   = Any.WorkOrder().Generate();
        Machine   machine = MachineTests.MachineIn(state);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => order.RecordPart(machine, order.Tolerance.NominalMm));

        // Assert
        Assert.Contains(machine.MachineId, exception.Message);
        Assert.Equal(0, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
    }

    [Fact(Skip = "domain defect: RecordPart(null) fails with NullReferenceException instead of ArgumentNullException, literal input so no seed")]
    public void A_missing_machine_is_refused_as_an_argument_error() {
        // 'domain defect: a null machine fails with NullReferenceException, unguarded' -- see NOTES.discovery.md
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyOrder.RecordPart(null!, anyOrder.Tolerance.NominalMm));
    }

    [Fact]
    public void Recording_continues_beyond_the_planned_quantity_and_whatever_the_machine_id() {
        // The domain checks neither the count against the plan nor the machine's identity.
        // Arrange
        WorkOrder order   = Any.WorkOrder().WithPlannedQuantity(1).WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, 10m);
        order.RecordPart(machine, 10m);

        // Assert
        Assert.Equal(2, order.GoodCount);
    }

    [Fact]
    public void An_order_is_complete_once_it_has_as_many_good_parts_as_planned() {
        // Arrange
        WorkOrder order   = Any.WorkOrder().WithPlannedQuantity(3).WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act and assert
        order.RecordPart(machine, 10m);
        order.RecordPart(machine, 10m);
        Assert.False(order.IsComplete);

        order.RecordPart(machine, 10m);
        Assert.True(order.IsComplete);
    }

    [Fact]
    public void Scrap_does_not_bring_an_order_closer_to_completion() {
        // Arrange
        WorkOrder order   = Any.WorkOrder().WithPlannedQuantity(2).WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, 10m);
        order.RecordPart(machine, 99m);
        order.RecordPart(machine, 99m);

        // Assert
        Assert.False(order.IsComplete);
    }

    [Theory]
    [InlineData(1, 0, "100.0")]
    [InlineData(0, 1, "0.0")]
    [InlineData(1, 2, "33.3")]
    [InlineData(2, 1, "66.7")]
    [InlineData(1, 1, "50.0")]
    [InlineData(1, 7, "12.5")]
    [InlineData(1, 15, "6.3")]      // 6.25 exactly: midpoint rounds away from zero
    [InlineData(5, 11, "31.3")]     // 31.25
    [InlineData(3, 13, "18.8")]     // 18.75
    [InlineData(1, 999, "0.1")]     // 0.1
    [InlineData(1, 2000, "0.0")]    // 0.0499...
    [InlineData(199, 1, "99.5")]
    public void The_yield_is_the_good_share_in_percent_rounded_to_one_decimal(int good, int scrap, string expected) {
        // Arrange
        WorkOrder order   = Any.WorkOrder().WithTolerance(new Tolerance(10m, 0.1m, 0.1m)).Generate();
        Machine   machine = RunningMachine();

        // Act
        for (int i = 0; i < good; i++) { order.RecordPart(machine, 10m); }
        for (int i = 0; i < scrap; i++) { order.RecordPart(machine, 11m); }

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), order.YieldPercent);
    }

}
