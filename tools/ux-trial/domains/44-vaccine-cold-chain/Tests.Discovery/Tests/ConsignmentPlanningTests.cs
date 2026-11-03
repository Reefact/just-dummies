using AgriTech.Domain;

using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using Xunit;

using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Tests;

public sealed class ConsignmentPlanningTests {

    private static VaccineProduct ProductWithAllowance(int cumulative, int single) {
        return new AnyVaccineProduct().WithBudget(new StabilityBudget(cumulative, single)).Generate();
    }

    private static ColdChainPallet PalletOf(VaccineProduct product) {
        return new AnyColdChainPallet().ForProduct(product).Generate();
    }

    [Fact]
    public void A_consignment_is_planned_on_a_booked_reefer_with_pallets_of_its_product() {
        // Arrange
        ConsignmentId        anyId = new AnyConsignmentId().Generate();
        VaccineProduct       anyProduct = new AnyVaccineProduct().Generate();
        FreightShipment      shipment = Journey.BookedReefer(2);
        List<ColdChainPallet> pallets = [PalletOf(anyProduct), PalletOf(anyProduct), PalletOf(anyProduct)];

        // Act
        ColdChainConsignment consignment = new ColdChainConsignment(anyId, anyProduct, shipment, pallets);

        // Assert
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
        Assert.Same(anyId, consignment.Id);
        Assert.Same(anyProduct, consignment.Product);
        Assert.Same(shipment, consignment.Shipment);
        Assert.Equal(pallets, consignment.Pallets);
        Assert.Equal(pallets.Sum(p => p.PackCount), consignment.TotalPacks);
        Assert.Null(consignment.DispatchedAt);
        Assert.Null(consignment.DeliveredAt);
        Assert.Null(consignment.ReceivedBy);
        Assert.Empty(consignment.Custody);
        Assert.Empty(consignment.Decisions);
        Assert.Null(consignment.CurrentLeg);
    }

    [Fact]
    public void Every_pallet_is_given_an_empty_history_against_the_label_of_the_product() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned(new JourneyOptions { Pallets = 2, LabelMin = 3m, LabelMax = 6m }).Consignment;

        // Assert
        Assert.All(consignment.Pallets, pallet => {
            TemperatureHistory history = consignment.HistoryOf(pallet.Sscc);
            Assert.Empty(history.Readings);
            Assert.Same(pallet.Logger, history.Logger);
            Assert.Equal(3m, history.LabelledRange.MinCelsius);
            Assert.Equal(6m, history.LabelledRange.MaxCelsius);
        });
    }

    [Fact]
    public void A_shipment_already_departed_is_refused() {
        // Arrange
        VaccineProduct  anyProduct = new AnyVaccineProduct().Generate();
        FreightShipment departed   = Journey.BookedReefer(1);
        departed.Depart(Journey.DispatchedAt);

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, departed, [PalletOf(anyProduct)]));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("shipment", refusal.ParamName);
    }

    [Fact]
    public void A_cancelled_shipment_is_refused() {
        // Arrange
        VaccineProduct  anyProduct = new AnyVaccineProduct().Generate();
        FreightShipment cancelled  = Journey.BookedReefer(1);
        cancelled.Cancel();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, cancelled, [PalletOf(anyProduct)]));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("shipment", refusal.ParamName);
    }

    [Theory]
    [InlineData(ContainerType.Dry20)]
    [InlineData(ContainerType.Dry40)]
    [InlineData(ContainerType.HighCube40)]
    public void A_container_that_is_not_a_reefer_is_refused(ContainerType containerType) {
        // Arrange
        VaccineProduct  anyProduct = new AnyVaccineProduct().Generate();
        FreightShipment dry        = new AnyShipment().WithContainerType(containerType).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, dry, [PalletOf(anyProduct)]));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("shipment", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(ColdChainConsignment.MaxPallets)]
    public void A_reefer_takes_from_one_to_twenty_pallets(int count) {
        // Act
        ColdChainConsignment consignment = new AnyColdChainConsignment().WithPalletCount(count).Generate();

        // Assert
        Assert.Equal(count, consignment.Pallets.Count);
    }

    [Fact]
    public void No_pallet_at_all_is_refused() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), []));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Fact]
    public void More_than_twenty_pallets_are_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainConsignment().WithPalletCount(ColdChainConsignment.MaxPallets + 1).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Fact]
    public void A_missing_pallet_is_refused() {
        // Arrange
        VaccineProduct        anyProduct = new AnyVaccineProduct().Generate();
        List<ColdChainPallet> pallets    = [PalletOf(anyProduct), null!];

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), pallets));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Fact]
    public void Every_part_is_required() {
        // Arrange
        ConsignmentId         anyId      = new AnyConsignmentId().Generate();
        VaccineProduct        anyProduct = new AnyVaccineProduct().Generate();
        FreightShipment       shipment   = Journey.BookedReefer(1);
        List<ColdChainPallet> pallets    = [PalletOf(anyProduct)];

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(null!, anyProduct, shipment, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(anyId, null!, shipment, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(anyId, anyProduct, null!, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(anyId, anyProduct, shipment, null!));
    }

    [Fact]
    public void An_sscc_cannot_appear_twice() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();
        Sscc           anySscc    = new AnySscc().Generate();
        ColdChainPallet first     = new AnyColdChainPallet().ForProduct(anyProduct).WithSscc(anySscc).Generate();
        ColdChainPallet second    = new AnyColdChainPallet().ForProduct(anyProduct).WithSscc(anySscc).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), [first, second]));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Fact]
    public void Each_pallet_travels_with_its_own_logger() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();
        SensorId       anySensor  = new AnySensorId().Generate();
        ColdChainPallet first     = new AnyColdChainPallet().ForProduct(anyProduct).WithLogger(l => l.WithId(anySensor).WithRecordingIntervalMinutes(1)).Generate();
        ColdChainPallet second    = new AnyColdChainPallet().ForProduct(anyProduct).WithLogger(l => l.WithId(anySensor).WithRecordingIntervalMinutes(1)).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), [first, second]));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Fact]
    public void A_serialised_pack_cannot_be_on_two_pallets() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();
        SerialNumber   anySerial  = new AnySerialNumber().Generate();
        SerialisedPack firstPack  = new AnySerialisedPack().WithGtin(anyProduct.Gtin).WithSerial(anySerial).Generate();
        SerialisedPack secondPack = new AnySerialisedPack().WithGtin(anyProduct.Gtin).WithSerial(anySerial).Generate();
        ColdChainPallet first     = new AnyColdChainPallet().ForProduct(anyProduct).WithPacks([firstPack]).Generate();
        ColdChainPallet second    = new AnyColdChainPallet().ForProduct(anyProduct).WithPacks([secondPack]).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), [first, second]));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Fact]
    public void Every_pallet_carries_the_product_of_the_consignment() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();
        Gtin           otherGtin  = new AnyGtin().DifferentFrom(anyProduct.Gtin).Generate();
        List<ColdChainPallet> pallets = [PalletOf(anyProduct), new AnyColdChainPallet().ForProduct(anyProduct).OfProduct(otherGtin).Generate()];

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), pallets));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallets", refusal.ParamName);
    }

    [Theory]
    [InlineData(60, 60, true)]
    [InlineData(60, 10, true)]
    [InlineData(59, 60, false)]
    [InlineData(20, 20, true)]
    [InlineData(19, 20, false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void A_logger_must_record_at_least_as_often_as_the_single_excursion_allowance(int single, int interval, bool accepted) {
        // Arrange
        // A cumulative allowance of one week leaves any single allowance room.
        VaccineProduct  product = ProductWithAllowance(StabilityBudget.MaxCumulativeMinutes, single);
        ColdChainPallet pallet  = new AnyColdChainPallet().OfProduct(product.Gtin).WithLogger(l => l.WithRecordingIntervalMinutes(interval)).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ColdChainConsignment(new AnyConsignmentId().Generate(), product, Journey.BookedReefer(1), [pallet]));

        // Assert
        if (accepted) {
            Assert.Null(thrown);
        } else {
            ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
            Assert.Equal("pallets", refusal.ParamName);
        }
    }

    [Fact]
    public void A_product_with_no_single_excursion_allowance_puts_no_condition_on_the_logger() {
        // Arrange
        VaccineProduct  zeroTolerance = ProductWithAllowance(0, 0);
        ColdChainPallet slowPallet    = new AnyColdChainPallet().OfProduct(zeroTolerance.Gtin).WithLogger(l => l.WithRecordingIntervalMinutes(60)).Generate();

        // Act
        ColdChainConsignment consignment = new ColdChainConsignment(new AnyConsignmentId().Generate(), zeroTolerance, Journey.BookedReefer(1), [slowPallet]);

        // Assert
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
    }

    [Fact]
    public void The_pallet_list_is_a_copy_the_caller_cannot_change_afterwards() {
        // Arrange
        VaccineProduct        anyProduct = new AnyVaccineProduct().Generate();
        List<ColdChainPallet> pallets    = [PalletOf(anyProduct)];
        ColdChainConsignment  consignment = new ColdChainConsignment(new AnyConsignmentId().Generate(), anyProduct, Journey.BookedReefer(1), pallets);

        // Act
        pallets.Add(PalletOf(anyProduct));

        // Assert
        Assert.Single(consignment.Pallets);
    }

    [Fact]
    public void The_generator_only_draws_consignments_the_constructor_accepts() {
        // Act
        ColdChainConsignment anyConsignment = new AnyColdChainConsignment().Generate();

        // Assert
        Assert.Equal(ContainerType.Reefer40, anyConsignment.Shipment.ContainerType);
        Assert.All(anyConsignment.Pallets, pallet => Assert.Equal(anyConsignment.Product.Gtin, pallet.Gtin));
    }

}
