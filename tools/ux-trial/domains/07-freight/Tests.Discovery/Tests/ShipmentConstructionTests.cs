using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class ShipmentConstructionTests {

    [Fact]
    public void A_new_shipment_is_booked_and_has_neither_departed_nor_been_delivered() {
        // Arrange
        Guid           id        = Guid.Parse("11111111-2222-3333-4444-555555555555");
        ContainerCode  container = new ContainerCode("CSQU3054383");
        Weight         weight    = new Weight(10_000);
        Volume         volume    = new Volume(20);
        Leg            leg       = Calendar.LeHavreToShanghai();

        // Act
        Shipment shipment = new Shipment(id, container, ContainerType.Dry20, weight, volume, new[] { leg });

        // Assert
        Assert.Equal(id, shipment.Id);
        Assert.Equal(container, shipment.Container);
        Assert.Equal(ContainerType.Dry20, shipment.ContainerType);
        Assert.Equal(weight, shipment.GrossWeight);
        Assert.Equal(volume, shipment.Volume);
        Assert.Same(leg, Assert.Single(shipment.Legs));
        Assert.Equal(ShipmentStatus.Booked, shipment.Status);
        Assert.Null(shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Fact]
    public void The_itinerary_is_copied() {
        // Arrange
        List<Leg> legs = new List<Leg> { Calendar.LeHavreToShanghai() };
        Shipment  anyShipment = Any.Shipment().WithLegs(legs).Generate();

        // Act
        legs.Clear();

        // Assert
        Assert.Single(anyShipment.Legs);
    }

    [Fact]
    public void A_shipment_without_identifier_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Shipment(Guid.Empty, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));
    }

    [Fact]
    public void A_missing_container_is_refused_with_an_argument_null_exception() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentNullException>(() => new Shipment(anyShipment.Id, null!, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));
    }

    [Fact]
    public void A_missing_weight_is_refused_with_an_argument_null_exception() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentNullException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, null!, anyShipment.Volume, anyShipment.Legs));
    }

    [Fact]
    public void A_missing_volume_is_refused_with_an_argument_null_exception() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentNullException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, null!, anyShipment.Legs));
    }

    [Fact]
    public void A_missing_itinerary_is_refused_with_an_argument_null_exception() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentNullException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, null!));
    }

    [Fact]
    public void An_empty_itinerary_is_refused() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new List<Leg>()));
    }

    [Fact]
    public void An_undefined_container_type_is_refused_with_an_argument_out_of_range_exception() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Shipment(anyShipment.Id, anyShipment.Container, (ContainerType)42, anyShipment.GrossWeight, anyShipment.Volume, anyShipment.Legs));
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200)]
    [InlineData(ContainerType.Dry40, 26_700)]
    [InlineData(ContainerType.HighCube40, 26_500)]
    [InlineData(ContainerType.Reefer40, 27_700)]
    public void A_weight_equal_to_the_payload_is_accepted_and_a_gram_more_is_refused(ContainerType type, double payload) {
        // Arrange
        Shipment anyShipment = Any.Shipment().WithContainerType(type).Generate();

        // Act
        Shipment full = new Shipment(anyShipment.Id, anyShipment.Container, type, new Weight(payload), anyShipment.Volume, anyShipment.Legs);

        // Assert
        Assert.Equal(payload, full.GrossWeight.Kilograms);
        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, type, new Weight(payload + 0.001), anyShipment.Volume, anyShipment.Legs));
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 33.2, 33.3)]
    [InlineData(ContainerType.Dry40, 67.7, 67.8)]
    [InlineData(ContainerType.Reefer40, 59.3, 59.4)]
    public void A_volume_equal_to_the_capacity_is_accepted_and_a_little_more_is_refused(ContainerType type, double capacity, double slightlyMore) {
        // Arrange
        Shipment anyShipment = Any.Shipment().WithContainerType(type).Generate();

        // Act
        Shipment full = new Shipment(anyShipment.Id, anyShipment.Container, type, anyShipment.GrossWeight, new Volume(capacity), anyShipment.Legs);

        // Assert
        Assert.Equal(capacity, full.Volume.CubicMetres);
        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, type, anyShipment.GrossWeight, new Volume(slightlyMore), anyShipment.Legs));
    }

    [Fact]
    public void A_high_cube_takes_the_largest_volume_a_volume_can_hold() {
        // Arrange
        Shipment anyShipment = Any.Shipment().WithContainerType(ContainerType.HighCube40).Generate();

        // Act
        Shipment full = new Shipment(anyShipment.Id, anyShipment.Container, ContainerType.HighCube40, anyShipment.GrossWeight, new Volume(Volume.MaxCubicMetres), anyShipment.Legs);

        // Assert
        Assert.Equal(1.0, full.VolumeUtilisation);
    }

    [Fact]
    public void A_volume_that_fits_a_larger_container_is_refused_in_a_smaller_one() {
        // Arrange: a light cargo, so only the volume decides
        Shipment anyShipment = Any.Shipment().WithGrossWeight(new Weight(1_000)).Generate();

        // Assert: 40 m3 fits a Dry40 (67.7) but not a Dry20 (33.2)
        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, ContainerType.Dry20, anyShipment.GrossWeight, new Volume(40), anyShipment.Legs));
        Assert.Equal(40, new Shipment(anyShipment.Id, anyShipment.Container, ContainerType.Dry40, anyShipment.GrossWeight, new Volume(40), anyShipment.Legs).Volume.CubicMetres);
    }

    [Fact]
    public void Consecutive_legs_must_be_contiguous_in_place() {
        // Arrange
        Leg first = Calendar.Leg("FRLEH", "GBSOU", Calendar.At(1), Calendar.At(3));
        Leg other = Calendar.Leg("NLRTM", "CNSHA", Calendar.At(4), Calendar.At(30));   // starts at Rotterdam, not Southampton

        Shipment anyShipment = Any.Shipment().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, other }));
    }

    [Fact]
    public void The_next_leg_cannot_depart_before_the_previous_one_arrives() {
        // Arrange
        Leg first = Calendar.Leg("FRLEH", "GBSOU", Calendar.At(1), Calendar.At(3));
        Leg next  = Calendar.Leg("GBSOU", "CNSHA", Calendar.At(3).AddTicks(-1), Calendar.At(30));

        Shipment anyShipment = Any.Shipment().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, next }));
    }

    [Fact]
    public void The_next_leg_may_depart_at_the_very_instant_the_previous_one_arrives() {
        // Arrange
        Leg first = Calendar.Leg("FRLEH", "GBSOU", Calendar.At(1), Calendar.At(3));
        Leg next  = Calendar.Leg("GBSOU", "CNSHA", Calendar.At(3), Calendar.At(30));

        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        Shipment shipment = new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, next });

        // Assert
        Assert.Equal(2, shipment.Legs.Count);
    }

    [Fact]
    public void A_three_leg_itinerary_is_checked_pair_by_pair() {
        // Arrange: the second hand-over is the broken one
        Leg first  = Calendar.Leg("FRLEH", "GBSOU", Calendar.At(1), Calendar.At(3));
        Leg second = Calendar.Leg("GBSOU", "NLRTM", Calendar.At(4), Calendar.At(6));
        Leg third  = Calendar.Leg("BEANR", "CNSHA", Calendar.At(7), Calendar.At(30));

        Shipment anyShipment = Any.Shipment().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Shipment(anyShipment.Id, anyShipment.Container, anyShipment.ContainerType, anyShipment.GrossWeight, anyShipment.Volume, new[] { first, second, third }));
    }

}
