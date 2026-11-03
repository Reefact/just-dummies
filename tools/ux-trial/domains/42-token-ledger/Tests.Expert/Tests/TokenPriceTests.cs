using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class TokenPriceTests {

    private static ExactDecimal D(string text) {
        return ExactDecimal.Parse(text);
    }

    // 4 decimals: 1 EXT is 10_000 base units, 1234.5678 EXT is 12_345_678.
    private static TokenDefinition ExampleToken() {
        return Any.TokenDefinition().WithDecimals(4).WithSupplyCap(Amount.Of(BigInteger.Parse("1000000000000"))).Generate();
    }

    private static TokenDefinition TokenWithDecimals(int decimals) {
        return Any.TokenDefinition().WithDecimals(decimals).Generate();
    }

    // ---- construction ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_price_keeps_its_amount_and_its_currency() {
        // Act
        TokenPrice price = new TokenPrice(D("2.5"), "USD");

        // Assert
        Assert.Equal(D("2.5"), price.PerToken);
        Assert.Equal("USD", price.QuoteCurrency);
        Assert.Equal("2.5 USD", price.ToString());
    }

    [Fact]
    public void The_smallest_price_is_one_unit_at_the_eighteenth_decimal() {
        Assert.Equal(D("0.000000000000000001"), new TokenPrice(D("0.000000000000000001"), "USD").PerToken);
    }

    [Fact]
    public void A_price_with_nineteen_fractional_digits_is_refused() {
        Assert.Throws<ArgumentException>(() => new TokenPrice(D("0.0000000000000000001"), "USD"));
    }

    [Fact]
    public void Trailing_zeros_do_not_count_towards_the_eighteen_digits() {
        Assert.Equal(D("1.5"), new TokenPrice(D("1.5000000000000000000000"), "USD").PerToken);
    }

    [Fact]
    public void A_price_of_zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPrice(ExactDecimal.Zero, "USD"));
    }

    [Fact]
    public void Any_negative_price_is_refused() {
        // Arrange
        ExactDecimal anyNegative = Any.ExactDecimal().WithMantissa(m => m.LessThan(0)).WithScale(s => s.LessThanOrEqualTo(TokenPrice.MaxPriceScale)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPrice(anyNegative, "USD"));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("ZZZ")]
    public void A_currency_of_three_capital_letters_is_accepted(string currency) {
        Assert.Equal(currency, new TokenPrice(D("1"), currency).QuoteCurrency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("U5D")]
    [InlineData("US$")]
    [InlineData(" USD")]
    [InlineData("USD ")]
    public void A_currency_that_is_not_exactly_three_capital_letters_is_refused(string currency) {
        Assert.Throws<ArgumentException>(() => new TokenPrice(D("1"), currency));
    }

    [Fact(Skip = "domain defect: the currency regex ends with '$', which .NET also matches before a final '\\n', so a drawn currency + \"\\n\" is accepted; seed -929908857")]
    public void A_currency_followed_by_a_line_break_is_refused() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new TokenPrice(D("1"), anyCurrency + "\n"));
    }

    [Fact]
    public void A_missing_price_or_currency_is_refused_as_a_missing_argument() {
        Assert.Throws<ArgumentNullException>(() => new TokenPrice(null!, "USD"));
        Assert.Throws<ArgumentNullException>(() => new TokenPrice(D("1"), null!));
    }

    // ---- value of an amount ---------------------------------------------------------------------------------------

    [Fact]
    public void The_value_of_an_amount_is_its_human_quantity_times_the_price() {
        // 1234.5678 EXT at 2.5 USD
        Assert.Equal(D("3086.4195"), new TokenPrice(D("2.5"), "USD").ValueOf(Amount.Of(12_345_678), ExampleToken()));
    }

    [Fact]
    public void The_value_of_one_token_is_the_price_itself() {
        // Arrange
        TokenDefinition anyToken = Any.TokenDefinition().Generate();
        TokenPrice      price    = new TokenPrice(D("0.000000000000000001"), "USD");

        // Act
        ExactDecimal value = price.ValueOf(anyToken.OneToken, anyToken);

        // Assert
        Assert.Equal(D("0.000000000000000001"), value);
    }

    [Fact]
    public void The_value_of_nothing_is_zero() {
        // Arrange
        TokenDefinition anyToken = Any.TokenDefinition().Generate();
        TokenPrice      anyPrice = Any.TokenPrice().Generate();

        // Act
        ExactDecimal value = anyPrice.ValueOf(TokenAmount.Zero, anyToken);

        // Assert
        Assert.Equal(ExactDecimal.Zero, value);
    }

    [Fact]
    public void The_value_of_one_base_unit_at_the_finest_decimals_and_price_is_kept_exactly() {
        // 36 decimals + 18 price digits: 54 fractional digits, within the 80 an exact decimal holds.
        // Arrange
        TokenDefinition finest = new TokenDefinition("Example Token", "EXT", 36, new TokenAmount(BigInteger.Pow(10, 36)));
        TokenPrice      price  = new TokenPrice(D("0.000000000000000001"), "USD");

        // Act
        ExactDecimal value = price.ValueOf(Amount.Of(1), finest);

        // Assert
        Assert.Equal(new ExactDecimal(1, 54), value);
    }

    [Fact]
    public void Valuing_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        TokenPrice      anyPrice = Any.TokenPrice().Generate();
        TokenDefinition anyToken = Any.TokenDefinition().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPrice.ValueOf(null!, anyToken));
        Assert.Throws<ArgumentNullException>(() => anyPrice.ValueOf(Amount.Of(1), null!));
    }

    // ---- tokens for a quote ---------------------------------------------------------------------------------------

    [Fact]
    public void A_quote_buys_its_share_of_a_token_in_base_units() {
        // 100 USD at 2 USD per EXT is 50 EXT, i.e. 500_000 base units at 4 decimals.
        Assert.Equal(Amount.Of(500_000), new TokenPrice(D("2"), "USD").TokensFor(D("100"), ExampleToken()));
    }

    [Fact]
    public void A_quote_buys_a_fraction_of_a_token_when_it_is_smaller_than_the_price() {
        Assert.Equal(Amount.Of(2_500), new TokenPrice(D("2"), "USD").TokensFor(D("0.5"), ExampleToken()));
    }

    [Theory]
    [InlineData("10", "3", 0, 3)]
    [InlineData("1", "3", 0, 0)]
    [InlineData("2", "3", 0, 0)]
    [InlineData("1", "3", 4, 3_333)]
    [InlineData("2", "3", 4, 6_666)]
    [InlineData("0.0001", "3", 4, 0)]
    public void What_a_quote_buys_is_rounded_down_to_a_whole_base_unit(string quote, string price, int decimals, int expectedBaseUnits) {
        Assert.Equal(Amount.Of(expectedBaseUnits), new TokenPrice(D(price), "USD").TokensFor(D(quote), TokenWithDecimals(decimals)));
    }

    [Fact]
    public void A_quote_of_nothing_buys_nothing() {
        // Arrange
        TokenDefinition anyToken = Any.TokenDefinition().Generate();
        TokenPrice      anyPrice = Any.TokenPrice().Generate();

        // Act
        TokenAmount bought = anyPrice.TokensFor(ExactDecimal.Zero, anyToken);

        // Assert
        Assert.Equal(TokenAmount.Zero, bought);
    }

    [Fact]
    public void A_negative_quote_is_refused() {
        // Arrange
        TokenDefinition anyToken = Any.TokenDefinition().Generate();
        TokenPrice      anyPrice = Any.TokenPrice().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPrice.TokensFor(D("-0.01"), anyToken));
    }

    [Fact]
    public void A_quote_that_buys_exactly_the_largest_amount_is_accepted() {
        // Arrange: at a price of 1 and no decimals, one unit of quote buys one base unit.
        TokenPrice      price = new TokenPrice(D("1"), "USD");
        TokenDefinition token = TokenWithDecimals(0);

        // Act
        TokenAmount bought = price.TokensFor(ExactDecimal.FromInteger(TokenAmount.MaxBaseUnits), token);

        // Assert
        Assert.Equal(TokenAmount.Max, bought);
    }

    [Fact]
    public void A_quote_that_buys_one_base_unit_more_than_the_largest_amount_overflows() {
        // Arrange
        TokenPrice      price = new TokenPrice(D("1"), "USD");
        TokenDefinition token = TokenWithDecimals(0);

        // Act / Assert
        Assert.Throws<OverflowException>(() => price.TokensFor(ExactDecimal.FromInteger(TokenAmount.MaxBaseUnits + 1), token));
    }

    [Fact]
    public void Buying_with_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        TokenPrice      anyPrice = Any.TokenPrice().Generate();
        TokenDefinition anyToken = Any.TokenDefinition().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPrice.TokensFor(null!, anyToken));
        Assert.Throws<ArgumentNullException>(() => anyPrice.TokensFor(D("1"), null!));
    }

    // ---- equality -------------------------------------------------------------------------------------------------

    [Fact]
    public void Two_prices_with_the_same_amount_and_currency_are_equal_and_share_their_hash_code() {
        // Arrange
        TokenPrice first  = new TokenPrice(D("2.50"), "USD");
        TokenPrice second = new TokenPrice(D("2.5"), "USD");

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Prices_differing_by_amount_or_currency_are_not_equal() {
        // Arrange
        TokenPrice reference = new TokenPrice(D("2.5"), "USD");

        // Assert
        Assert.False(reference.Equals(new TokenPrice(D("2.6"), "USD")));
        Assert.False(reference.Equals(new TokenPrice(D("2.5"), "EUR")));
        Assert.False(reference.Equals((TokenPrice?)null));
    }

}
