namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class TraceEntryTests {

    [Fact]
    public void An_entry_keeps_what_it_was_given() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act
        TraceEntry entry = new TraceEntry(anyLot, 2, 0.25m, Kg.Of(5m));

        // Assert
        Assert.Same(anyLot, entry.Lot);
        Assert.Equal(2, entry.Depth);
        Assert.Equal(0.25m, entry.Share);
        Assert.Equal(Kg.Of(5m), entry.ExposedQuantity);
    }

    [Fact]
    public void The_nearest_depth_is_one() {
        // Arrange
        TraceEntry anyEntry = Any.TraceEntry().Generate();

        // Act
        TraceEntry nearest = new TraceEntry(anyEntry.Lot, 1, anyEntry.Share, anyEntry.ExposedQuantity);

        // Assert
        Assert.Equal(1, nearest.Depth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_depth_below_one_is_refused(int depth) {
        // Arrange
        TraceEntry anyEntry = Any.TraceEntry().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TraceEntry(anyEntry.Lot, depth, anyEntry.Share, anyEntry.ExposedQuantity));
    }

    [Fact]
    public void A_zero_share_is_refused() {
        // Arrange
        TraceEntry anyEntry = Any.TraceEntry().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TraceEntry(anyEntry.Lot, anyEntry.Depth, 0m, anyEntry.ExposedQuantity));
    }

    [Fact]
    public void A_share_above_one_is_refused() {
        // Arrange
        TraceEntry anyEntry   = Any.TraceEntry().Generate();
        decimal    anyAboveOne = Any.Decimal().GreaterThan(1m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TraceEntry(anyEntry.Lot, anyEntry.Depth, anyAboveOne, anyEntry.ExposedQuantity));
    }

    [Fact]
    public void Missing_parts_are_refused() {
        // Arrange
        TraceEntry anyEntry = Any.TraceEntry().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TraceEntry(null!, anyEntry.Depth, anyEntry.Share, anyEntry.ExposedQuantity));
        Assert.Throws<ArgumentNullException>(() => new TraceEntry(anyEntry.Lot, anyEntry.Depth, anyEntry.Share, null!));
    }

    [Fact]
    public void Printing_shows_lot_depth_share_and_quantity() {
        // Act
        TraceEntry entry = new TraceEntry(new LotNumber("API-000001"), 3, 0.5m, Kg.Of(10m));

        // Assert
        string text = entry.ToString();
        Assert.Contains("API-000001", text);
        Assert.Contains("depth 3", text);
        Assert.Contains("10 kg", text);
    }

}
