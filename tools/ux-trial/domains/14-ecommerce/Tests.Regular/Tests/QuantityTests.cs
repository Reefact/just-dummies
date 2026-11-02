using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class QuantityTests {

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99)]
    public void A_number_of_units_from_1_to_99_is_accepted(int units) {
        // Act
        Quantity quantity = new Quantity(units);

        // Assert
        Assert.Equal(units, quantity.Units);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_number_of_units_outside_1_to_99_is_refused(int units) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));

        // Assert
        Assert.Equal("units", exception.ParamName);
    }

    [Fact]
    public void The_maximum_is_99() {
        // Act & Assert
        Assert.Equal(99, Quantity.Maximum);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(3, 4, 7)]
    [InlineData(98, 1, 99)]
    [InlineData(49, 50, 99)]
    public void Adding_quantities_gives_a_quantity_of_the_summed_units(int left, int right, int expected) {
        // Act
        Quantity sum = new Quantity(left).Add(new Quantity(right));

        // Assert
        Assert.Equal(new Quantity(expected), sum);
    }

    [Theory]
    [InlineData(99, 1)]
    [InlineData(50, 50)]
    [InlineData(99, 99)]
    public void Adding_quantities_whose_sum_exceeds_99_is_refused(int left, int right) {
        // Arrange
        Quantity quantity = new Quantity(left);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => quantity.Add(new Quantity(right)));
    }

    [Fact]
    public void Two_quantities_are_equal_when_their_units_are() {
        // Arrange
        Quantity quantity = new Quantity(7);

        // Act & Assert
        Assert.True(quantity.Equals(new Quantity(7)));
        Assert.Equal(quantity.GetHashCode(), new Quantity(7).GetHashCode());
        Assert.NotEqual(quantity, new Quantity(8));
    }

    [Fact]
    public void A_quantity_is_not_equal_to_null_or_to_a_bare_number() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act & Assert
        Assert.False(anyQuantity.Equals(null));
        Assert.False(anyQuantity.Equals(anyQuantity.Units));
    }

}
