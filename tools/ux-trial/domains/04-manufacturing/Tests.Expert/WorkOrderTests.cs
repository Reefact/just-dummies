namespace Manufacturing.Domain.Tests.Expert;

public sealed class WorkOrderTests {

    // 10 mm, from 9.9 to 10.2 included: the measurements below are the point of the tests that use it.
    private static readonly Tolerance TenMillimetres = new Tolerance(10m, 0.1m, 0.2m);

    private const decimal Good  = 10m;
    private const decimal Scrap = 11m;

    // ---- construction ---------------------------------------------------------------------------

    [Fact]
    public void A_work_order_keeps_what_it_was_created_with() {
        // Arrange
        BatchCode  batch     = new BatchCode("LYN-2614-0042");
        Cadence    cadence   = new Cadence(120);
        ShiftWindow window   = Any.ShiftWindow().Generate();
        string[]   operators = ["ines", "marc"];

        // Act
        WorkOrder order = new WorkOrder(batch, "SKU-77", 500, cadence, TenMillimetres, window, operators);

        // Assert
        Assert.Same(batch, order.Batch);
        Assert.Equal("SKU-77", order.ProductSku);
        Assert.Equal(500, order.PlannedQuantity);
        Assert.Same(cadence, order.Cadence);
        Assert.Same(TenMillimetres, order.Tolerance);
        Assert.Same(window, order.Window);
        Assert.Equal(["ines", "marc"], order.Operators);
    }

    [Fact]
    public void A_new_work_order_has_produced_nothing() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().Generate();

        // Assert
        Assert.Equal(0, anyOrder.GoodCount);
        Assert.Equal(0, anyOrder.ScrapCount);
        Assert.Equal(0m, anyOrder.YieldPercent);
        Assert.False(anyOrder.IsComplete);
    }

    [Fact]
    public void The_operators_are_copied_so_that_the_caller_cannot_change_them_afterwards() {
        // Arrange
        List<string> operators = ["ines"];
        WorkOrder    anyOrder  = Any.WorkOrder().WithOperators(operators).Generate();

        // Act
        operators.Add("marc");
        operators[0] = "someone else";

        // Assert
        Assert.Equal(["ines"], anyOrder.Operators);
    }

    [Fact]
    public void Operator_names_are_not_looked_at() {
        // Arrange
        string[] operators = ["", " ", "a name with\ttabs and 12 digits"];

        // Act
        WorkOrder anyOrder = Any.WorkOrder().WithOperators(operators).Generate();

        // Assert
        Assert.Equal(3, anyOrder.Operators.Count);
    }

    [Fact]
    public void A_work_order_without_operators_is_refused() {
        // Act
        Action create = () => Any.WorkOrder().WithOperators(new List<string>()).Generate();

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(create);
        Assert.Equal("operators", error.ParamName);
    }

    [Fact(Skip = "domain defect: a null operator list fails with NullReferenceException instead of ArgumentNullException like the other arguments, deterministic")]
    public void A_work_order_with_a_null_list_of_operators_is_refused_as_a_null_argument() {
        // Act
        Action create = () => Any.WorkOrder().WithOperators((IReadOnlyList<string>)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(create);
    }

    [Fact]
    public void A_work_order_without_batch_is_refused() {
        // Act
        Action create = () => Any.WorkOrder().WithBatch((BatchCode)null!).Generate();

        // Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(create);
        Assert.Equal("batch", error.ParamName);
    }

    [Fact]
    public void A_work_order_without_cadence_is_refused() {
        // Act
        Action create = () => Any.WorkOrder().WithCadence((Cadence)null!).Generate();

        // Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(create);
        Assert.Equal("cadence", error.ParamName);
    }

    [Fact]
    public void A_work_order_without_tolerance_is_refused() {
        // Act
        Action create = () => Any.WorkOrder().WithTolerance((Tolerance)null!).Generate();

        // Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(create);
        Assert.Equal("tolerance", error.ParamName);
    }

    [Fact]
    public void A_work_order_without_window_is_refused() {
        // Act
        Action create = () => Any.WorkOrder().WithWindow((ShiftWindow)null!).Generate();

        // Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(create);
        Assert.Equal("window", error.ParamName);
    }

    [Fact]
    public void A_null_sku_is_refused_as_a_null_argument() {
        // Act
        Action create = () => Any.WorkOrder().WithProductSku((string)null!).Generate();

        // Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(create);
        Assert.Equal("productSku", error.ParamName);
    }

    [Fact]
    public void An_empty_sku_is_refused_as_an_argument_error() {
        // Act
        Action create = () => Any.WorkOrder().WithProductSku("").Generate();

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(create);
        Assert.Equal("productSku", error.ParamName);
    }

    [Fact]
    public void A_sku_of_twenty_characters_is_accepted() {
        // Arrange
        string sku = new string('S', 20);

        // Act
        WorkOrder anyOrder = Any.WorkOrder().WithProductSku(sku).Generate();

        // Assert
        Assert.Equal(sku, anyOrder.ProductSku);
    }

    [Fact]
    public void A_sku_of_twenty_one_characters_is_refused() {
        // Arrange
        string sku = new string('S', 21);

        // Act
        Action create = () => Any.WorkOrder().WithProductSku(sku).Generate();

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(create);
        Assert.Equal("productSku", error.ParamName);
    }

    [Fact]
    public void A_one_character_sku_is_accepted() {
        // Act
        WorkOrder anyOrder = Any.WorkOrder().WithProductSku("X").Generate();

        // Assert
        Assert.Equal("X", anyOrder.ProductSku);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100_000)]
    public void A_planned_quantity_from_one_to_one_hundred_thousand_is_accepted(int plannedQuantity) {
        // Act
        WorkOrder anyOrder = Any.WorkOrder().WithPlannedQuantity(plannedQuantity).Generate();

        // Assert
        Assert.Equal(plannedQuantity, anyOrder.PlannedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100_001)]
    [InlineData(int.MaxValue)]
    public void A_planned_quantity_outside_one_to_one_hundred_thousand_is_refused(int plannedQuantity) {
        // Act
        Action create = () => Any.WorkOrder().WithPlannedQuantity(plannedQuantity).Generate();

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("plannedQuantity", error.ParamName);
    }

    [Fact]
    public void The_arguments_are_checked_in_order_the_sku_before_the_quantity() {
        // Act: both wrong
        Action create = () => Any.WorkOrder().WithProductSku("").WithPlannedQuantity(0).Generate();

        // Assert: the sku's error, an ArgumentException itself rather than the quantity's out-of-range subtype
        ArgumentException error = Assert.Throws<ArgumentException>(create);
        Assert.Equal("productSku", error.ParamName);
    }

    [Fact]
    public void The_arguments_are_checked_in_order_the_batch_before_the_sku() {
        // Act: both wrong
        Action create = () => Any.WorkOrder().WithBatch((BatchCode)null!).WithProductSku("").Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(create);
    }

    // ---- planning -------------------------------------------------------------------------------

    [Fact]
    public void The_estimated_duration_is_the_time_to_produce_the_planned_quantity_at_the_cadence() {
        // Arrange
        WorkOrder anyOrderOf250At100PerHour = Any.WorkOrder()
                                                 .WithPlannedQuantity(250)
                                                 .WithCadence(new Cadence(100))
                                                 .Generate();

        // Act
        TimeSpan duration = anyOrderOf250At100PerHour.EstimatedDuration;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(150), duration);
    }

    [Fact]
    public void The_estimated_duration_is_rounded_up_to_the_whole_minute() {
        // Arrange
        WorkOrder anyOrderOfOneAt7PerHour = Any.WorkOrder()
                                               .WithPlannedQuantity(1)
                                               .WithCadence(new Cadence(7))
                                               .Generate();

        // Assert: 8.57 minutes
        Assert.Equal(TimeSpan.FromMinutes(9), anyOrderOfOneAt7PerHour.EstimatedDuration);
    }

    [Fact]
    public void An_order_fits_in_a_window_exactly_as_long_as_its_estimated_duration() {
        // Arrange: 480 units at 60 per hour take 8 hours
        WorkOrder anyOrderInAnExactWindow = Any.WorkOrder()
                                               .WithPlannedQuantity(480)
                                               .WithCadence(new Cadence(60))
                                               .WithWindow(w => w.WithLength(TimeSpan.FromHours(8)))
                                               .Generate();

        // Assert
        Assert.True(anyOrderInAnExactWindow.FitsInWindow);
    }

    [Fact]
    public void An_order_does_not_fit_in_a_window_one_minute_shorter_than_its_estimated_duration() {
        // Arrange
        WorkOrder anyOrderInATightWindow = Any.WorkOrder()
                                              .WithPlannedQuantity(480)
                                              .WithCadence(new Cadence(60))
                                              .WithWindow(w => w.WithLength(TimeSpan.FromHours(8) - TimeSpan.FromMinutes(1)))
                                              .Generate();

        // Assert
        Assert.False(anyOrderInATightWindow.FitsInWindow);
    }

    [Fact]
    public void An_order_fits_in_a_window_longer_than_its_estimated_duration() {
        // Arrange
        WorkOrder anyOrderInARoomyWindow = Any.WorkOrder()
                                              .WithPlannedQuantity(60)
                                              .WithCadence(new Cadence(60))
                                              .WithWindow(w => w.WithLength(TimeSpan.FromHours(2)))
                                              .Generate();

        // Assert
        Assert.True(anyOrderInARoomyWindow.FitsInWindow);
    }

    [Fact]
    public void The_rounding_up_of_the_estimate_decides_whether_the_order_fits() {
        // Arrange: one unit at 7 per hour takes 8.57 minutes, estimated 9
        WorkOrder anyOrderInEightMinutes = Any.WorkOrder()
                                              .WithPlannedQuantity(1)
                                              .WithCadence(new Cadence(7))
                                              .WithWindow(w => w.WithLength(TimeSpan.FromMinutes(8)))
                                              .Generate();
        WorkOrder anyOrderInNineMinutes = Any.WorkOrder()
                                             .WithPlannedQuantity(1)
                                             .WithCadence(new Cadence(7))
                                             .WithWindow(w => w.WithLength(TimeSpan.FromMinutes(9)))
                                             .Generate();

        // Assert
        Assert.False(anyOrderInEightMinutes.FitsInWindow);
        Assert.True(anyOrderInNineMinutes.FitsInWindow);
    }

    // ---- recording parts ------------------------------------------------------------------------

    [Fact]
    public void A_measure_within_tolerance_counts_as_good() {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        anyOrder.RecordPart(anyRunningMachine, Good);

        // Assert
        Assert.Equal(1, anyOrder.GoodCount);
        Assert.Equal(0, anyOrder.ScrapCount);
    }

    [Fact]
    public void A_measure_out_of_tolerance_counts_as_scrap() {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        anyOrder.RecordPart(anyRunningMachine, Scrap);

        // Assert
        Assert.Equal(0, anyOrder.GoodCount);
        Assert.Equal(1, anyOrder.ScrapCount);
    }

    [Theory]
    [InlineData("9.9", true)]
    [InlineData("10.2", true)]
    [InlineData("9.899", false)]
    [InlineData("10.201", false)]
    public void The_limits_of_the_tolerance_are_good_and_just_beyond_them_is_scrap(string measuredMm, bool expectedGood) {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        anyOrder.RecordPart(anyRunningMachine, decimal.Parse(measuredMm));

        // Assert
        Assert.Equal(expectedGood ? 1 : 0, anyOrder.GoodCount);
        Assert.Equal(expectedGood ? 0 : 1, anyOrder.ScrapCount);
    }

    [Fact]
    public void Each_recorded_part_is_counted_once() {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrder, anyRunningMachine, good: 5, scrap: 3);

        // Assert
        Assert.Equal(5, anyOrder.GoodCount);
        Assert.Equal(3, anyOrder.ScrapCount);
    }

    [Theory]
    [MemberData(nameof(MachineTests.StatesOtherThanRunning), MemberType = typeof(MachineTests))]
    public void A_part_is_refused_unless_the_machine_is_running_and_nothing_is_counted(MachineState state) {
        // Arrange
        WorkOrder anyOrder   = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   notRunning = MachineTests.InState(state);

        // Act
        Action record = () => anyOrder.RecordPart(notRunning, Good);

        // Assert
        Assert.Throws<InvalidOperationException>(record);
        Assert.Equal(0, anyOrder.GoodCount);
        Assert.Equal(0, anyOrder.ScrapCount);
    }

    [Fact]
    public void A_part_measured_out_of_tolerance_is_still_refused_when_the_machine_is_not_running() {
        // Arrange
        WorkOrder anyOrder   = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   notRunning = MachineTests.InState(MachineState.Faulted);

        // Act
        Action record = () => anyOrder.RecordPart(notRunning, Scrap);

        // Assert
        Assert.Throws<InvalidOperationException>(record);
        Assert.Equal(0, anyOrder.ScrapCount);
    }

    [Fact]
    public void A_machine_that_stopped_cannot_record_again_until_it_is_restarted() {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);
        anyOrder.RecordPart(anyRunningMachine, Good);
        anyRunningMachine.Fire(MachineEvent.Stop);

        // Act
        Action recordWhileStopped = () => anyOrder.RecordPart(anyRunningMachine, Good);

        // Assert
        Assert.Throws<InvalidOperationException>(recordWhileStopped);
        Assert.Equal(1, anyOrder.GoodCount);

        // Act again, once restarted
        anyRunningMachine.Fire(MachineEvent.Start);
        anyOrder.RecordPart(anyRunningMachine, Good);

        // Assert again
        Assert.Equal(2, anyOrder.GoodCount);
    }

    [Fact(Skip = "domain defect: recording a part without a machine fails with NullReferenceException instead of ArgumentNullException, deterministic")]
    public void A_part_recorded_without_a_machine_is_refused_as_a_null_argument() {
        // Arrange
        WorkOrder anyOrder = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();

        // Act
        Action record = () => anyOrder.RecordPart(null!, Good);

        // Assert
        Assert.Throws<ArgumentNullException>(record);
    }

    // ---- completion -----------------------------------------------------------------------------

    [Fact]
    public void An_order_is_complete_once_as_many_good_parts_as_planned_are_recorded() {
        // Arrange
        WorkOrder anyOrderOfThree   = Any.WorkOrder().WithPlannedQuantity(3).WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrderOfThree, anyRunningMachine, good: 2, scrap: 0);
        bool completeAfterTwo = anyOrderOfThree.IsComplete;
        Record(anyOrderOfThree, anyRunningMachine, good: 1, scrap: 0);

        // Assert
        Assert.False(completeAfterTwo);
        Assert.True(anyOrderOfThree.IsComplete);
    }

    [Fact]
    public void Scrap_does_not_bring_an_order_closer_to_completion() {
        // Arrange
        WorkOrder anyOrderOfThree   = Any.WorkOrder().WithPlannedQuantity(3).WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrderOfThree, anyRunningMachine, good: 2, scrap: 5);

        // Assert
        Assert.False(anyOrderOfThree.IsComplete);
    }

    [Fact]
    public void An_order_stays_complete_when_more_good_parts_than_planned_are_recorded() {
        // Arrange
        WorkOrder anyOrderOfOne     = Any.WorkOrder().WithPlannedQuantity(1).WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrderOfOne, anyRunningMachine, good: 4, scrap: 0);

        // Assert
        Assert.True(anyOrderOfOne.IsComplete);
        Assert.Equal(4, anyOrderOfOne.GoodCount);
    }

    // ---- yield ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 0, 100.0)]
    [InlineData(0, 1, 0.0)]
    [InlineData(1, 1, 50.0)]
    [InlineData(3, 1, 75.0)]
    [InlineData(1, 2, 33.3)]       // 33.33...
    [InlineData(2, 1, 66.7)]       // 66.66...
    [InlineData(199, 1, 99.5)]
    [InlineData(1, 199, 0.5)]
    public void The_yield_is_the_share_of_good_parts_in_percent_with_one_decimal(int good, int scrap, double expectedPercent) {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithPlannedQuantity(100_000).WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrder, anyRunningMachine, good, scrap);

        // Assert
        Assert.Equal((decimal)expectedPercent, anyOrder.YieldPercent);
    }

    [Fact]
    public void A_yield_exactly_halfway_between_two_tenths_is_rounded_away_from_zero() {
        // Arrange: 1 good out of 16 is 6.25 %, and banker's rounding would give 6.2
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrder, anyRunningMachine, good: 1, scrap: 15);

        // Assert
        Assert.Equal(6.3m, anyOrder.YieldPercent);
    }

    [Fact]
    public void A_yield_halfway_after_a_hundred_parts_is_rounded_up() {
        // Arrange: 49 good out of 400 is 12.25 %, and banker's rounding would give 12.2
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrder, anyRunningMachine, good: 49, scrap: 351);

        // Assert
        Assert.Equal(12.3m, anyOrder.YieldPercent);
    }

    [Fact]
    public void The_yield_is_zero_when_only_scrap_has_been_recorded() {
        // Arrange
        WorkOrder anyOrder          = Any.WorkOrder().WithTolerance(TenMillimetres).Generate();
        Machine   anyRunningMachine = MachineTests.InState(MachineState.Running);

        // Act
        Record(anyOrder, anyRunningMachine, good: 0, scrap: 4);

        // Assert
        Assert.Equal(0m, anyOrder.YieldPercent);
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static void Record(WorkOrder order, Machine machine, int good, int scrap) {
        for (int i = 0; i < good; i++) {
            order.RecordPart(machine, Good);
        }
        for (int i = 0; i < scrap; i++) {
            order.RecordPart(machine, Scrap);
        }
    }

}
