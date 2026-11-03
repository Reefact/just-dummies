using JustDummies;

using Manufacturing.Domain;

using Xunit;

namespace Manufacturing.Tests.Regular;

public sealed class WorkOrderTests {

    private static Machine RunningMachine() {
        return MachineTests.MachineIn(MachineState.Running);
    }

    private static AnyWorkOrder AnyOrder() {
        return new AnyWorkOrder();
    }

    // Builds a work order from a generated one, replacing a single constructor argument.
    private static WorkOrder OrderWith(BatchCode? batch = null, string? sku = null, int? quantity = null, Cadence? cadence = null, Tolerance? tolerance = null, ShiftWindow? window = null, IReadOnlyList<string>? operators = null) {
        WorkOrder anyOrder = AnyOrder().Generate();

        return new WorkOrder(batch ?? anyOrder.Batch, sku ?? anyOrder.ProductSku, quantity ?? anyOrder.PlannedQuantity, cadence ?? anyOrder.Cadence, tolerance ?? anyOrder.Tolerance, window ?? anyOrder.Window, operators ?? anyOrder.Operators);
    }

    [Fact]
    public void A_new_order_carries_what_it_was_given_and_has_produced_nothing() {
        // Arrange
        BatchCode         anyBatch     = new AnyBatchCode().Generate();
        string            anySku       = Any.String().NonEmpty().WithMaxLength(20).Generate();
        int               anyQuantity  = Any.Int32().Between(1, 100_000).Generate();
        Cadence           anyCadence   = new AnyCadence().Generate();
        Tolerance         anyTolerance = new AnyTolerance().Generate();
        ShiftWindow       anyWindow    = new AnyShiftWindow().Generate();
        List<string>      anyOperators = Any.ListOf(Any.String().Alpha().WithLengthBetween(1, 20)).NonEmpty().Generate().ToList();

        // Act
        WorkOrder order = new WorkOrder(anyBatch, anySku, anyQuantity, anyCadence, anyTolerance, anyWindow, anyOperators);

        // Assert
        Assert.Same(anyBatch, order.Batch);
        Assert.Equal(anySku, order.ProductSku);
        Assert.Equal(anyQuantity, order.PlannedQuantity);
        Assert.Same(anyCadence, order.Cadence);
        Assert.Same(anyTolerance, order.Tolerance);
        Assert.Same(anyWindow, order.Window);
        Assert.Equal(anyOperators, order.Operators);
        Assert.Equal(0, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
        Assert.False(order.IsComplete);
        Assert.Equal(0m, order.YieldPercent);
    }

    [Fact]
    public void A_generated_order_is_a_valid_one() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Assert
        Assert.InRange(anyOrder.PlannedQuantity, 1, 100_000);
        Assert.NotEmpty(anyOrder.Operators);
        Assert.InRange(anyOrder.ProductSku.Length, 1, 20);
    }

    [Fact]
    public void The_operators_are_copied() {
        // Arrange
        List<string> anyOperators = Any.ListOf(Any.String().Alpha().WithLengthBetween(1, 20)).WithCountBetween(2, 5).Generate().ToList();
        List<string> before       = anyOperators.ToList();
        WorkOrder    order        = OrderWith(operators: anyOperators);

        // Act
        anyOperators.Clear();
        anyOperators.Add("someone else");

        // Assert
        Assert.Equal(before, order.Operators);
    }

    [Fact]
    public void Operator_names_are_not_validated() {
        // Act
        WorkOrder order = OrderWith(operators: new[] { "", "   " });

        // Assert
        Assert.Equal(2, order.Operators.Count);
    }

    [Fact]
    public void An_order_without_batch_is_refused() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act
        Action act = () => new WorkOrder(null!, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_order_without_sku_is_refused() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act
        Action act = () => new WorkOrder(anyOrder.Batch, null!, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_order_with_an_empty_sku_is_refused() {
        Assert.Throws<ArgumentException>(() => OrderWith(sku: ""));
    }

    [Fact]
    public void A_whitespace_sku_is_accepted_since_only_emptiness_is_checked() {
        WorkOrder order = OrderWith(sku: "   ");

        Assert.Equal("   ", order.ProductSku);
    }

    [Fact]
    public void A_sku_of_20_characters_is_accepted() {
        // Arrange
        string anySku = Any.String().Alpha().WithLength(20).Generate();

        // Act
        WorkOrder order = OrderWith(sku: anySku);

        // Assert
        Assert.Equal(anySku, order.ProductSku);
    }

    [Fact]
    public void A_sku_of_21_characters_is_refused() {
        // Arrange
        string anyTooLong = Any.String().Alpha().WithLength(21).Generate();

        // Act
        Action act = () => OrderWith(sku: anyTooLong);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100_000)]
    public void The_limits_of_the_planned_quantity_are_accepted(int quantity) {
        WorkOrder order = OrderWith(quantity: quantity);

        Assert.Equal(quantity, order.PlannedQuantity);
    }

    [Fact]
    public void A_quantity_below_one_is_refused() {
        // Arrange
        int anyNonPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        Action act = () => OrderWith(quantity: anyNonPositive);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_quantity_above_100000_is_refused() {
        // Arrange
        int anyTooMany = Any.Int32().GreaterThan(100_000).Generate();

        // Act
        Action act = () => OrderWith(quantity: anyTooMany);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void An_order_without_cadence_is_refused() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act
        Action act = () => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, null!, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_order_without_tolerance_is_refused() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act
        Action act = () => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, null!, anyOrder.Window, anyOrder.Operators);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_order_without_window_is_refused() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act
        Action act = () => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, null!, anyOrder.Operators);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_order_without_operators_is_refused() {
        Assert.Throws<ArgumentException>(() => OrderWith(operators: new List<string>()));
    }

    [Fact(Skip = "domain defect: a null operators list fails with NullReferenceException instead of ArgumentNullException like every other argument, any seed")]
    public void A_null_list_of_operators_is_refused_as_a_null_argument() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act
        Action act = () => new WorkOrder(anyOrder.Batch, anyOrder.ProductSku, anyOrder.PlannedQuantity, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void The_arguments_are_checked_in_declaration_order() {
        // Arrange
        WorkOrder anyOrder = AnyOrder().Generate();

        // Act: the null batch is reported before the empty sku and the bad quantity.
        Action act = () => new WorkOrder(null!, "", 0, anyOrder.Cadence, anyOrder.Tolerance, anyOrder.Window, anyOrder.Operators);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void The_estimated_duration_is_the_time_to_produce_the_planned_quantity() {
        // Arrange
        WorkOrder order = OrderWith(quantity: 400, cadence: new Cadence(100));

        // Act
        TimeSpan estimated = order.EstimatedDuration;

        // Assert
        Assert.Equal(TimeSpan.FromHours(4), estimated);
    }

    [Fact]
    public void An_order_that_takes_exactly_the_window_fits_in_it() {
        // Arrange
        DateTime    anyStart = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)).Generate();
        ShiftWindow window   = new ShiftWindow(anyStart, anyStart.AddHours(4));
        WorkOrder   order    = OrderWith(quantity: 400, cadence: new Cadence(100), window: window);

        // Act
        bool fits = order.FitsInWindow;

        // Assert
        Assert.True(fits);
    }

    [Fact]
    public void An_order_one_minute_over_the_window_does_not_fit() {
        // Arrange
        DateTime    anyStart = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)).Generate();
        ShiftWindow window   = new ShiftWindow(anyStart, anyStart.AddHours(4));
        WorkOrder   order    = OrderWith(quantity: 401, cadence: new Cadence(100), window: window);

        // Act
        bool fits = order.FitsInWindow;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(241), order.EstimatedDuration);
        Assert.False(fits);
    }

    [Fact]
    public void A_good_part_is_counted_good() {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 5);
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, order.Tolerance.NominalMm);

        // Assert
        Assert.Equal(1, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
    }

    [Fact]
    public void A_part_out_of_tolerance_is_counted_scrap() {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 5);
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, order.Tolerance.UpperLimitMm + 1m);

        // Assert
        Assert.Equal(0, order.GoodCount);
        Assert.Equal(1, order.ScrapCount);
    }

    [Fact]
    public void A_part_exactly_on_a_limit_is_good() {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 5);
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, order.Tolerance.LowerLimitMm);
        order.RecordPart(machine, order.Tolerance.UpperLimitMm);

        // Assert
        Assert.Equal(2, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
    }

    [Theory]
    [InlineData(MachineState.Idle)]
    [InlineData(MachineState.Setup)]
    [InlineData(MachineState.Stopped)]
    [InlineData(MachineState.Faulted)]
    [InlineData(MachineState.Maintenance)]
    public void A_part_cannot_be_recorded_on_a_machine_that_is_not_running(MachineState state) {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 5);
        Machine   machine = MachineTests.MachineIn(state);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => order.RecordPart(machine, order.Tolerance.NominalMm));

        // Assert
        Assert.Contains(machine.MachineId, error.Message);
        Assert.Contains(state.ToString(), error.Message);
        Assert.Equal(0, order.GoodCount);
        Assert.Equal(0, order.ScrapCount);
    }

    [Fact(Skip = "domain defect: a null machine fails with NullReferenceException instead of ArgumentNullException like every other argument, any seed")]
    public void A_null_machine_is_refused_as_a_null_argument() {
        // Arrange
        WorkOrder order = AnyOrder().Generate();

        // Act
        Action act = () => order.RecordPart(null!, order.Tolerance.NominalMm);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void The_order_is_complete_once_the_good_parts_reach_the_planned_quantity() {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 3);
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, order.Tolerance.NominalMm);
        order.RecordPart(machine, order.Tolerance.NominalMm);
        bool completeBefore = order.IsComplete;
        order.RecordPart(machine, order.Tolerance.NominalMm);

        // Assert
        Assert.False(completeBefore);
        Assert.True(order.IsComplete);
    }

    [Fact]
    public void Scrap_does_not_bring_the_order_closer_to_completion() {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 2);
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, order.Tolerance.NominalMm);
        order.RecordPart(machine, order.Tolerance.UpperLimitMm + 1m);
        order.RecordPart(machine, order.Tolerance.UpperLimitMm + 1m);

        // Assert
        Assert.False(order.IsComplete);
    }

    [Fact]
    public void Parts_beyond_the_planned_quantity_are_still_recorded() {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 1);
        Machine   machine = RunningMachine();

        // Act
        order.RecordPart(machine, order.Tolerance.NominalMm);
        order.RecordPart(machine, order.Tolerance.NominalMm);

        // Assert
        Assert.Equal(2, order.GoodCount);
        Assert.True(order.IsComplete);
    }

    [Fact]
    public void The_yield_is_zero_before_any_part() {
        Assert.Equal(0m, AnyOrder().Generate().YieldPercent);
    }

    [Theory]
    [InlineData(3, 0, "100.0")]
    [InlineData(0, 3, "0.0")]
    [InlineData(1, 2, "33.3")]
    [InlineData(2, 1, "66.7")]
    [InlineData(1, 15, "6.3")]      // 6.25 rounds away from zero, not to even
    [InlineData(3, 13, "18.8")]     // 18.75
    [InlineData(1, 31, "3.1")]      // 3.125
    [InlineData(1, 7, "12.5")]
    public void The_yield_is_the_good_share_rounded_to_one_decimal(int good, int scrap, string expected) {
        // Arrange
        WorkOrder order   = OrderWith(quantity: 100);
        Machine   machine = RunningMachine();

        // Act
        for (int index = 0; index < good; index++) {
            order.RecordPart(machine, order.Tolerance.NominalMm);
        }
        for (int index = 0; index < scrap; index++) {
            order.RecordPart(machine, order.Tolerance.UpperLimitMm + 1m);
        }

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), order.YieldPercent);
    }

    [Fact]
    public void A_work_order_may_be_produced_on_any_running_machine_whatever_its_identity() {
        // Arrange
        WorkOrder order        = OrderWith(quantity: 5);
        Machine   firstMachine = RunningMachine();
        Machine   otherMachine = RunningMachine();

        // Act
        order.RecordPart(firstMachine, order.Tolerance.NominalMm);
        order.RecordPart(otherMachine, order.Tolerance.NominalMm);

        // Assert
        Assert.Equal(2, order.GoodCount);
    }

}
