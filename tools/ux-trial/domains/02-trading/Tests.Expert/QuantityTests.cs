using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class QuantityTests {

    [Theory]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void A_number_of_shares_from_one_to_a_million_is_a_quantity(int shares) {
        // Act
        Quantity quantity = new Quantity(shares);

        // Assert
        Assert.Equal(shares, quantity.Shares);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_number_of_shares_outside_one_to_a_million_is_refused(int shares) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(shares));

        // Assert
        Assert.Equal("shares", refusal.ParamName);
    }

    [Fact]
    public void The_maximum_is_one_million_shares() {
        // Act & Assert
        Assert.Equal(1_000_000, Quantity.Maximum);
    }

    [Fact]
    public void Two_quantities_of_the_same_shares_are_equal() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        Quantity sameShares = new Quantity(anyQuantity.Shares);

        // Assert
        Assert.Equal(anyQuantity, sameShares);
        Assert.Equal(anyQuantity.GetHashCode(), sameShares.GetHashCode());
    }

    [Fact]
    public void Two_quantities_of_different_shares_are_not_equal() {
        // Arrange
        Quantity anyQuantity      = Any.Quantity().Generate();
        Quantity anyOtherQuantity = Any.Quantity().WithShares(shares => shares.DifferentFrom(anyQuantity.Shares)).Generate();

        // Act & Assert
        Assert.NotEqual(anyQuantity, anyOtherQuantity);
    }

}
