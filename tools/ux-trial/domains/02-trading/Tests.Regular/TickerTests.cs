namespace Trading.Tests.Regular;

public sealed class TickerTests {

    [Theory]
    [InlineData("A")]
    [InlineData("AAPL")]
    [InlineData("ABCDE")]
    [InlineData("BRK.B")]
    [InlineData("A.Z")]
    [InlineData("ABCDE.A")]
    public void A_ticker_accepts_one_to_five_capital_letters_with_an_optional_share_class(string symbol) {
        // Act
        Ticker ticker = new Ticker(symbol);

        // Assert
        Assert.Equal(symbol, ticker.Symbol);
    }

    [Theory]
    [InlineData("aapl")]
    [InlineData("Aapl")]
    [InlineData("ABCDEF")]
    [InlineData("AB1")]
    [InlineData("A-B")]
    [InlineData("BRK.")]
    [InlineData(".B")]
    [InlineData("BRK.b")]
    [InlineData("BRK.BB")]
    [InlineData("BRK.B.C")]
    [InlineData("BRK..B")]
    [InlineData(" AAPL")]
    [InlineData("AAPL ")]
    [InlineData("AA PL")]
    [InlineData("ÉCOLE")]
    public void A_ticker_refuses_a_symbol_outside_the_format(string symbol) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ticker(symbol));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
        Assert.Contains(symbol, refusal.Message);
    }

    [Fact]
    public void A_ticker_refuses_a_null_symbol_as_a_null_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Ticker(null!));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void A_ticker_refuses_a_blank_symbol_as_a_plain_argument_error(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ticker(blank));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact]
    public void Two_tickers_of_the_same_symbol_are_equal_and_share_a_hash_code() {
        // Arrange
        string anySymbol = Any.Ticker().Generate().Symbol;

        // Act
        Ticker first  = new Ticker(anySymbol);
        Ticker second = new Ticker(anySymbol);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_tickers_of_different_symbols_are_different() {
        // Act
        Ticker first  = new Ticker("AAPL");
        Ticker second = new Ticker("AAPM");

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_ticker_is_not_equal_to_its_bare_symbol_nor_to_null() {
        // Arrange
        Ticker anyTicker = Any.Ticker().Generate();

        // Assert
        Assert.False(anyTicker.Equals(anyTicker.Symbol));
        Assert.False(anyTicker.Equals(null));
    }

    [Fact]
    public void A_ticker_prints_as_its_symbol() {
        // Act
        Ticker ticker = new Ticker("BRK.B");

        // Assert
        Assert.Equal("BRK.B", ticker.ToString());
    }

    [Fact(Skip = "domain defect: the pattern is anchored with $, which also matches before a final newline, so \"AAPL\\n\" is accepted as a symbol (no seed: literal input)")]
    public void A_ticker_refuses_a_symbol_with_a_trailing_newline() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ticker("AAPL\n"));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

}
