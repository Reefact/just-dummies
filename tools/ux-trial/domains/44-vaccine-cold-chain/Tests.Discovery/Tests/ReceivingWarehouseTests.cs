using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class ReceivingWarehouseTests {

    [Fact]
    public void A_warehouse_keeps_everything_it_was_built_with() {
        // Arrange
        UnLocode         anyLocation = new AnyUnLocode().Generate();
        CustodianId      anyOperator = new AnyCustodianId().Generate();
        TemperatureRange coldRoom    = new TemperatureRange(2m, 8m);
        BinLocation      bay         = new BinLocation('B', 12, 3);

        // Act
        ReceivingWarehouse warehouse = new ReceivingWarehouse(anyLocation, anyOperator, coldRoom, bay);

        // Assert
        Assert.Same(anyLocation, warehouse.Location);
        Assert.Same(anyOperator, warehouse.OperatorId);
        Assert.Same(coldRoom, warehouse.ColdRoom);
        Assert.Same(bay, warehouse.QuarantineBay);
    }

    [Theory]
    [InlineData(2, 8)]
    [InlineData(0, 8)]
    [InlineData(0, 1)]
    [InlineData(4, 6)]
    [InlineData(7.9, 8)]
    public void A_chilled_room_never_below_zero_is_accepted(double min, double max) {
        // Act
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse().WithColdRoom(new TemperatureRange((decimal)min, (decimal)max)).Generate();

        // Assert
        Assert.Equal((decimal)min, warehouse.ColdRoom.MinCelsius);
        Assert.Equal((decimal)max, warehouse.ColdRoom.MaxCelsius);
    }

    [Theory]
    [InlineData(2, 8.1)]
    [InlineData(2, 15)]
    [InlineData(15, 25)]
    [InlineData(-20, -18)]
    [InlineData(-25, -18)]
    public void A_room_that_is_not_chilled_is_refused(double min, double max) {
        // Arrange
        TemperatureRange room = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        Exception? thrown = Record.Exception(() => new AnyReceivingWarehouse().WithColdRoom(room).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("coldRoom", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.1, 8)]
    [InlineData(-5, 4)]
    [InlineData(-17.9, 8)]
    public void A_chilled_room_running_below_zero_is_refused(double min, double max) {
        // Arrange
        TemperatureRange room = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        Exception? thrown = Record.Exception(() => new AnyReceivingWarehouse().WithColdRoom(room).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("coldRoom", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void A_quarantine_bay_at_floor_levels_one_to_four_is_accepted(int level) {
        // Act
        ReceivingWarehouse warehouse = new AnyReceivingWarehouse().WithQuarantineBay(new BinLocation('A', 1, level)).Generate();

        // Assert
        Assert.Equal(level, warehouse.QuarantineBay.Level);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(20)]
    public void A_quarantine_bay_needing_a_reach_truck_is_refused(int level) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyReceivingWarehouse().WithQuarantineBay(new BinLocation('A', 1, level)).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("quarantineBay", refusal.ParamName);
    }

    [Fact]
    public void Every_part_is_required() {
        // Arrange
        UnLocode         anyLocation = new AnyUnLocode().Generate();
        CustodianId      anyOperator = new AnyCustodianId().Generate();
        TemperatureRange anyRoom     = new TemperatureRange(2m, 8m);
        BinLocation      anyBay      = new BinLocation('A', 1, 1);

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ReceivingWarehouse(null!, anyOperator, anyRoom, anyBay));
        Assert.Throws<ArgumentNullException>(() => new ReceivingWarehouse(anyLocation, null!, anyRoom, anyBay));
        Assert.Throws<ArgumentNullException>(() => new ReceivingWarehouse(anyLocation, anyOperator, null!, anyBay));
        Assert.Throws<ArgumentNullException>(() => new ReceivingWarehouse(anyLocation, anyOperator, anyRoom, null!));
    }

    [Fact]
    public void Warehouses_are_equal_when_all_their_parts_are() {
        // Arrange
        UnLocode           anyLocation = new AnyUnLocode().Generate();
        CustodianId        anyOperator = new AnyCustodianId().Generate();
        ReceivingWarehouse warehouse   = new ReceivingWarehouse(anyLocation, anyOperator, new TemperatureRange(2m, 8m), new BinLocation('A', 1, 1));
        ReceivingWarehouse same        = new ReceivingWarehouse(anyLocation, anyOperator, new TemperatureRange(2m, 8m), new BinLocation('A', 1, 1));

        // Assert
        Assert.Equal(warehouse, same);
        Assert.Equal(warehouse.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(warehouse, new ReceivingWarehouse(anyLocation, anyOperator, new TemperatureRange(3m, 8m), new BinLocation('A', 1, 1)));
        Assert.NotEqual(warehouse, new ReceivingWarehouse(anyLocation, anyOperator, new TemperatureRange(2m, 7m), new BinLocation('A', 1, 1)));
        Assert.NotEqual(warehouse, new ReceivingWarehouse(anyLocation, anyOperator, new TemperatureRange(2m, 8m), new BinLocation('A', 1, 2)));
    }

    [Fact]
    public void A_warehouse_reads_back_its_operator_and_its_place() {
        // Arrange
        CustodianId        @operator  = new CustodianId("FR-GDP-04217");
        UnLocode           location   = new UnLocode("FRLYS");
        ReceivingWarehouse warehouse  = new AnyReceivingWarehouse().WithOperatorId(@operator).WithLocation(location).Generate();

        // Assert
        Assert.Equal("FR-GDP-04217 at FRLYS", warehouse.ToString());
    }

}
