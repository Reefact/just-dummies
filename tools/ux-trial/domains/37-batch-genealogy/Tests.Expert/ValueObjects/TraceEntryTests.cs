using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.ValueObjects;

public sealed class TraceEntryTests {

    [Fact]
    public void An_entry_says_how_far_a_lot_is_what_share_it_carries_and_how_much_that_is() {
        // Arrange
        LotNumber lot      = new LotNumber("API-240117");
        Quantity  exposure = new Quantity(30m, QuantityUnit.Kilogram);

        // Act
        TraceEntry entry = new TraceEntry(lot, 2, 0.3m, exposure);

        // Assert
        Assert.Same(lot, entry.Lot);
        Assert.Equal(2, entry.Depth);
        Assert.Equal(0.3m, entry.Share);
        Assert.Same(exposure, entry.ExposedQuantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void A_depth_of_at_least_one_is_accepted(int depth) {
        Assert.Equal(depth, Any.TraceEntry().WithDepth(depth).Generate().Depth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_lot_is_at_least_one_transformation_away_so_a_smaller_depth_is_refused(int depth) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.TraceEntry().WithDepth(depth).Generate());
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("1")]
    public void The_smallest_and_the_largest_share_are_accepted(string share) {
        Assert.Equal(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture),
                     Any.TraceEntry().WithShare(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture)).Generate().Share);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    [InlineData("1.0000000000000000000000000001")]
    public void A_share_of_zero_or_less_or_above_one_is_refused(string share) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.TraceEntry().WithShare(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture)).Generate());
    }

    [Fact]
    public void An_entry_exposing_nothing_is_accepted_because_only_the_share_must_be_positive() {
        // Act
        TraceEntry entry = Any.TraceEntry().WithExposedQuantity(new Quantity(0m, QuantityUnit.Gram)).Generate();

        // Assert
        Assert.True(entry.ExposedQuantity.IsZero);
    }

    [Fact]
    public void A_missing_lot_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TraceEntry(null!, 1, 1m, Any.Quantity().Generate()));
    }

    [Fact]
    public void A_missing_exposed_quantity_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TraceEntry(Any.LotNumber().Generate(), 1, 1m, null!));
    }

    [Fact]
    public void An_entry_prints_its_lot_depth_share_and_quantity() {
        // Arrange
        TraceEntry entry = new TraceEntry(new LotNumber("API-240117"), 2, 0.3m, new Quantity(30m, QuantityUnit.Kilogram));

        // Act
        string text = CultureInfoScope.Use("", () => entry.ToString());

        // Assert
        Assert.Equal("API-240117 (depth 2, 30.00 %, 30 kg)", text);
    }

}
