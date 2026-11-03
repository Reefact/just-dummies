namespace Trading.Tests.Regular;

public sealed class TickSizeTests {

    public static TheoryData<decimal> AcceptedIncrements => new() {
        0.0001m,
        0.0005m,
        0.01m,
        0.05m,
        0.25m,
        0.3333m,
        1m
    };

    [Theory]
    [MemberData(nameof(AcceptedIncrements))]
    public void A_tick_size_accepts_an_increment_above_zero_up_to_one_with_at_most_four_decimals(decimal increment) {
        // Act
        TickSize tick = new TickSize(increment);

        // Assert
        Assert.Equal(increment, tick.Increment);
    }

    public static TheoryData<decimal> NonPositiveIncrements => new() {
        0m,
        -0.01m,
        -1m
    };

    [Theory]
    [MemberData(nameof(NonPositiveIncrements))]
    public void A_tick_size_refuses_a_zero_or_negative_increment(decimal increment) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TickSize(increment));

        // Assert
        Assert.Equal("increment", refusal.ParamName);
    }

    public static TheoryData<decimal> IncrementsAboveTheLargest => new() {
        1.0001m,
        2m,
        decimal.MaxValue
    };

    [Theory]
    [MemberData(nameof(IncrementsAboveTheLargest))]
    public void A_tick_size_refuses_an_increment_above_one(decimal increment) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TickSize(increment));

        // Assert
        Assert.Equal("increment", refusal.ParamName);
    }

    public static TheoryData<decimal> IncrementsWithAScaleAboveFour => new() {
        0.00001m,
        0.01000m,
        0.12345m,
        1.00000m
    };

    [Theory]
    [MemberData(nameof(IncrementsWithAScaleAboveFour))]
    public void A_tick_size_refuses_a_representation_with_more_than_four_decimal_places_even_when_the_value_would_fit(decimal increment) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TickSize(increment));

        // Assert
        Assert.Equal("increment", refusal.ParamName);
    }

    [Fact]
    public void The_largest_increment_is_one() {
        Assert.Equal(1m, TickSize.Largest);
    }

    public static TheoryData<decimal, decimal, bool> Grids => new() {
        { 0.01m, 12.34m, true },
        { 0.01m, 12.345m, false },
        { 0.05m, 1.25m, true },
        { 0.05m, 1.23m, false },
        { 0.25m, 100.50m, true },
        { 0.25m, 100.10m, false },
        { 1m, 5m, true },
        { 1m, 5.5m, false },
        { 0.0001m, 123.4567m, true },
        { 0.3333m, 0.6666m, true },
        { 0.3333m, 0.7m, false },
        { 0.05m, 0m, true },
        { 0.05m, -0.10m, true },
        { 0.05m, -0.12m, false }
    };

    [Theory]
    [MemberData(nameof(Grids))]
    public void A_tick_size_admits_exactly_the_multiples_of_its_increment(decimal increment, decimal price, bool admitted) {
        // Arrange
        TickSize tick = new TickSize(increment);

        // Act
        bool admits = tick.Admits(price);

        // Assert
        Assert.Equal(admitted, admits);
    }

    public static TheoryData<decimal, decimal, Side, decimal> Roundings => new() {
        { 0.05m, 10.07m, Side.Buy, 10.05m },
        { 0.05m, 10.07m, Side.Sell, 10.10m },
        { 0.05m, 10.05m, Side.Buy, 10.05m },
        { 0.05m, 10.05m, Side.Sell, 10.05m },
        { 0.25m, 99.99m, Side.Buy, 99.75m },
        { 0.25m, 99.99m, Side.Sell, 100.00m },
        { 1m, 2.5m, Side.Buy, 2m },
        { 1m, 2.5m, Side.Sell, 3m },
        { 0.01m, 0.004m, Side.Buy, 0m },
        { 0.01m, 0.004m, Side.Sell, 0.01m },
        { 0.05m, -10.07m, Side.Buy, -10.10m },
        { 0.05m, -10.07m, Side.Sell, -10.05m }
    };

    [Theory]
    [MemberData(nameof(Roundings))]
    public void Rounding_goes_down_for_a_buy_and_up_for_a_sell(decimal increment, decimal price, Side side, decimal expected) {
        // Arrange
        TickSize tick = new TickSize(increment);

        // Act
        decimal rounded = tick.Round(price, side);

        // Assert
        Assert.Equal(expected, rounded);
    }

    [Fact]
    public void A_rounded_price_is_on_the_grid() {
        // Arrange
        TickSize tick = new TickSize(0.05m);

        // Act
        decimal buy  = tick.Round(37.123m, Side.Buy);
        decimal sell = tick.Round(37.123m, Side.Sell);

        // Assert
        Assert.True(tick.Admits(buy));
        Assert.True(tick.Admits(sell));
    }

    [Fact]
    public void Rounding_refuses_a_side_that_is_not_defined() {
        // Arrange
        TickSize tick = new TickSize(0.05m);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => tick.Round(10.07m, (Side)2));

        // Assert
        Assert.Equal("side", refusal.ParamName);
    }

    [Fact]
    public void Two_tick_sizes_of_the_same_increment_are_equal_whatever_the_trailing_zeros() {
        // Act
        TickSize first  = new TickSize(0.25m);
        TickSize second = new TickSize(0.250m);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_tick_sizes_of_different_increments_are_different() {
        Assert.False(new TickSize(0.25m).Equals(new TickSize(0.05m)));
    }

    [Fact]
    public void A_tick_size_is_not_equal_to_a_bare_decimal_nor_to_null() {
        // Arrange
        TickSize anyTick = Any.TickSize().Generate();

        // Assert
        Assert.False(anyTick.Equals(anyTick.Increment));
        Assert.False(anyTick.Equals(null));
    }

}
