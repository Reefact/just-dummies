using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class BatchConstructionTests {

    [Fact]
    public void A_batch_keeps_what_it_was_made_of() {
        // Arrange
        Gtin               gtin           = new Gtin("04006381333931");
        BatchNumber        number         = new BatchNumber("LOT-42");
        DateOnly           manufacturedOn = new DateOnly(2025, 3, 1);
        DateOnly           expiresOn      = new DateOnly(2027, 3, 31);
        ReimbursementPrice price          = new ReimbursementPrice(12.50m);

        // Act
        Batch batch = new Batch(gtin, number, manufacturedOn, expiresOn, 1_000, price);

        // Assert
        Assert.Same(gtin, batch.Gtin);
        Assert.Same(number, batch.Number);
        Assert.Equal(manufacturedOn, batch.ManufacturedOn);
        Assert.Equal(expiresOn, batch.ExpiresOn);
        Assert.Equal(1_000, batch.PlannedQuantity);
        Assert.Same(price, batch.Price);
    }

    [Fact]
    public void A_new_batch_has_no_pack() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Assert
        Assert.Empty(anyBatch.Packs);
    }

    [Fact]
    public void A_batch_without_a_gtin_is_refused() {
        // Arrange
        BatchNumber        anyNumber = Any.BatchNumber().Generate();
        ReimbursementPrice anyPrice  = Any.ReimbursementPrice().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new Batch(null!, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2027, 3, 31), 10, anyPrice));

        // Assert
        Assert.Equal("gtin", exception.ParamName);
    }

    [Fact]
    public void A_batch_without_a_number_is_refused() {
        // Arrange
        Gtin               anyGtin  = Any.Gtin().Generate();
        ReimbursementPrice anyPrice = Any.ReimbursementPrice().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new Batch(anyGtin, null!, new DateOnly(2025, 3, 1), new DateOnly(2027, 3, 31), 10, anyPrice));

        // Assert
        Assert.Equal("number", exception.ParamName);
    }

    [Fact]
    public void A_batch_without_a_price_is_refused() {
        // Arrange
        Gtin        anyGtin   = Any.Gtin().Generate();
        BatchNumber anyNumber = Any.BatchNumber().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new Batch(anyGtin, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2027, 3, 31), 10, null!));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

    [Fact]
    public void A_batch_expiring_the_day_after_it_is_made_is_accepted() {
        // Arrange
        Gtin               anyGtin   = Any.Gtin().Generate();
        BatchNumber        anyNumber = Any.BatchNumber().Generate();
        ReimbursementPrice anyPrice  = Any.ReimbursementPrice().Generate();

        // Act
        Batch batch = new Batch(anyGtin, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 2), 10, anyPrice);

        // Assert
        Assert.Equal(new DateOnly(2025, 3, 2), batch.ExpiresOn);
    }

    [Fact]
    public void A_batch_expiring_the_day_it_is_made_is_refused() {
        // Arrange
        Gtin               anyGtin   = Any.Gtin().Generate();
        BatchNumber        anyNumber = Any.BatchNumber().Generate();
        ReimbursementPrice anyPrice  = Any.ReimbursementPrice().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new Batch(anyGtin, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 1), 10, anyPrice));

        // Assert
        Assert.Equal("expiresOn", exception.ParamName);
    }

    [Fact]
    public void A_batch_expiring_before_it_is_made_is_refused() {
        // Arrange
        Gtin               anyGtin   = Any.Gtin().Generate();
        BatchNumber        anyNumber = Any.BatchNumber().Generate();
        ReimbursementPrice anyPrice  = Any.ReimbursementPrice().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new Batch(anyGtin, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2025, 2, 28), 10, anyPrice));

        // Assert
        Assert.Equal("expiresOn", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500_000)]
    public void A_planned_quantity_from_one_to_five_hundred_thousand_is_accepted(int plannedQuantity) {
        // Arrange
        Gtin               anyGtin   = Any.Gtin().Generate();
        BatchNumber        anyNumber = Any.BatchNumber().Generate();
        ReimbursementPrice anyPrice  = Any.ReimbursementPrice().Generate();

        // Act
        Batch batch = new Batch(anyGtin, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2027, 3, 31), plannedQuantity, anyPrice);

        // Assert
        Assert.Equal(plannedQuantity, batch.PlannedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(500_001)]
    [InlineData(int.MaxValue)]
    public void A_planned_quantity_outside_one_to_five_hundred_thousand_is_refused(int plannedQuantity) {
        // Arrange
        Gtin               anyGtin   = Any.Gtin().Generate();
        BatchNumber        anyNumber = Any.BatchNumber().Generate();
        ReimbursementPrice anyPrice  = Any.ReimbursementPrice().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Batch(anyGtin, anyNumber, new DateOnly(2025, 3, 1), new DateOnly(2027, 3, 31), plannedQuantity, anyPrice));

        // Assert
        Assert.Equal("plannedQuantity", exception.ParamName);
    }

}
