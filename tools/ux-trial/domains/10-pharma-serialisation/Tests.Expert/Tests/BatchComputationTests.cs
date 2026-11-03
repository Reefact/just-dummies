using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class BatchComputationTests {

    // A batch made on 2025-01-01 and expiring on 2025-01-09 lasts eight days, which makes every fraction below exact.
    public static TheoryData<string, string> EightDayShelfLife => new TheoryData<string, string> {
        { "2024-12-01", "1" },     // long before it was made: all the shelf life is left, never more
        { "2025-01-01", "1" },     // the day it is made
        { "2025-01-02", "0.88" },  // 7/8 = 0.875, half away from zero
        { "2025-01-04", "0.63" },  // 5/8 = 0.625: the midpoint goes up, where banker's rounding would give 0.62
        { "2025-01-05", "0.5" },
        { "2025-01-08", "0.13" },  // 1/8 = 0.125: the midpoint goes up, where banker's rounding would give 0.12
        { "2025-01-09", "0" },     // the expiry day
        { "2025-01-10", "0" },     // after it, nothing is left, never less
        { "2030-01-01", "0" }
    };

    [Theory]
    [MemberData(nameof(EightDayShelfLife))]
    public void The_remaining_shelf_life_is_the_share_of_days_left_rounded_to_two_places(string today, string expectedRatio) {
        // Arrange
        Batch anyBatch = Any.Batch().WithManufacturedOn(new DateOnly(2025, 1, 1)).WithExpiresOn(new DateOnly(2025, 1, 9)).Generate();

        // Act
        decimal ratio = anyBatch.RemainingShelfLifeRatio(DateOnly.Parse(today));

        // Assert
        Assert.Equal(decimal.Parse(expectedRatio, System.Globalization.CultureInfo.InvariantCulture), ratio);
    }

    [Theory]
    [InlineData("2025-01-02", "0.67")] // 2/3 = 0.666...
    [InlineData("2025-01-03", "0.33")] // 1/3 = 0.333...
    public void A_share_that_does_not_end_is_rounded_to_two_places(string today, string expectedRatio) {
        // Arrange: three days from manufacture to expiry
        Batch anyBatch = Any.Batch().WithManufacturedOn(new DateOnly(2025, 1, 1)).WithExpiresOn(new DateOnly(2025, 1, 4)).Generate();

        // Act
        decimal ratio = anyBatch.RemainingShelfLifeRatio(DateOnly.Parse(today));

        // Assert
        Assert.Equal(decimal.Parse(expectedRatio, System.Globalization.CultureInfo.InvariantCulture), ratio);
    }

    [Fact]
    public void A_batch_of_a_single_day_shelf_life_has_all_of_it_left_until_the_day_it_is_made() {
        // Arrange
        Batch anyBatch = Any.Batch().WithManufacturedOn(new DateOnly(2025, 1, 1)).WithExpiresOn(new DateOnly(2025, 1, 2)).Generate();

        // Act & Assert
        Assert.Equal(1m, anyBatch.RemainingShelfLifeRatio(new DateOnly(2025, 1, 1)));
        Assert.Equal(0m, anyBatch.RemainingShelfLifeRatio(new DateOnly(2025, 1, 2)));
    }

    [Fact]
    public void A_batch_without_pack_has_no_stock_value() {
        // Arrange
        Batch anyEmptyBatch = Any.Batch().WithPrice(new ReimbursementPrice(12.34m)).Generate();

        // Act
        decimal value = anyEmptyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(0m, value);
    }

    [Fact]
    public void The_stock_value_is_the_active_packs_times_the_unit_price() {
        // Arrange
        Batch anyBatch = Any.Batch().WithPrice(new ReimbursementPrice(12.34m)).WithRoomFor(3).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Commission(new SerialNumber("SERIAL03"));

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(37.02m, value);
    }

    [Fact]
    public void A_dispensed_pack_leaves_the_stock_value() {
        // Arrange
        DateOnly expiry = new DateOnly(2027, 6, 30);
        Batch anyBatch  = Any.Batch().WithPrice(new ReimbursementPrice(12.34m)).WithExpiresOn(expiry).WithRoomFor(3).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Commission(new SerialNumber("SERIAL03"));
        anyBatch.Dispense(new SerialNumber("SERIAL02"), expiry);

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(24.68m, value);
    }

    [Fact]
    public void A_recalled_batch_has_no_stock_value() {
        // Arrange
        Batch anyBatch = Any.Batch().WithPrice(new ReimbursementPrice(12.34m)).WithRoomFor(2).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Recall();

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(0m, value);
    }

    [Fact]
    public void An_expired_pack_still_counts_in_the_stock_value_while_it_is_active() {
        // Arrange
        Batch anyBatch = Any.Batch().WithPrice(new ReimbursementPrice(0.07m)).WithExpiresOn(new DateOnly(2027, 6, 30)).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Verify(new SerialNumber("SERIAL01"), new DateOnly(2028, 1, 1));

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(0.07m, value);
    }

    [Fact]
    public void The_stock_value_of_a_full_batch_at_the_top_price_stays_exact() {
        // Arrange
        Batch anyBatch = Any.Batch().WithPrice(new ReimbursementPrice(100_000m)).WithPlannedQuantity(3).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Commission(new SerialNumber("SERIAL03"));

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(300_000m, value);
    }

}
