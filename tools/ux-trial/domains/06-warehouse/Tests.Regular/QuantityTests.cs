namespace Warehouse.Domain.Tests.Regular;

public sealed class QuantityTests {

    [Fact]
    public void The_maximum_is_ten_thousand_units() {
        Assert.Equal(10_000, Quantity.Maximum);
    }

    [Fact]
    public void Any_count_within_the_limits_is_kept() {
        // Arrange
        int anyUnits = Any.Int32().Between(0, Quantity.Maximum).Generate();

        // Act
        Quantity quantity = new Quantity(anyUnits);

        // Assert
        Assert.Equal(anyUnits, quantity.Units);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10_000)]
    public void The_limits_of_the_range_are_accepted(int units) {
        // Act
        Quantity quantity = new Quantity(units);

        // Assert
        Assert.Equal(units, quantity.Units);
    }

    [Fact]
    public void A_negative_count_is_refused() {
        // Arrange
        int anyNegative = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(anyNegative));

        // Assert
        Assert.Equal("units", refusal.ParamName);
    }

    [Fact]
    public void Minus_one_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(-1));
    }

    [Fact]
    public void A_count_above_the_maximum_is_refused() {
        // Arrange
        int anyTooMany = Any.Int32().GreaterThan(Quantity.Maximum).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(anyTooMany));

        // Assert
        Assert.Equal("units", refusal.ParamName);
        Assert.Equal(anyTooMany, refusal.ActualValue);
    }

    [Fact]
    public void One_unit_above_the_maximum_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(10_001));
    }

    [Fact]
    public void Two_quantities_of_the_same_count_are_equal() {
        // Arrange
        int anyUnits = Any.Int32().Between(0, Quantity.Maximum).Generate();

        // Act
        Quantity first  = new Quantity(anyUnits);
        Quantity second = new Quantity(anyUnits);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_quantities_of_different_counts_are_not_equal() {
        // Arrange
        int anyUnits = Any.Int32().Between(0, Quantity.Maximum).Generate();
        int another  = Any.Int32().Between(0, Quantity.Maximum).DifferentFrom(anyUnits).Generate();

        // Assert
        Assert.False(new Quantity(anyUnits).Equals(new Quantity(another)));
    }

    [Fact]
    public void A_quantity_is_not_equal_to_null_or_to_a_bare_number() {
        // Arrange
        Quantity anyQuantity = new AnyQuantity().Generate();

        // Assert
        Assert.False(anyQuantity.Equals(null));
        Assert.False(anyQuantity.Equals(anyQuantity.Units));
    }

    [Fact]
    public void It_is_written_as_its_count_with_no_group_separator() {
        Assert.Equal("10000", new Quantity(10_000).ToString());
        Assert.Equal("0", new Quantity(0).ToString());
    }

}
