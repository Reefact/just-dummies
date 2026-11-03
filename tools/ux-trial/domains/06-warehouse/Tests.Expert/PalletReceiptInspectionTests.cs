using JustDummies;

using Warehouse.Domain.Tests.Expert.Support;
using Warehouse.Domain.Tests.Generators;

using Xunit;

using static Warehouse.Domain.Tests.Expert.Support.Receiving;

namespace Warehouse.Domain.Tests.Expert;

public sealed class PalletReceiptInspectionTests {

    private const string ShortDelivery  = "short delivery";
    private const string Overweight     = "overweight";
    private const string ShortShelfLife = "short shelf life";

    // The reception instant the shelf-life tests are about: a literal, since every best-before date is read against it.
    private static readonly DateTime Monday       = new DateTime(2026, 3, 2, 14, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly MondayDate   = new DateOnly(2026, 3, 2);

    public static TheoryData<decimal, bool> TaresOnAPalletOf1150Kg => new() {
        { -100m,   false },
        { 0m,      false },
        { 49.99m,  false },
        { 50m,     false },    // 1200 kg exactly is not above the limit
        { 50.01m,  true },
        { 1_000m,  true }
    };

    public static TheoryData<int, bool> ShelfLifeInDaysAndWhetherItIsShort => new() {
        { -400, true },
        { -1,   true },     // already expired when received
        { 0,    true },
        { 1,    true },
        { 29,   true },
        { 30,   false },    // exactly thirty days is enough
        { 31,   false },
        { 365,  false }
    };

    [Fact]
    public void Goods_that_arrived_as_announced_with_a_long_shelf_life_and_a_light_load_raise_no_reason() {
        // Arrange
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt, ConformingLine(anyShelfLife), ConformingLine(anyShelfLife));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Empty(reasons);
    }

    [Fact]
    public void A_pallet_is_held_back_when_a_line_arrives_short() {
        // Arrange
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        ReceiptLine   shortLine    = Line(expected: 10, received: 9, LightUnitKg, anyShelfLife.BestBefore);
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt, ConformingLine(anyShelfLife), shortLine);

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal([ShortDelivery], reasons);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 11)]
    [InlineData(0, 0)]
    [InlineData(0, 10_000)]
    public void A_line_that_arrives_complete_or_in_excess_is_not_a_short_delivery(int expectedUnits, int receivedUnits) {
        // Arrange
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        ReceiptLine   line         = Line(expectedUnits, receivedUnits, LightUnitKg, anyShelfLife.BestBefore);
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt, line);

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Empty(reasons);
    }

    [Fact]
    public void Several_short_lines_give_the_short_delivery_reason_only_once() {
        // Arrange
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt,
                                             Line(10, 9, LightUnitKg, anyShelfLife.BestBefore),
                                             Line(20, 0, LightUnitKg, anyShelfLife.BestBefore),
                                             Line(5, 4, LightUnitKg, anyShelfLife.BestBefore));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal([ShortDelivery], reasons);
    }

    [Theory]
    [MemberData(nameof(TaresOnAPalletOf1150Kg))]
    public void A_pallet_is_overweight_only_when_goods_and_tare_together_exceed_1200_kilograms(decimal tareKg, bool expectedOverweight) {
        // Arrange: one line of 100 units at 11.5 kg, announced and received alike.
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt, Line(100, 100, 11.5m, anyShelfLife.BestBefore));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(tareKg);

        // Assert
        Assert.Equal(expectedOverweight ? [Overweight] : [], reasons);
    }

    [Fact]
    public void The_weight_limit_applies_to_the_whole_pallet_not_to_each_line() {
        // Arrange: two lines of 700 kg each, none of them overweight on its own.
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt,
                                             Line(70, 70, 10m, anyShelfLife.BestBefore),
                                             Line(70, 70, 10m, anyShelfLife.BestBefore));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(0m);

        // Assert
        Assert.Equal([Overweight], reasons);
    }

    [Fact]
    public void The_weight_that_counts_is_the_received_one_not_the_announced_one() {
        // Arrange: 100 units were announced at 20 kg, but only 10 arrived.
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        PalletReceipt receipt      = Receipt(anyShelfLife.ReceivedAt, Line(expected: 100, received: 10, 20m, anyShelfLife.BestBefore));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(0m);

        // Assert
        Assert.Equal([ShortDelivery], reasons);
    }

    [Theory]
    [MemberData(nameof(ShelfLifeInDaysAndWhetherItIsShort))]
    public void A_line_has_a_short_shelf_life_when_fewer_than_30_days_separate_reception_from_its_best_before_date(int shelfLifeDays, bool expectedShort) {
        // Arrange
        ReceiptLine   line    = Line(10, 10, LightUnitKg, MondayDate.AddDays(shelfLifeDays));
        PalletReceipt receipt = Receipt(Monday, line);

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal(expectedShort ? [ShortShelfLife] : [], reasons);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(23, 59, 59, 999)]
    public void The_time_of_day_of_the_reception_does_not_move_the_shelf_life_boundary(int hour, int minute, int second, int millisecond) {
        // Arrange: thirty calendar days after the reception date, whatever the hour.
        DateTime      receivedAt = new DateTime(2026, 3, 2, hour, minute, second, millisecond, DateTimeKind.Utc);
        PalletReceipt exactlyThirtyDays = Receipt(receivedAt, Line(10, 10, LightUnitKg, MondayDate.AddDays(30)));
        PalletReceipt twentyNineDays    = Receipt(receivedAt, Line(10, 10, LightUnitKg, MondayDate.AddDays(29)));

        // Act
        IReadOnlyList<string> enough = exactlyThirtyDays.InspectionReasons(25m);
        IReadOnlyList<string> short_ = twentyNineDays.InspectionReasons(25m);

        // Assert
        Assert.Empty(enough);
        Assert.Equal([ShortShelfLife], short_);
    }

    [Fact]
    public void The_thirty_days_are_counted_in_calendar_days_across_a_leap_february() {
        // Arrange: 2024-02-15 + 30 days is 2024-03-16, February having 29 days that year.
        DateTime      receivedAt = new DateTime(2024, 2, 15, 9, 0, 0, DateTimeKind.Utc);
        PalletReceipt enough     = Receipt(receivedAt, Line(10, 10, LightUnitKg, new DateOnly(2024, 3, 16)));
        PalletReceipt notEnough  = Receipt(receivedAt, Line(10, 10, LightUnitKg, new DateOnly(2024, 3, 15)));

        // Act
        IReadOnlyList<string> reasonsWhenEnough    = enough.InspectionReasons(25m);
        IReadOnlyList<string> reasonsWhenNotEnough = notEnough.InspectionReasons(25m);

        // Assert
        Assert.Empty(reasonsWhenEnough);
        Assert.Equal([ShortShelfLife], reasonsWhenNotEnough);
    }

    [Fact]
    public void One_line_with_a_short_shelf_life_is_enough_and_the_reason_is_given_once() {
        // Arrange
        ShelfLife     anyShelfLife = AmpleShelfLife().Generate();
        PalletReceipt receipt      = Receipt(Monday,
                                             Line(10, 10, LightUnitKg, MondayDate.AddDays(400)),
                                             Line(10, 10, LightUnitKg, MondayDate.AddDays(5)),
                                             Line(10, 10, LightUnitKg, MondayDate.AddDays(12)));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal([ShortShelfLife], reasons);
    }

    [Fact]
    public void The_reasons_come_in_the_policy_order_short_delivery_then_overweight_then_short_shelf_life() {
        // Arrange: a short, heavy line close to its best-before date.
        PalletReceipt receipt = Receipt(Monday, Line(expected: 200, received: 100, 12m, MondayDate.AddDays(3)));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal([ShortDelivery, Overweight, ShortShelfLife], reasons);
    }

    [Fact]
    public void Reasons_that_do_not_apply_are_left_out_without_disturbing_the_order() {
        // Arrange: heavy and close to its best-before date, but complete.
        PalletReceipt receipt = Receipt(Monday, Line(100, 100, 12m, MondayDate.AddDays(3)));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal([Overweight, ShortShelfLife], reasons);
    }

    [Fact]
    public void Inspecting_a_pallet_does_not_change_its_status() {
        // Arrange
        PalletReceipt receipt = Receipt(Monday, Line(expected: 200, received: 100, 12m, MondayDate.AddDays(3)));

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.NotEmpty(reasons);
        Assert.Equal(ReceiptStatus.Received, receipt.Status);
        Assert.Null(receipt.QuarantineReason);
    }

}
