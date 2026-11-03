using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

/// <summary>The hand-completed generators are test infrastructure: a few hundred draws must never be refused by the domain.</summary>
public sealed class GeneratorSanityTests {

    [Fact]
    public void The_hand_completed_generators_never_draw_a_value_the_domain_refuses() {
        // Act
        List<Shipment>    shipments = Any.ListOf(Any.Shipment()).WithCount(300).Generate();
        List<Leg>         legs      = Any.ListOf(Any.Leg()).WithCount(300).Generate();
        List<FreightRate> rates     = Any.ListOf(Any.FreightRate()).WithCount(300).Generate();

        // Assert: reaching this line is the assertion, since every constructor guard ran; check the ties too
        Assert.All(shipments, shipment => Assert.NotEmpty(shipment.Legs));
        Assert.All(legs, leg => Assert.NotEqual(leg.Origin, leg.Destination));
        Assert.All(rates, rate => Assert.Equal(rate.PerRevenueTon, decimal.Round(rate.PerRevenueTon, 2)));
    }

}
