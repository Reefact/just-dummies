using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class ShipmentConstructionTests {

    private static readonly DateTimeOffset FirstDeparture = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_shipment_is_booked_with_nothing_departed_or_delivered() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Equal(ShipmentStatus.Booked, anyShipment.Status);
        Assert.Null(anyShipment.DepartedAt);
        Assert.Null(anyShipment.DeliveredAt);
    }

    [Fact]
    public void A_shipment_keeps_what_it_was_built_from() {
        Guid          id        = Guid.Parse("4f2a9c1e-6d3b-4a8e-9b7c-1d2e3f405162");
        ContainerCode container = new ContainerCode("CSQU3054383");
        Weight        weight    = new Weight(21_000);
        Volume        volume    = new Volume(30);
        Leg           leg       = Any.Leg().Generate();

        Shipment shipment = new Shipment(id, container, ContainerType.Dry20, weight, volume, [leg]);

        Assert.Equal(id, shipment.Id);
        Assert.Same(container, shipment.Container);
        Assert.Equal(ContainerType.Dry20, shipment.ContainerType);
        Assert.Same(weight, shipment.GrossWeight);
        Assert.Same(volume, shipment.Volume);
        Assert.Same(leg, Assert.Single(shipment.Legs));
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(Guid.Empty, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));

        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_shipment_without_a_container_is_refused_as_missing() {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, null!, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));

        Assert.Equal("container", refusal.ParamName);
    }

    [Fact]
    public void A_shipment_without_a_gross_weight_is_refused_as_missing() {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, null!, anyShipment.Volume, anyShipment.Legs));

        Assert.Equal("grossWeight", refusal.ParamName);
    }

    [Fact]
    public void A_shipment_without_a_volume_is_refused_as_missing() {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, null!, anyShipment.Legs));

        Assert.Equal("volume", refusal.ParamName);
    }

    [Fact]
    public void A_shipment_without_an_itinerary_is_refused_as_missing() {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, null!));

        Assert.Equal("legs", refusal.ParamName);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void An_undefined_container_type_is_refused_before_any_load_is_compared_to_it(int undefined) {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, (ContainerType)undefined, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));

        Assert.Equal("containerType", refusal.ParamName);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200)]
    [InlineData(ContainerType.Dry40, 26_700)]
    [InlineData(ContainerType.HighCube40, 26_500)]
    [InlineData(ContainerType.Reefer40, 27_700)]
    public void A_load_equal_to_the_payload_of_the_container_is_accepted(ContainerType type, double payloadKilograms) {
        Shipment anyShipment = Any.Shipment().WithContainerType(type).Generate();

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, type, new Weight(payloadKilograms), anyShipment.Volume, anyShipment.Legs);

        Assert.Equal(payloadKilograms, shipment.GrossWeight.Kilograms);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200.01)]
    [InlineData(ContainerType.Dry40, 26_700.01)]
    [InlineData(ContainerType.HighCube40, 26_500.01)]
    [InlineData(ContainerType.Reefer40, 27_700.01)]
    [InlineData(ContainerType.HighCube40, Weight.MaxGrossKilograms)]
    public void A_load_above_the_payload_of_the_container_is_refused(ContainerType type, double kilograms) {
        Shipment anyShipment = Any.Shipment().WithContainerType(type).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, type, new Weight(kilograms), anyShipment.Volume, anyShipment.Legs));

        Assert.Equal("grossWeight", refusal.ParamName);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 33.2)]
    [InlineData(ContainerType.Dry40, 67.7)]
    [InlineData(ContainerType.HighCube40, 76.4)]
    [InlineData(ContainerType.Reefer40, 59.3)]
    public void A_volume_equal_to_the_capacity_of_the_container_is_accepted(ContainerType type, double capacityCubicMetres) {
        Shipment anyShipment = Any.Shipment().WithContainerType(type).Generate();

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, type, anyShipment.GrossWeight, new Volume(capacityCubicMetres), anyShipment.Legs);

        Assert.Equal(capacityCubicMetres, shipment.Volume.CubicMetres);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 33.21)]
    [InlineData(ContainerType.Dry40, 67.71)]
    [InlineData(ContainerType.Reefer40, 59.31)]
    [InlineData(ContainerType.Dry20, Volume.MaxCubicMetres)]
    public void A_volume_above_the_capacity_of_the_container_is_refused(ContainerType type, double cubicMetres) {
        Shipment anyShipment = Any.Shipment().WithContainerType(type).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, type, anyShipment.GrossWeight, new Volume(cubicMetres), anyShipment.Legs));

        Assert.Equal("volume", refusal.ParamName);
    }

    [Fact]
    public void An_itinerary_needs_at_least_one_leg() {
        Shipment anyShipment = Any.Shipment().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, []));

        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void A_leg_must_start_where_the_previous_one_ended() {
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg      elsewhere   = Any.Leg()
                                  .WithOrigin(origin => origin.WithCode(code => code.DifferentFrom(first.Destination.Code)))
                                  .WithDeparture(FirstDeparture.AddDays(11))
                                  .Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, [first, elsewhere]));

        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void A_leg_may_not_depart_before_the_previous_one_arrived() {
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg      early       = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival.AddTicks(-1)).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, [first, early]));

        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void A_leg_may_depart_the_very_instant_the_previous_one_arrived() {
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg      onTheDot    = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival).Generate();

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, [first, onTheDot]);

        Assert.Equal(2, shipment.Legs.Count);
    }

    [Fact]
    public void Departure_is_compared_with_arrival_as_instants_whatever_the_offset_of_each_port() {
        Shipment       anyShipment = Any.Shipment().Generate();
        DateTimeOffset arrivalAtTenInTheEvening = new DateTimeOffset(2026, 3, 11, 22, 0, 0, TimeSpan.FromHours(2));    // 20:00 UTC
        DateTimeOffset departureAtEightUtc      = new DateTimeOffset(2026, 3, 11, 20, 0, 0, TimeSpan.Zero);
        Leg            first                    = Any.Leg().WithDeparture(FirstDeparture).WithArrival(arrivalAtTenInTheEvening).Generate();
        Leg            sameInstant              = Any.Leg().WithOrigin(first.Destination).WithDeparture(departureAtEightUtc).Generate();

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, [first, sameInstant]);

        Assert.Equal(2, shipment.Legs.Count);
    }

    [Fact]
    public void A_layover_between_two_legs_is_accepted() {
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg      afterAWeek  = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival.AddDays(7)).Generate();

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, [first, afterAWeek]);

        Assert.Equal(2, shipment.Legs.Count);
    }

    [Fact]
    public void A_three_leg_itinerary_chained_end_to_end_is_accepted() {
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg      second      = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival).WithArrival(first.Arrival.AddDays(1)).Generate();
        Leg      third       = Any.Leg().WithOrigin(second.Destination).WithDeparture(second.Arrival).WithArrival(second.Arrival.AddDays(1)).Generate();

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, [first, second, third]);

        Assert.Equal(3, shipment.Legs.Count);
    }

    [Fact]
    public void The_itinerary_is_copied_so_the_caller_cannot_change_it_afterwards() {
        Shipment  anyShipment = Any.Shipment().WithLegs(legs => legs.WithLegCount(2)).Generate();
        List<Leg> itinerary   = [.. anyShipment.Legs];

        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, itinerary);
        itinerary.Clear();

        Assert.Equal(2, shipment.Legs.Count);
    }

}
