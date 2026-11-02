using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class QuantityTests {

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(98)]
    [InlineData(Quantity.Maximum)]
    public void A_number_of_units_from_one_to_the_maximum_is_kept(int units) {
        // Act
        Quantity quantity = new Quantity(units);

        // Assert
        Assert.Equal(units, quantity.Units);
    }

    [Fact]
    public void The_maximum_is_ninety_nine() {
        // Assert
        Assert.Equal(99, Quantity.Maximum);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Fewer_than_one_unit_is_refused(int units) {
        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));
        Assert.Equal("units", error.ParamName);
    }

    [Theory]
    [InlineData(Quantity.Maximum + 1)]
    [InlineData(int.MaxValue)]
    public void More_than_the_maximum_is_refused(int units) {
        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));
        Assert.Equal("units", error.ParamName);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(40, 59, 99)]
    [InlineData(98, 1, 99)]
    public void Adding_two_quantities_sums_their_units(int left, int right, int expected) {
        // Arrange
        Quantity leftQuantity  = new Quantity(left);
        Quantity rightQuantity = new Quantity(right);

        // Act
        Quantity sum = leftQuantity.Add(rightQuantity);

        // Assert
        Assert.Equal(expected, sum.Units);
    }

    [Fact]
    public void Adding_builds_a_new_quantity_and_leaves_both_operands_alone() {
        // Arrange
        Quantity three = new Quantity(3);
        Quantity four  = new Quantity(4);

        // Act
        Quantity sum = three.Add(four);

        // Assert
        Assert.NotSame(three, sum);
        Assert.NotSame(four, sum);
        Assert.Equal(3, three.Units);
        Assert.Equal(4, four.Units);
    }

    [Theory]
    [InlineData(99, 1)]
    [InlineData(50, 50)]
    [InlineData(99, 99)]
    public void A_sum_above_the_maximum_is_refused(int left, int right) {
        // Arrange
        Quantity leftQuantity  = new Quantity(left);
        Quantity rightQuantity = new Quantity(right);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => leftQuantity.Add(rightQuantity));
    }

    [Fact]
    public void Two_quantities_of_the_same_units_are_equal() {
        // Arrange
        Quantity anyQuantity  = Any.Quantity().Generate();
        Quantity sameQuantity = new Quantity(anyQuantity.Units);

        // Act
        bool areEqual = anyQuantity.Equals(sameQuantity);

        // Assert
        Assert.True(areEqual);
        Assert.Equal(anyQuantity.GetHashCode(), sameQuantity.GetHashCode());
    }

    [Fact]
    public void Two_quantities_of_different_units_are_not_equal() {
        // Arrange
        Quantity anyQuantity      = Any.Quantity().Generate();
        Quantity anyOtherQuantity = Any.Quantity().WithUnits(u => u.DifferentFrom(anyQuantity.Units)).Generate();

        // Act
        bool areEqual = anyQuantity.Equals(anyOtherQuantity);

        // Assert
        Assert.False(areEqual);
    }

}
