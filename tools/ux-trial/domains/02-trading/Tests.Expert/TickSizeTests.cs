using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class TickSizeTests {

    [Theory]
    [InlineData("0.0001")]
    [InlineData("0.01")]
    [InlineData("0.05")]
    [InlineData("0.25")]
    [InlineData("1")]
    [InlineData("1.0000")]
    public void An_increment_up_to_one_with_at_most_four_decimal_places_is_a_tick_size(string increment) {
        // Act
        TickSize tick = new TickSize(Literal.Decimal(increment));

        // Assert
        Assert.Equal(Literal.Decimal(increment), tick.Increment);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("1.0001")]
    [InlineData("2")]
    [InlineData("1.00001")]
    public void An_increment_outside_zero_exclusive_to_one_inclusive_is_refused_as_out_of_range(string increment) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TickSize(Literal.Decimal(increment)));

        // Assert
        Assert.Equal("increment", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.00001")]
    [InlineData("0.01000")]
    [InlineData("1.00000")]
    public void An_increment_written_with_more_than_four_decimal_places_is_refused_even_when_its_value_would_fit(string increment) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TickSize(Literal.Decimal(increment)));

        // Assert
        Assert.Equal("increment", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.05", "0")]
    [InlineData("0.05", "10.00")]
    [InlineData("0.05", "10.05")]
    [InlineData("0.05", "-10.05")]
    [InlineData("0.25", "99.75")]
    [InlineData("0.0001", "123456.7891")]
    [InlineData("1", "7")]
    public void A_price_that_is_a_whole_number_of_ticks_is_admitted(string increment, string price) {
        // Arrange
        TickSize tick = new TickSize(Literal.Decimal(increment));

        // Act & Assert
        Assert.True(tick.Admits(Literal.Decimal(price)));
    }

    [Theory]
    [InlineData("0.05", "10.03")]
    [InlineData("0.05", "10.0001")]
    [InlineData("0.25", "99.80")]
    [InlineData("0.25", "-0.10")]
    [InlineData("1", "7.5")]
    [InlineData("0.0003", "0.0004")]
    public void A_price_between_two_ticks_is_not_admitted(string increment, string price) {
        // Arrange
        TickSize tick = new TickSize(Literal.Decimal(increment));

        // Act & Assert
        Assert.False(tick.Admits(Literal.Decimal(price)));
    }

    [Theory]
    [InlineData("0.05", "10.037", "10.00", "10.05")]
    [InlineData("0.05", "10.0001", "10.00", "10.05")]
    [InlineData("0.05", "10.0499", "10.00", "10.05")]
    [InlineData("0.25", "0.30", "0.25", "0.50")]
    [InlineData("1", "2.9999", "2", "3")]
    [InlineData("0.05", "-10.03", "-10.05", "-10.00")]
    public void A_price_between_two_ticks_rounds_down_for_a_buy_and_up_for_a_sell(string increment, string price, string buyRounded, string sellRounded) {
        // Arrange
        TickSize tick = new TickSize(Literal.Decimal(increment));

        // Act
        decimal forBuy  = tick.Round(Literal.Decimal(price), Side.Buy);
        decimal forSell = tick.Round(Literal.Decimal(price), Side.Sell);

        // Assert
        Assert.Equal(Literal.Decimal(buyRounded), forBuy);
        Assert.Equal(Literal.Decimal(sellRounded), forSell);
    }

    [Theory]
    [InlineData(Side.Buy)]
    [InlineData(Side.Sell)]
    public void A_price_already_on_the_grid_does_not_move_whatever_the_side(Side side) {
        // Arrange
        TickSize tick = new TickSize(0.05m);

        // Act
        decimal rounded = tick.Round(10.05m, side);

        // Assert
        Assert.Equal(10.05m, rounded);
    }

    [Fact]
    public void A_rounded_price_is_admitted_by_the_tick_that_rounded_it() {
        // Arrange
        TickSize anyTick = Any.TickSize().Generate();
        Side     anySide = Any.Enum<Side>().Generate();

        // Act
        decimal rounded = anyTick.Round(1234.5678m, anySide);

        // Assert
        Assert.True(anyTick.Admits(rounded));
    }

    [Fact]
    public void An_undefined_side_cannot_round_a_price() {
        // Arrange
        TickSize anyTick = Any.TickSize().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyTick.Round(10m, (Side)42));

        // Assert
        Assert.Equal("side", refusal.ParamName);
    }

    [Fact]
    public void Two_ticks_of_the_same_increment_are_equal_whatever_the_decimal_places_written() {
        // Arrange
        TickSize once  = new TickSize(0.05m);
        TickSize twice = new TickSize(0.050m);

        // Act & Assert
        Assert.Equal(once, twice);
        Assert.Equal(once.GetHashCode(), twice.GetHashCode());
    }

    [Fact]
    public void Two_ticks_of_different_increments_are_not_equal() {
        // Arrange
        TickSize anyTick      = Any.TickSize().Generate();
        TickSize anyOtherTick = Any.TickSize().WithIncrement(increment => increment.DifferentFrom(anyTick.Increment)).Generate();

        // Act & Assert
        Assert.NotEqual(anyTick, anyOtherTick);
    }

}
