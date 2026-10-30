using Freight.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Freight.Tests;

[Reproducible]
public sealed class ShipmentTests {

    private static readonly TimeSpan Rotterdam = TimeSpan.FromHours(1);
    private static readonly TimeSpan Shanghai  = TimeSpan.FromHours(8);

    /// <summary>A leg arriving exactly when the test says; where from and when it left do not matter.</summary>
    private static Leg AnyLegArrivingAt(DateTimeOffset arrival) {
        return Arbitrary.Leg()
                        .WithArrival(arrival)
                        .WithDeparture(departure => departure.Between(arrival.AddDays(-60), arrival.AddHours(-1)))
                        .Generate();
    }

    [Fact]
    public void Estimated_arrival_can_be_read_at_another_port_offset_as_the_same_instant() {
        // Arrange
        DateTimeOffset arrivalInRotterdam = new(2025, 3, 10, 8, 0, 0, Rotterdam);
        Shipment       anyShipment        = Arbitrary.Shipment().WithLegs([AnyLegArrivingAt(arrivalInRotterdam)]).Generate();

        // Act
        DateTimeOffset seenFromShanghai = anyShipment.EstimatedArrivalAt(Shanghai);

        // Assert
        Assert.Equal(new DateTime(2025, 3, 10, 15, 0, 0), seenFromShanghai.DateTime);
        Assert.Equal(Shanghai, seenFromShanghai.Offset);
    }

    [Fact]
    public void Planned_transit_time_is_measured_between_instants_not_local_clocks() {
        // Arrange
        DateTimeOffset departsShanghai   = new(2025, 6, 1, 10, 0, 0, Shanghai);
        DateTimeOffset arrivesRotterdam  = new(2025, 6, 1, 10, 0, 0, Rotterdam);
        Leg            anyDirectSailing  = Arbitrary.Leg().WithDeparture(departsShanghai).WithArrival(arrivesRotterdam).Generate();
        Shipment       anyShipment       = Arbitrary.Shipment().WithLegs([anyDirectSailing]).Generate();

        // Act
        TimeSpan transit = anyShipment.PlannedTransitTime;

        // Assert
        Assert.Equal(TimeSpan.FromHours(7), transit);
    }

    [Fact]
    public void Estimated_arrival_is_the_arrival_of_the_last_leg() {
        // Arrange
        DateTimeOffset firstArrival = new(2025, 4, 2, 6, 0, 0, Shanghai);
        DateTimeOffset finalArrival = new(2025, 5, 9, 17, 30, 0, Rotterdam);
        Leg            anyFeeder    = AnyLegArrivingAt(firstArrival);
        Leg            anyMainLine  = Arbitrary.Leg()
                                               .WithOrigin(anyFeeder.Destination)
                                               .WithDeparture(departure => departure.Between(firstArrival, finalArrival.AddHours(-1)))
                                               .WithArrival(finalArrival)
                                               .Generate();
        Shipment anyShipment = Arbitrary.Shipment().WithLegs([anyFeeder, anyMainLine]).Generate();

        // Act
        DateTimeOffset eta = anyShipment.EstimatedArrival;

        // Assert
        Assert.Equal(finalArrival, eta);
    }

    [Fact]
    public void Dense_cargo_is_charged_by_its_weight_in_metric_tons() {
        // Arrange
        Shipment anyDenseShipment = Arbitrary.Shipment()
                                             .WithGrossWeight(new Weight(20_000))
                                             .WithVolume(new Volume(12))
                                             .Generate();

        // Act
        double revenueTons = anyDenseShipment.RevenueTons;

        // Assert
        Assert.Equal(20, revenueTons);
    }

    [Fact]
    public void Light_cargo_is_charged_by_its_volume_in_cubic_metres() {
        // Arrange
        Shipment anyLightShipment = Arbitrary.Shipment()
                                             .WithGrossWeight(new Weight(3_000))
                                             .WithVolume(new Volume(30))
                                             .Generate();

        // Act
        double revenueTons = anyLightShipment.RevenueTons;

        // Assert
        Assert.Equal(30, revenueTons);
    }

    [Fact]
    public void A_quote_is_the_rate_times_the_revenue_tons_rounded_to_the_cent() {
        // Arrange
        Shipment    anyShipment = Arbitrary.Shipment().WithGrossWeight(new Weight(10_505)).WithVolume(new Volume(7.25)).Generate();
        FreightRate anyRate     = Arbitrary.FreightRate().WithPerRevenueTon(45.50m).Generate();

        // Act
        decimal quote = anyShipment.QuoteWith(anyRate);

        // Assert
        Assert.Equal(477.98m, quote); // 10.505 RT x 45.50 = 477.9775
    }

    [Fact]
    public void Volume_utilisation_is_the_share_of_the_container_capacity() {
        // Arrange
        Shipment anyHalfFullHighCube = Arbitrary.Shipment()
                                                .WithContainerType(ContainerType.HighCube40)
                                                .WithVolume(new Volume(38.2))
                                                .Generate();

        // Act
        double utilisation = anyHalfFullHighCube.VolumeUtilisation;

        // Assert
        Assert.Equal(0.5, utilisation, 10);
    }

    [Fact]
    public void A_payload_heavier_than_the_container_type_allows_is_refused() {
        // Arrange
        AnyShipment anyOverweightDry40 = Arbitrary.Shipment()
                                                  .WithContainerType(ContainerType.Dry40)
                                                  .WithGrossWeight(new Weight(26_701));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyOverweightDry40.Generate());

        // Assert
        Assert.Equal("grossWeight", refusal.ParamName);
    }

    [Fact]
    public void Legs_that_do_not_connect_are_refused() {
        // Arrange
        Leg      anyFirstLeg      = Arbitrary.Leg()
                                             .WithDestination(new UnLocode("NLRTM"))
                                             .WithDeparture(departure => departure.Before(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)))
                                             .Generate();
        Leg      anyDisconnected  = Arbitrary.Leg()
                                             .WithOrigin(new UnLocode("BEANR"))
                                             .WithDeparture(departure => departure.After(anyFirstLeg.Arrival))
                                             .Generate();
        AnyShipment anyItinerary = Arbitrary.Shipment().WithLegs([anyFirstLeg, anyDisconnected]);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyItinerary.Generate());

        // Assert
        Assert.Contains("starts at BEANR, not at NLRTM", refusal.Message);
    }

    [Fact]
    public void A_booked_shipment_that_departs_is_in_transit() {
        // Arrange
        Shipment       anyBookedShipment = Arbitrary.Shipment().Generate();
        DateTimeOffset anyDepartureTime  = Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14)).Generate();

        // Act
        anyBookedShipment.Depart(anyDepartureTime);

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, anyBookedShipment.Status);
    }

    [Fact]
    public void A_shipment_still_booked_cannot_be_delivered() {
        // Arrange
        Shipment       anyBookedShipment = Arbitrary.Shipment().Generate();
        DateTimeOffset anyDeliveryTime   = Any.DateTimeOffset().Generate();

        // Act
        Action delivering = () => anyBookedShipment.Deliver(anyDeliveryTime);

        // Assert
        Assert.Throws<InvalidOperationException>(delivering);
    }

    [Fact]
    public void A_delivery_is_ordered_after_the_departure_by_instant_not_by_local_clock() {
        // Arrange
        DateTimeOffset departedLeHavre    = new(2025, 7, 1, 10, 0, 0, TimeSpan.FromHours(2)); // 08:00 UTC
        DateTimeOffset deliveredFelixstowe = new(2025, 7, 1, 9, 30, 0, TimeSpan.FromHours(1)); // 08:30 UTC
        Shipment       anyShipment        = Arbitrary.Shipment().Generate();
        anyShipment.Depart(departedLeHavre);

        // Act
        anyShipment.Deliver(deliveredFelixstowe);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, anyShipment.Status);
    }

    [Fact]
    public void A_shipment_in_transit_cannot_be_cancelled() {
        // Arrange
        Shipment anyShipment = Arbitrary.Shipment().Generate();
        anyShipment.Depart(Any.DateTimeOffset().Generate());

        // Act
        Action cancelling = anyShipment.Cancel;

        // Assert
        Assert.Throws<InvalidOperationException>(cancelling);
    }

    [Fact]
    public void A_shipment_is_late_once_its_eta_instant_has_passed_even_if_the_local_clock_reads_earlier() {
        // Arrange
        DateTimeOffset etaInRotterdam = new(2025, 3, 10, 8, 0, 0, Rotterdam);           // 07:00 UTC
        DateTimeOffset nowInLondon    = new(2025, 3, 10, 7, 30, 0, TimeSpan.Zero);      // 07:30 UTC
        Shipment       anyShipment    = Arbitrary.Shipment().WithLegs([AnyLegArrivingAt(etaInRotterdam)]).Generate();

        // Act
        bool late = anyShipment.IsLateAt(nowInLondon);

        // Assert
        Assert.True(late);
    }

    [Fact]
    public void A_container_code_built_from_its_parts_carries_the_ISO_6346_check_digit() {
        // Act
        ContainerCode code = ContainerCode.Create("CSQ", 'U', 305438);

        // Assert
        Assert.Equal("CSQU3054383", code.Value);
    }

    [Fact]
    public void A_container_code_with_a_wrong_check_digit_is_refused() {
        // Act
        Action parsing = () => new ContainerCode("CSQU3054384");

        // Assert
        Assert.Throws<ArgumentException>(parsing);
    }

}
