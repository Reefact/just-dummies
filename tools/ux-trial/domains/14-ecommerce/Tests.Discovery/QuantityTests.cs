using Xunit;

namespace Shop.Domain;

public sealed class QuantityTests {

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(98)]
    [InlineData(99)]
    public void A_quantity_from_one_to_ninety_nine_is_accepted(int units) {
        Quantity quantity = new Quantity(units);

        Assert.Equal(units, quantity.Units);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Zero_or_less_is_refused(int units) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void More_than_the_maximum_is_refused(int units) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(units));
    }

    [Fact]
    public void The_maximum_is_ninety_nine() {
        Assert.Equal(99, Quantity.Maximum);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(40, 50, 90)]
    [InlineData(98, 1, 99)]
    [InlineData(50, 49, 99)]
    public void Add_builds_a_new_quantity_holding_the_sum(int left, int right, int expected) {
        Quantity sum = new Quantity(left).Add(new Quantity(right));

        Assert.Equal(new Quantity(expected), sum);
    }

    [Theory]
    [InlineData(99, 1)]
    [InlineData(50, 50)]
    [InlineData(99, 99)]
    public void Add_refuses_a_sum_above_the_maximum(int left, int right) {
        Quantity quantity = new Quantity(left);

        Assert.Throws<ArgumentOutOfRangeException>(() => quantity.Add(new Quantity(right)));
    }

    [Fact]
    public void Add_leaves_both_operands_untouched() {
        Quantity left  = new Quantity(3);
        Quantity right = new Quantity(4);

        left.Add(right);

        Assert.Equal(3, left.Units);
        Assert.Equal(4, right.Units);
    }

    [Fact]
    public void Two_quantities_of_the_same_units_are_equal() {
        Assert.Equal(new Quantity(7), new Quantity(7));
        Assert.Equal(new Quantity(7).GetHashCode(), new Quantity(7).GetHashCode());
        Assert.NotEqual(new Quantity(7), new Quantity(8));
    }

}
