using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class QuantityTests {

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_999)]
    [InlineData(10_000)]
    public void A_quantity_from_zero_to_the_maximum_is_accepted(int units) {
        Quantity quantity = new Quantity(units);

        Assert.Equal(units, quantity.Units);
        Assert.Equal(units.ToString(), quantity.ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_quantity_outside_zero_to_the_maximum_is_refused(int units) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));

        Assert.Equal("units", exception.ParamName);
    }

    [Fact]
    public void The_maximum_is_ten_thousand_units() {
        Assert.Equal(10_000, Quantity.Maximum);
    }

    [Fact]
    public void Quantities_are_equal_by_value() {
        Assert.True(new Quantity(42).Equals(new Quantity(42)));
        Assert.Equal(new Quantity(42).GetHashCode(), new Quantity(42).GetHashCode());
        Assert.False(new Quantity(42).Equals(new Quantity(43)));
        Assert.False(new Quantity(42).Equals(42));
        Assert.False(new Quantity(42).Equals(null));
    }

}
