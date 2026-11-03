using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class TraceEntryTests {

    [Fact]
    public void An_entry_records_how_far_a_lot_is_and_how_much_of_it_is_concerned() {
        // Arrange
        LotNumber anyLot      = Any.LotNumber().Generate();
        Quantity  anyExposure = Any.Quantity().Generate();

        // Act
        TraceEntry entry = new TraceEntry(anyLot, 2, 0.125m, anyExposure);

        // Assert
        Assert.Same(anyLot, entry.Lot);
        Assert.Equal(2, entry.Depth);
        Assert.Equal(0.125m, entry.Share);
        Assert.Same(anyExposure, entry.ExposedQuantity);
    }

    [Fact]
    public void The_nearest_depth_and_a_whole_share_are_accepted() {
        // Arrange
        TraceEntry anyEntry = Any.TraceEntry().WithDepth(1).WithShare(1m).Generate();

        // Assert
        Assert.Equal(1, anyEntry.Depth);
        Assert.Equal(1m, anyEntry.Share);
    }

    [Fact]
    public void A_depth_below_one_is_refused() {
        // Arrange
        int       anyDepth = Any.Int32().LessThanOrEqualTo(0).Generate();
        LotNumber anyLot   = Any.LotNumber().Generate();
        Quantity  anyExposure = Any.Quantity().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new TraceEntry(anyLot, anyDepth, 0.5m, anyExposure));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.01)]
    public void A_share_outside_zero_exclusive_to_one_inclusive_is_refused(double share) {
        // Arrange
        LotNumber anyLot      = Any.LotNumber().Generate();
        Quantity  anyExposure = Any.Quantity().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new TraceEntry(anyLot, 1, (decimal)share, anyExposure));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void A_missing_lot_or_exposure_is_refused() {
        // Arrange
        LotNumber anyLot      = Any.LotNumber().Generate();
        Quantity  anyExposure = Any.Quantity().Generate();

        // Act
        Exception? noLot      = Record.Exception(() => new TraceEntry(null!, 1, 0.5m, anyExposure));
        Exception? noExposure = Record.Exception(() => new TraceEntry(anyLot, 1, 0.5m, null!));

        // Assert
        Assert.IsType<ArgumentNullException>(noLot);
        Assert.IsType<ArgumentNullException>(noExposure);
    }

}
