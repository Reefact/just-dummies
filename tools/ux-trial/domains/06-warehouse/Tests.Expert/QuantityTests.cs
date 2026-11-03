using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class QuantityTests {

    [Fact]
    public void The_maximum_is_ten_thousand_units() {
        Assert.Equal(10_000, Quantity.Maximum);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_999)]
    [InlineData(10_000)]
    public void A_count_from_zero_to_the_maximum_is_accepted_and_kept(int units) {
        // Act
        Quantity quantity = new Quantity(units);

        // Assert
        Assert.Equal(units, quantity.Units);
        Assert.Equal(units.ToString(), quantity.ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(10_001)]
    [InlineData(int.MaxValue)]
    public void A_count_outside_zero_to_the_maximum_is_refused(int units) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));

        // Assert
        Assert.Equal("units", refused.ParamName);
    }

    [Fact]
    public void Two_quantities_of_the_same_count_are_equal() {
        // Arrange
        int anyUnits = Any.Int32().Between(0, Quantity.Maximum).Generate();

        // Act
        Quantity first  = new Quantity(anyUnits);
        Quantity second = new Quantity(anyUnits);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_quantities_of_different_counts_are_not_equal() {
        // Arrange
        Quantity anyFirst  = Any.Quantity().Generate();
        Quantity anySecond = Any.Quantity().WithUnits(u => u.DifferentFrom(anyFirst.Units)).Generate();

        // Act
        bool equal = anyFirst.Equals(anySecond);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_quantity_is_not_equal_to_a_bare_integer_or_to_null() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        bool equalToInt  = anyQuantity.Equals(anyQuantity.Units);
        bool equalToNull = anyQuantity.Equals(null);

        // Assert
        Assert.False(equalToInt);
        Assert.False(equalToNull);
    }

}
