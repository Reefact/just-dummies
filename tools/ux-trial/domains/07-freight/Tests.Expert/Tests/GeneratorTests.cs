using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

// The generators are test infrastructure: these tests keep them honest, so that a failure elsewhere is never theirs.
public sealed class GeneratorTests {

    [Fact]
    public void Shipments_of_any_kind_can_be_drawn_in_numbers() {
        List<Shipment> anyShipments = Any.ListOf(Any.Shipment()).WithCount(300).Generate();

        Assert.Equal(300, anyShipments.Count);
    }

    [Fact]
    public void Leg_durations_stay_within_the_transit_ceiling() {
        List<Leg> anyLegs = Any.ListOf(Any.Leg()).WithCount(300).Generate();

        Assert.All(anyLegs, leg => Assert.InRange(leg.Duration, TimeSpan.FromTicks(1), Leg.MaxTransit));
    }

    [Fact]
    public void A_pinned_load_chooses_a_container_that_can_carry_it() {
        Shipment heavy = Any.Shipment().WithGrossWeight(new Weight(28_000)).Generate();
        Shipment large = Any.Shipment().WithVolume(new Volume(70)).Generate();

        Assert.Equal(ContainerType.Dry20, heavy.ContainerType);
        Assert.Equal(ContainerType.HighCube40, large.ContainerType);
    }

    [Fact]
    public void A_pinned_container_type_bounds_the_load() {
        Shipment anyDry20 = Any.Shipment().WithContainerType(ContainerType.Dry20).Generate();

        Assert.True(anyDry20.GrossWeight.Kilograms <= 28_200);
        Assert.True(anyDry20.Volume.CubicMetres <= 33.2);
    }

    [Fact]
    public void An_itinerary_can_be_asked_for_its_leg_count() {
        Shipment anyThreeLegShipment = Any.Shipment().WithLegs(legs => legs.WithLegCount(3)).Generate();

        Assert.Equal(3, anyThreeLegShipment.Legs.Count);
    }

}
