namespace Freight.Domain.Tests;

public sealed class ShipmentConstructionTests {

    [Fact]
    public void A_new_shipment_is_booked_and_has_neither_departed_nor_been_delivered() {
        // Arrange
        Guid                anyId       = Any.Guid().NonEmpty().Generate();
        ContainerCode       anyCode     = Any.ContainerCode().Generate();
        ContainerType       anyType     = Any.Enum<ContainerType>().Generate();
        Weight              anyWeight   = Any.Weight().WithKilograms(k => k.LessThanOrEqualTo(anyType.MaxPayloadKilograms())).Generate();
        Volume              anyVolume   = Any.Volume().WithCubicMetres(v => v.LessThanOrEqualTo(anyType.CapacityCubicMetres())).Generate();
        IReadOnlyList<Leg>  anyLegs     = new AnyItinerary().Generate();

        // Act
        Shipment shipment = new Shipment(anyId, anyCode, anyType, anyWeight, anyVolume, anyLegs);

        // Assert
        Assert.Equal(anyId, shipment.Id);
        Assert.Same(anyCode, shipment.Container);
        Assert.Equal(anyType, shipment.ContainerType);
        Assert.Same(anyWeight, shipment.GrossWeight);
        Assert.Same(anyVolume, shipment.Volume);
        Assert.Equal(anyLegs, shipment.Legs);
        Assert.Equal(ShipmentStatus.Booked, shipment.Status);
        Assert.Null(shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Fact]
    public void A_drawn_shipment_is_always_constructible() {
        // Act
        Shipment anyShipment = Any.Shipment().Generate();

        // Assert
        Assert.NotEqual(Guid.Empty, anyShipment.Id);
        Assert.NotEmpty(anyShipment.Legs);
    }

    [Fact]
    public void The_legs_are_copied() {
        // Arrange
        List<Leg> anyLegs    = new List<Leg>(new AnyItinerary().WithLegCount(2).Generate());
        Shipment  shipment   = Any.Shipment().WithLegs(anyLegs).Generate();
        Leg       anyExtra   = Any.Leg().WithOrigin(anyLegs[^1].Destination).Generate();

        // Act
        anyLegs.Add(anyExtra);
        anyLegs.RemoveAt(0);

        // Assert
        Assert.Equal(2, shipment.Legs.Count);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(Guid.Empty, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void The_identifier_is_checked_first() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(Guid.Empty, null!, (ContainerType)99, null!, null!, null!));

        // Assert
        Assert.Equal("id", refusal.ParamName);
        Assert.IsNotType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_missing_container_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, null!, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));

        // Assert
        Assert.Equal("container", refusal.ParamName);
    }

    [Fact]
    public void A_missing_gross_weight_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, null!, anyShipment.Volume, anyShipment.Legs));

        // Assert
        Assert.Equal("grossWeight", refusal.ParamName);
    }

    [Fact]
    public void A_missing_volume_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, null!, anyShipment.Legs));

        // Assert
        Assert.Equal("volume", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_container_type_is_refused() {
        // Arrange
        Shipment      anyShipment = Any.Shipment().Generate();
        ContainerType undefined   = (ContainerType)Any.Int32().Between(4, 1_000).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, undefined, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));

        // Assert
        Assert.Equal("containerType", refusal.ParamName);
    }

    [Fact]
    public void The_container_type_is_checked_before_the_cargo() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();
        Weight   heavy       = new Weight(Weight.MaxGrossKilograms);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, (ContainerType)42, heavy, anyShipment.Volume, anyShipment.Legs));

        // Assert
        Assert.Equal("containerType", refusal.ParamName);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200)]
    [InlineData(ContainerType.Dry40, 26_700)]
    [InlineData(ContainerType.HighCube40, 26_500)]
    [InlineData(ContainerType.Reefer40, 27_700)]
    public void A_cargo_as_heavy_as_the_payload_is_accepted(ContainerType type, double payloadKilograms) {
        // Act
        Shipment shipment = Any.Shipment().WithContainerType(type).WithGrossWeight(new Weight(payloadKilograms)).Generate();

        // Assert
        Assert.Equal(payloadKilograms, shipment.GrossWeight.Kilograms);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200)]
    [InlineData(ContainerType.Dry40, 26_700)]
    [InlineData(ContainerType.HighCube40, 26_500)]
    [InlineData(ContainerType.Reefer40, 27_700)]
    public void A_cargo_above_the_payload_is_refused(ContainerType type, double payloadKilograms) {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();
        Weight   tooHeavy    = new Weight(double.BitIncrement(payloadKilograms));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, type, tooHeavy, anyShipment.Volume, anyShipment.Legs));

        // Assert
        Assert.Equal("grossWeight", refusal.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 33.2)]
    [InlineData(ContainerType.Dry40, 67.7)]
    [InlineData(ContainerType.HighCube40, 76.4)]
    [InlineData(ContainerType.Reefer40, 59.3)]
    public void A_cargo_as_large_as_the_capacity_is_accepted(ContainerType type, double capacityCubicMetres) {
        // Act
        Shipment shipment = Any.Shipment().WithContainerType(type).WithVolume(new Volume(capacityCubicMetres)).Generate();

        // Assert
        Assert.Equal(capacityCubicMetres, shipment.Volume.CubicMetres);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 33.2)]
    [InlineData(ContainerType.Dry40, 67.7)]
    [InlineData(ContainerType.Reefer40, 59.3)]
    public void A_cargo_above_the_capacity_is_refused(ContainerType type, double capacityCubicMetres) {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();
        Volume   tooLarge    = new Volume(double.BitIncrement(capacityCubicMetres));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, type, anyShipment.GrossWeight, tooLarge, anyShipment.Legs));

        // Assert
        Assert.Equal("volume", refusal.ParamName);
    }

    [Fact]
    public void The_weight_is_checked_before_the_volume() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, ContainerType.Dry20, new Weight(Weight.MaxGrossKilograms), new Volume(Volume.MaxCubicMetres), anyShipment.Legs));

        // Assert
        Assert.Equal("grossWeight", refusal.ParamName);
    }

    [Fact]
    public void The_cargo_is_checked_before_the_itinerary() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, ContainerType.Dry20, anyShipment.GrossWeight, new Volume(Volume.MaxCubicMetres), Array.Empty<Leg>()));

        // Assert
        Assert.Equal("volume", refusal.ParamName);
    }

    [Fact]
    public void A_missing_itinerary_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, null!));

        // Assert
        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void An_itinerary_without_legs_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, Array.Empty<Leg>()));

        // Assert
        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void A_single_leg_is_an_itinerary() {
        // Arrange
        Leg anyLeg = Any.Leg().Generate();

        // Act
        Shipment shipment = Any.Shipment().WithLegs(new[] { anyLeg }).Generate();

        // Assert
        Assert.Single(shipment.Legs);
    }

    [Fact]
    public void A_leg_that_starts_elsewhere_than_the_previous_one_ended_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        Leg      detached    = Any.Leg()
                                  .WithOrigin(origin => origin.WithCode(code => code.DifferentFrom(first.Destination.Code)))
                                  .WithDeparture(first.Arrival.AddDays(1))
                                  .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, detached }));

        // Assert
        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void A_leg_that_departs_before_the_previous_one_arrives_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();
        Leg      first       = Any.Leg().WithDeparture(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        Leg      early       = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival.AddTicks(-1)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, early }));

        // Assert
        Assert.Equal("legs", refusal.ParamName);
    }

    [Fact]
    public void A_leg_that_departs_the_instant_the_previous_one_arrives_is_accepted() {
        // Arrange
        Leg first   = Any.Leg().WithDeparture(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        Leg sameNow = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival).Generate();

        // Act
        Shipment shipment = Any.Shipment().WithLegs(new[] { first, sameNow }).Generate();

        // Assert
        Assert.Equal(2, shipment.Legs.Count);
    }

    [Fact]
    public void Departure_and_arrival_are_compared_as_instants_across_ports() {
        // Arrange: the next leg departs at 10:00 on the local clock of a +10:00 port, i.e. 00:00 UTC, before the previous 02:00 UTC arrival.
        Leg first = Any.Leg()
                       .WithDeparture(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
                       .WithArrival(new DateTimeOffset(2026, 1, 1, 2, 0, 0, TimeSpan.Zero))
                       .Generate();
        Leg next  = Any.Leg()
                       .WithOrigin(first.Destination)
                       .WithDeparture(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(10)))
                       .WithDuration(TimeSpan.FromDays(3))
                       .Generate();
        Shipment anyShipment = Any.Shipment().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, next }));
    }

    [Fact]
    public void A_leg_that_departs_after_the_previous_one_arrived_is_accepted() {
        // Arrange
        Leg first = Any.Leg().WithDeparture(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        Leg later = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival.AddDays(20)).Generate();

        // Act
        Shipment shipment = Any.Shipment().WithLegs(new[] { first, later }).Generate();

        // Assert
        Assert.Same(first, shipment.Legs[0]);
        Assert.Same(later, shipment.Legs[1]);
    }

}
