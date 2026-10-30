using JustDummies;
using JustDummies.Xunit;

using Smoke.Domain;

using Xunit;

namespace Smoke.Tests;

// AnySku, AnyQuantity and AnyOrderLine are not committed: run.sh scaffolds them with the packed `any` into a
// disposable copy of this folder, so these tests exercise the tool's output as a user would receive it.
public sealed class ScaffoldedGeneratorTests {

    [Fact, Reproducible]
    public void A_scaffolded_aggregate_draws_through_its_scaffolded_parts() {
        // Arrange
        OrderLine anyLine = new AnyOrderLine().Generate();

        // Act
        int quantity = anyLine.Quantity.Value;

        // Assert
        Assert.InRange(quantity, 1, 999);
    }

    [Fact, Reproducible]
    public void A_scaffolded_pin_fixes_the_value_the_test_cares_about() {
        // Arrange
        Quantity three = new Quantity(3);

        // Act
        OrderLine anyLine = new AnyOrderLine().WithQuantity(three).Generate();

        // Assert
        Assert.Same(three, anyLine.Quantity);
    }

}
