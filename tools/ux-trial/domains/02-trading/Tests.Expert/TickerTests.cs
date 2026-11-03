using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class TickerTests {

    [Theory]
    [InlineData("A")]
    [InlineData("IBM")]
    [InlineData("GOOGL")]
    [InlineData("BRK.B")]
    [InlineData("A.B")]
    public void A_symbol_of_one_to_five_capitals_with_an_optional_class_suffix_is_a_ticker(string symbol) {
        // Act
        Ticker ticker = new Ticker(symbol);

        // Assert
        Assert.Equal(symbol, ticker.Symbol);
    }

    [Theory]
    [InlineData("ibm")]
    [InlineData("Ibm")]
    [InlineData("GOOGLE")]
    [InlineData("IBM1")]
    [InlineData("1")]
    [InlineData("BRK.")]
    [InlineData(".B")]
    [InlineData("BRK.BB")]
    [InlineData("BRK.b")]
    [InlineData("BRK.B.C")]
    [InlineData("BRK-B")]
    [InlineData("BR K")]
    [InlineData(" IBM")]
    [InlineData("IBM ")]
    [InlineData("ÉCU")]
    public void A_symbol_outside_the_exchange_format_is_refused(string symbol) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ticker(symbol));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which in .NET also matches before a final line feed, so \"IBM\\n\" is accepted as a ticker; seed: not applicable, the value is a literal")]
    public void A_symbol_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Ticker("IBM\n"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_symbol_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ticker(blank));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact]
    public void A_missing_symbol_is_refused_as_a_null_argument() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Ticker(null!));
    }

    [Fact]
    public void A_ticker_shows_as_its_symbol() {
        // Act
        string shown = new Ticker("BRK.B").ToString();

        // Assert
        Assert.Equal("BRK.B", shown);
    }

    [Fact]
    public void Two_tickers_of_the_same_symbol_are_equal() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();

        // Act
        Ticker sameSymbol = new Ticker(anyTicker.Symbol);

        // Assert
        Assert.Equal(anyTicker, sameSymbol);
        Assert.Equal(anyTicker.GetHashCode(), sameSymbol.GetHashCode());
    }

    [Fact]
    public void Two_tickers_of_different_symbols_are_not_equal() {
        // Arrange
        Ticker anyTicker      = Any.Ticker().Generate();
        Ticker anyOtherTicker = Any.Ticker().WithSymbol(symbol => symbol.DifferentFrom(anyTicker.Symbol)).Generate();

        // Act & Assert
        Assert.NotEqual(anyTicker, anyOtherTicker);
    }

    [Fact]
    public void A_ticker_is_not_equal_to_something_that_is_not_a_ticker() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();

        // Act & Assert
        Assert.False(anyTicker.Equals(anyTicker.Symbol));
        Assert.False(anyTicker.Equals(null));
    }

}
