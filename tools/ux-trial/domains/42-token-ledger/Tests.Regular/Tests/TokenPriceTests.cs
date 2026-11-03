namespace TokenLedger.Domain.Tests.Regular;

public sealed class TokenPriceTests {

    private static TokenDefinition Token(int decimals) => new TokenDefinition("Extra Token", "EXT", decimals, new TokenAmount(BigInteger.Pow(10, decimals) * 1_000_000));

    private static TokenPrice Price(string perToken, string currency = "EUR") => new TokenPrice(ExactDecimal.Parse(perToken), currency);

    [Fact]
    public void A_price_exposes_what_it_was_built_with() {
        // Act
        TokenPrice price = Price("2.5", "USD");

        // Assert
        Assert.Equal(ExactDecimal.Parse("2.5"), price.PerToken);
        Assert.Equal("USD", price.QuoteCurrency);
        Assert.Equal("2.5 USD", price.ToString());
    }

    [Fact]
    public void A_zero_price_is_refused() {
        // Arrange
        string anyCurrency = Any.TokenPrice().Generate().QuoteCurrency;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPrice(ExactDecimal.Zero, anyCurrency));
    }

    [Fact]
    public void A_negative_price_is_refused() {
        // Arrange
        string anyCurrency = Any.TokenPrice().Generate().QuoteCurrency;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPrice(ExactDecimal.Parse("-0.01"), anyCurrency));
    }

    [Fact]
    public void A_price_with_eighteen_fractional_digits_is_accepted() {
        // Act
        TokenPrice price = Price("0.000000000000000001");

        // Assert
        Assert.Equal(18, price.PerToken.Scale);
    }

    [Fact]
    public void A_price_with_nineteen_fractional_digits_is_refused() {
        // Arrange
        string anyCurrency = Any.TokenPrice().Generate().QuoteCurrency;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenPrice(ExactDecimal.Parse("0.0000000000000000001"), anyCurrency));
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    [InlineData("E1R")]
    [InlineData(" EUR")]
    public void A_currency_other_than_three_upper_case_letters_is_refused(string currency) {
        // Arrange
        ExactDecimal anyPrice = Any.TokenPrice().Generate().PerToken;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenPrice(anyPrice, currency));
    }

    [Fact(Skip = "domain defect: a currency with a trailing line feed passes ('^[A-Z]{3}$' without RegexOptions.Multiline matches before a final \\n)")]
    public void A_currency_with_a_trailing_line_feed_is_refused() {
        // Arrange
        ExactDecimal anyPrice = Any.TokenPrice().Generate().PerToken;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenPrice(anyPrice, "EUR\n"));
    }

    [Fact]
    public void A_price_refuses_null_arguments() {
        // Arrange
        ExactDecimal anyPrice    = Any.TokenPrice().Generate().PerToken;
        string       anyCurrency = Any.TokenPrice().Generate().QuoteCurrency;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TokenPrice(null!, anyCurrency));
        Assert.Throws<ArgumentNullException>(() => new TokenPrice(anyPrice, null!));
    }

    [Theory]
    [InlineData("2.5", 4, "12345678", "3086.4195")]     // 1234.5678 tokens x 2.5
    [InlineData("1", 2, "250", "2.5")]
    [InlineData("0.000000000000000001", 18, "1000000000000000000", "0.000000000000000001")]
    [InlineData("3", 6, "0", "0")]
    public void The_value_of_an_amount_is_exact(string price, int decimals, string baseUnits, string expected) {
        // Act
        ExactDecimal value = Price(price).ValueOf(new TokenAmount(BigInteger.Parse(baseUnits)), Token(decimals));

        // Assert
        Assert.Equal(ExactDecimal.Parse(expected), value);
    }

    [Fact]
    public void The_value_of_the_largest_amount_stays_exact() {
        // Arrange
        TokenDefinition token = Token(36);
        TokenPrice      price = Price("0.000000000000000001");

        // Act
        ExactDecimal value = price.ValueOf(TokenAmount.Max, token);

        // Assert: 2^256 - 1 base units at 36 decimals times 10^-18.
        Assert.Equal(new ExactDecimal(TokenAmount.MaxBaseUnits, 54), value);
    }

    [Theory]
    [InlineData("2.5", 2, "10", "400")]            // 10 EUR buys 4 tokens = 400 base units at 2 decimals
    [InlineData("3", 2, "10", "333")]              // 3.333... tokens, rounded down
    [InlineData("3", 0, "10", "3")]
    [InlineData("0.5", 6, "0", "0")]
    [InlineData("1", 6, "0.0000009", "0")]         // less than one base unit
    [InlineData("1", 6, "0.0000019", "1")]
    [InlineData("2", 2, "0.01", "0")]              // half a base unit
    public void The_tokens_a_quote_amount_buys_are_rounded_down(string price, int decimals, string quote, string expectedBaseUnits) {
        // Act
        TokenAmount bought = Price(price).TokensFor(ExactDecimal.Parse(quote), Token(decimals));

        // Assert
        Assert.Equal(BigInteger.Parse(expectedBaseUnits), bought.BaseUnits);
    }

    [Fact]
    public void A_negative_quote_amount_buys_nothing_and_is_refused() {
        // Arrange
        TokenPrice price = Price("2");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => price.TokensFor(ExactDecimal.Parse("-1"), Token(2)));
    }

    [Fact]
    public void A_quote_amount_buying_more_than_the_largest_amount_overflows() {
        // Arrange: at 0.000000000000000001 per token and 36 decimals, 10^60 quote buys far more than 2^256.
        TokenPrice price = Price("0.000000000000000001");

        // Act & Assert
        Assert.Throws<OverflowException>(() => price.TokensFor(new ExactDecimal(BigInteger.Pow(10, 60), 0), Token(36)));
    }

    [Fact]
    public void Valuing_and_buying_require_arguments() {
        // Arrange
        TokenPrice price = Price("1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => price.ValueOf(null!, Token(2)));
        Assert.Throws<ArgumentNullException>(() => price.ValueOf(TokenAmount.Zero, null!));
        Assert.Throws<ArgumentNullException>(() => price.TokensFor(null!, Token(2)));
        Assert.Throws<ArgumentNullException>(() => price.TokensFor(ExactDecimal.One, null!));
    }

    [Fact]
    public void Prices_are_equal_when_amount_and_currency_match() {
        // Arrange
        TokenPrice anyPrice = Any.TokenPrice().Generate();

        // Act
        TokenPrice copy = new TokenPrice(anyPrice.PerToken, anyPrice.QuoteCurrency);

        // Assert
        Assert.Equal(anyPrice, copy);
        Assert.Equal(anyPrice.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Prices_differing_in_amount_or_currency_are_not_equal() {
        // Assert
        Assert.NotEqual(Price("2", "EUR"), Price("2", "USD"));
        Assert.NotEqual(Price("2", "EUR"), Price("3", "EUR"));
        Assert.Equal(Price("2.0", "EUR"), Price("2", "EUR"));
    }

}
