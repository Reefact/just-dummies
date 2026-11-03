using AgriTech.Domain;
using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using Freight.Domain;
using JustDummies;
using Serialisation.Domain;
using Warehouse.Domain;
using Xunit;
using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class ConsignmentConstructionTests {

    [Fact]
    public void A_consignment_starts_planned_with_nothing_yet_dispatched_received_or_decided() {
        // Act
        ColdChainConsignment consignment = Any.ColdChainConsignment().Generate();

        // Assert
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
        Assert.Null(consignment.DispatchedAt);
        Assert.Null(consignment.DeliveredAt);
        Assert.Null(consignment.ReceivedBy);
        Assert.Empty(consignment.Custody);
        Assert.Empty(consignment.Decisions);
        Assert.Null(consignment.CurrentLeg);
    }

    [Fact]
    public void A_consignment_keeps_its_pallets_in_loading_order_and_counts_all_their_packs() {
        // Arrange
        Gtin sharedGtin = Any.Gtin().Generate();

        // A logger recording every minute resolves whatever allowance the drawn product has.
        ColdChainPallet first  = Any.ColdChainPallet().WithGtin(sharedGtin).WithPackCount(3).WithLogger(logger => logger.WithRecordingIntervalMinutes(1)).Generate();
        ColdChainPallet second = Any.ColdChainPallet().WithGtin(sharedGtin).WithPackCount(4).WithLogger(logger => logger.WithRecordingIntervalMinutes(1)).Generate();

        // Act
        ColdChainConsignment consignment = Any.ColdChainConsignment().WithProduct(product => product.WithGtin(sharedGtin)).WithPallets([first, second]).Generate();

        // Assert
        Assert.Equal([first, second], consignment.Pallets);
        Assert.Equal(7, consignment.TotalPacks);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(ColdChainConsignment.MaxPallets)]
    public void A_reefer_takes_one_to_twenty_pallets(int palletCount) {
        // Act
        ColdChainConsignment consignment = Any.ColdChainConsignment().WithPalletCount(palletCount).Generate();

        // Assert
        Assert.Equal(palletCount, consignment.Pallets.Count);
    }

    [Fact]
    public void A_consignment_without_pallets_is_refused() {
        // Arrange
        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithPallets(Array.Empty<ColdChainPallet>());

        // Assert
        Rejected.For("1 to 20 pallets", () => consignments.Generate());
    }

    [Fact]
    public void A_consignment_of_twenty_one_pallets_is_refused() {
        // Arrange
        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithPalletCount(ColdChainConsignment.MaxPallets + 1);

        // Assert
        Rejected.For("1 to 20 pallets", () => consignments.Generate());
    }

    [Fact]
    public void A_consignment_listing_a_missing_pallet_is_refused() {
        // Arrange
        Gtin            sharedGtin = Any.Gtin().Generate();
        ColdChainPallet anyPallet  = Any.ColdChainPallet().WithGtin(sharedGtin).Generate();

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithProduct(product => product.WithGtin(sharedGtin)).WithPallets([anyPallet, null!]);

        // Assert
        Rejected.For("missing pallet", () => consignments.Generate());
    }

    [Fact]
    public void A_shipment_already_on_its_way_cannot_be_planned_on() {
        // Arrange
        FreightShipment departed = Any.Shipment().WithContainerType(ContainerType.Reefer40).Generate();

        departed.Depart(Journey.Start);

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithShipment(departed);

        // Assert
        Rejected.For("booked shipment", () => consignments.Generate());
    }

    [Fact]
    public void A_cancelled_shipment_cannot_be_planned_on() {
        // Arrange
        FreightShipment cancelled = Any.Shipment().WithContainerType(ContainerType.Reefer40).Generate();

        cancelled.Cancel();

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithShipment(cancelled);

        // Assert
        Rejected.For("booked shipment", () => consignments.Generate());
    }

    [Theory]
    [InlineData(ContainerType.Dry20)]
    [InlineData(ContainerType.Dry40)]
    [InlineData(ContainerType.HighCube40)]
    public void A_shipment_in_a_container_that_is_not_a_reefer_is_refused(ContainerType containerType) {
        // Arrange
        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithShipment(shipment => shipment.WithContainerType(containerType));

        // Assert
        Rejected.For("reefer", () => consignments.Generate());
    }

    [Fact]
    public void Two_pallets_with_the_same_sscc_are_refused() {
        // Arrange
        Gtin sharedGtin = Any.Gtin().Generate();
        Sscc sharedSscc = Any.Sscc().Generate();

        ColdChainPallet first  = Any.ColdChainPallet().WithGtin(sharedGtin).WithSscc(sharedSscc).Generate();
        ColdChainPallet second = Any.ColdChainPallet().WithGtin(sharedGtin).WithSscc(sharedSscc).Generate();

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithProduct(product => product.WithGtin(sharedGtin)).WithPallets([first, second]);

        // Assert
        Rejected.For("SSCC appears twice", () => consignments.Generate());
    }

    [Fact]
    public void Two_pallets_sharing_a_logger_are_refused() {
        // Arrange
        Gtin     sharedGtin   = Any.Gtin().Generate();
        SensorId sharedSensor = Any.SensorId().Generate();

        ColdChainPallet first  = Any.ColdChainPallet().WithGtin(sharedGtin).WithLogger(logger => logger.WithId(sharedSensor)).Generate();
        ColdChainPallet second = Any.ColdChainPallet().WithGtin(sharedGtin).WithLogger(logger => logger.WithId(sharedSensor)).Generate();

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithProduct(product => product.WithGtin(sharedGtin)).WithPallets([first, second]);

        // Assert
        Rejected.For("own logger", () => consignments.Generate());
    }

    [Fact]
    public void A_serialised_pack_cannot_be_on_two_pallets() {
        // Arrange
        Gtin         sharedGtin   = Any.Gtin().Generate();
        SerialNumber sharedSerial = Any.SerialNumber().Generate();

        ColdChainPallet first  = Any.ColdChainPallet().WithPacks([Any.SerialisedPack().WithGtin(sharedGtin).WithSerial(sharedSerial).Generate()]).Generate();
        ColdChainPallet second = Any.ColdChainPallet().WithPacks([Any.SerialisedPack().WithGtin(sharedGtin).WithSerial(sharedSerial).Generate()]).Generate();

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithProduct(product => product.WithGtin(sharedGtin)).WithPallets([first, second]);

        // Assert
        Rejected.For("two pallets", () => consignments.Generate());
    }

    [Fact]
    public void A_pallet_of_another_product_than_the_consignments_is_refused() {
        // Arrange
        Gtin productGtin = Any.Gtin().Generate();
        Gtin palletGtin  = new AnyGtin().DifferentFrom(productGtin).Generate();

        ColdChainPallet foreign = Any.ColdChainPallet().WithGtin(palletGtin).Generate();

        AnyColdChainConsignment consignments = Any.ColdChainConsignment().WithProduct(product => product.WithGtin(productGtin)).WithPallets([foreign]);

        // Assert
        Rejected.For("carries", () => consignments.Generate());
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(1, true)]
    [InlineData(16, false)]
    [InlineData(60, false)]
    public void A_logger_must_record_at_least_as_often_as_the_single_excursion_allowance_it_is_to_resolve(int loggerInterval, bool expectedAccepted) {
        // Arrange
        AnyColdChainConsignment consignments = Any.ColdChainConsignment()
                                                  .WithProduct(product => product.WithBudget(new StabilityBudget(120, 15)))
                                                  .WithEachPallet(pallet => pallet.WithLogger(logger => logger.WithRecordingIntervalMinutes(loggerInterval)));

        // Assert
        if (expectedAccepted) {
            Assert.NotNull(consignments.Generate());
        } else {
            Rejected.For("cannot resolve", () => consignments.Generate());
        }
    }

    [Fact]
    public void A_product_with_no_single_excursion_allowance_puts_no_demand_on_the_loggers_interval() {
        // Arrange
        AnyColdChainConsignment consignments = Any.ColdChainConsignment()
                                                  .WithProduct(product => product.WithBudget(new StabilityBudget(0, 0)))
                                                  .WithEachPallet(pallet => pallet.WithLogger(logger => logger.WithRecordingIntervalMinutes(60)));

        // Assert
        Assert.NotNull(consignments.Generate());
    }

    [Fact]
    public void A_consignment_needs_its_number_product_shipment_and_pallets() {
        // Arrange
        ConsignmentId                id       = Any.ConsignmentId().Generate();
        VaccineProduct               product  = Any.VaccineProduct().Generate();
        FreightShipment              shipment = Any.Shipment().WithContainerType(ContainerType.Reefer40).Generate();
        IReadOnlyList<ColdChainPallet> pallets = [Any.ColdChainPallet().WithGtin(product.Gtin).Generate()];

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(null!, product, shipment, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(id, null!, shipment, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(id, product, null!, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(id, product, shipment, null!));
    }

}
