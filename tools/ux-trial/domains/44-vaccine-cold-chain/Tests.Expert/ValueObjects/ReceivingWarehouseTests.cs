using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using Freight.Domain;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class ReceivingWarehouseTests {

    [Theory]
    [InlineData("0", "8")]
    [InlineData("2", "8")]
    [InlineData("4", "6")]
    [InlineData("0", "0.1")]
    public void A_cold_room_is_chilled_and_never_runs_below_zero(string min, string max) {
        // Arrange
        TemperatureRange coldRoom = new TemperatureRange(Celsius.Of(min), Celsius.Of(max));

        // Act
        ReceivingWarehouse warehouse = Any.ReceivingWarehouse().WithColdRoom(coldRoom).Generate();

        // Assert
        Assert.Equal(StorageZone.Chilled, warehouse.ColdRoom.Zone);
    }

    [Theory]
    [InlineData("-25", "-18")]
    [InlineData("-30", "-20")]
    public void A_frozen_room_is_not_a_vaccine_cold_room(string min, string max) {
        // Arrange
        AnyReceivingWarehouse warehouses = Any.ReceivingWarehouse().WithColdRoom(new TemperatureRange(Celsius.Of(min), Celsius.Of(max)));

        // Assert
        Assert.Throws<ArgumentException>(() => warehouses.Generate());
    }

    [Theory]
    [InlineData("8.1", "15")]
    [InlineData("15", "25")]
    public void An_ambient_room_is_not_a_vaccine_cold_room(string min, string max) {
        // Arrange
        AnyReceivingWarehouse warehouses = Any.ReceivingWarehouse().WithColdRoom(new TemperatureRange(Celsius.Of(min), Celsius.Of(max)));

        // Assert
        Assert.Throws<ArgumentException>(() => warehouses.Generate());
    }

    [Theory]
    [InlineData("-0.1", "5")]
    [InlineData("-5", "4")]
    public void A_chilled_room_that_dips_below_zero_is_refused(string min, string max) {
        // Arrange
        AnyReceivingWarehouse warehouses = Any.ReceivingWarehouse().WithColdRoom(new TemperatureRange(Celsius.Of(min), Celsius.Of(max)));

        // Assert
        Assert.Throws<ArgumentException>(() => warehouses.Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void A_quarantine_bay_is_at_floor_level_one_to_four(int level) {
        // Arrange
        BinLocation bay = new BinLocation('B', 12, level);

        // Act
        ReceivingWarehouse warehouse = Any.ReceivingWarehouse().WithQuarantineBay(bay).Generate();

        // Assert
        Assert.Equal(bay, warehouse.QuarantineBay);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    public void A_quarantine_bay_that_needs_a_reach_truck_is_refused(int level) {
        // Arrange
        AnyReceivingWarehouse warehouses = Any.ReceivingWarehouse().WithQuarantineBay(new BinLocation('B', 12, level));

        // Assert
        Assert.Throws<ArgumentException>(() => warehouses.Generate());
    }

    [Fact]
    public void Two_warehouses_with_the_same_location_operator_room_and_bay_are_equal() {
        // Arrange
        UnLocode        sharedLocation = Any.UnLocode().Generate();
        CustodianId     sharedOperator = Any.CustodianId().Generate();
        TemperatureRange sharedRoom    = new TemperatureRange(2m, 8m);
        BinLocation     sharedBay      = new BinLocation('B', 12, 2);

        AnyReceivingWarehouse warehouses = Any.ReceivingWarehouse().WithLocation(sharedLocation).WithOperatorId(sharedOperator).WithColdRoom(sharedRoom).WithQuarantineBay(sharedBay);

        // Assert
        Assert.Equal(warehouses.Generate(), warehouses.Generate());
    }

    [Fact]
    public void Warehouses_with_rooms_of_different_ranges_are_different() {
        // Arrange
        UnLocode    sharedLocation = Any.UnLocode().Generate();
        CustodianId sharedOperator = Any.CustodianId().Generate();
        BinLocation sharedBay      = new BinLocation('B', 12, 2);

        AnyReceivingWarehouse warehouses = Any.ReceivingWarehouse().WithLocation(sharedLocation).WithOperatorId(sharedOperator).WithQuarantineBay(sharedBay);

        ReceivingWarehouse wide   = warehouses.WithColdRoom(new TemperatureRange(2m, 8m)).Generate();
        ReceivingWarehouse narrow = warehouses.WithColdRoom(new TemperatureRange(3m, 8m)).Generate();

        // Assert
        Assert.NotEqual(wide, narrow);
    }

}
