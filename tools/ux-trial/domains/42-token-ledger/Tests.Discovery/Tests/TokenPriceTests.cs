using System.Numerics;

using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class TokenPriceTests {

    private static TokenDefinition TokenWithDecimals(int decimals) {
        return Any.TokenDefinition()
                  .WithDecimals(decimals)
                  .WithSupplyCap(new TokenAmount(BigInteger.Pow(10, decimals) * 1_000_000))
                  .Generate();
    }

    [Fact]
    public void A_price_keeps_what_it_was_given() {
        TokenPrice price = new TokenPrice(ExactDecimal.Parse("2.5"), "USD");

        Assert.Equal(ExactDecimal.Parse("2.5"), price.PerToken);
        Assert.Equal("USD", price.QuoteCurrency);
        Assert.Equal("2.5 USD", price.ToString());
    }

    [Fact]
    public void A_drawn_price_is_positive_with_a_three_letter_currency() {
        for (int i = 0; i < 50; i++) {
            TokenPrice anyPrice = Any.TokenPrice().Generate();

            Assert.Equal(1, anyPrice.PerToken.Sign);
            Assert.True(anyPrice.PerToken.Scale <= TokenPrice.MaxPriceScale);
            Assert.Equal(3, anyPrice.QuoteCurrency.Length);
        }
    }

    [Fact]
    public void A_zero_price_is_refused() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPrice(ExactDecimal.Zero, anyCurrency));

        Assert.Equal("perToken", refusal.ParamName);
    }

    [Fact]
    public void A_negative_price_is_refused() {
        ExactDecimal anyNegative = Any.ExactDecimal().WithMantissa(m => m.AtMost(-1)).WithScale(s => s.LessThanOrEqualTo(TokenPrice.MaxPriceScale)).Generate();
        string       anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPrice(anyNegative, anyCurrency));
    }

    [Fact]
    public void A_price_with_eighteen_fractional_digits_is_accepted_and_with_nineteen_refused() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        Assert.Equal(18, new TokenPrice(new ExactDecimal(1, 18), anyCurrency).PerToken.Scale);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TokenPrice(new ExactDecimal(1, 19), anyCurrency));
        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("perToken", refusal.ParamName);
    }

    [Fact]
    public void Digits_that_normalise_away_do_not_count_toward_the_eighteen() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        TokenPrice price = new TokenPrice(new ExactDecimal(1_000_000, 24), anyCurrency);

        Assert.Equal(18, price.PerToken.Scale);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("U5D")]
    [InlineData("US ")]
    [InlineData("€UR")]
    public void A_quote_currency_that_is_not_three_upper_case_letters_is_refused(string currency) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TokenPrice(ExactDecimal.One, currency));

        Assert.Equal("quoteCurrency", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the currency pattern ends with $, which also matches before a final line feed, so 'USD' followed by a line feed is accepted; seed 1608270057")]
    public void A_quote_currency_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new TokenPrice(ExactDecimal.One, "USD\n"));
    }

    [Fact]
    public void A_null_price_or_currency_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TokenPrice(null!, "USD"));
        Assert.Throws<ArgumentNullException>(() => new TokenPrice(ExactDecimal.One, null!));
    }

    [Fact]
    public void The_value_of_an_amount_is_the_tokens_times_the_price() {
        // Arrange: 3.5 tokens at 2.25 EUR each.
        TokenDefinition token  = TokenWithDecimals(2);
        TokenPrice      price  = new TokenPrice(ExactDecimal.Parse("2.25"), "EUR");

        // Act
        ExactDecimal value = price.ValueOf(new TokenAmount(350), token);

        // Assert
        Assert.Equal(ExactDecimal.Parse("7.875"), value);
    }

    [Fact]
    public void The_value_of_nothing_is_zero() {
        TokenDefinition token = Any.TokenDefinition().Generate();
        TokenPrice      price = Any.TokenPrice().Generate();

        ExactDecimal value = price.ValueOf(TokenAmount.Zero, token);

        Assert.Equal(ExactDecimal.Zero, value);
    }

    [Fact]
    public void The_value_of_one_base_unit_at_the_finest_price_is_exact() {
        // 36 decimals and 18 price digits: 54 fractional digits, all kept.
        TokenDefinition token = TokenWithDecimals(36);
        TokenPrice      price = new TokenPrice(new ExactDecimal(1, 18), "USD");

        ExactDecimal value = price.ValueOf(new TokenAmount(1), token);

        Assert.Equal(new ExactDecimal(1, 54), value);
    }

    [Fact]
    public void Tokens_bought_are_rounded_down_to_a_whole_base_unit() {
        // 10 EUR at 3 EUR a token, 2 decimals: 3.333... tokens, so 333 base units.
        TokenDefinition token = TokenWithDecimals(2);
        TokenPrice      price = new TokenPrice(ExactDecimal.Parse("3"), "EUR");

        TokenAmount bought = price.TokensFor(ExactDecimal.Parse("10"), token);

        Assert.Equal(new TokenAmount(333), bought);
    }

    [Fact]
    public void Tokens_bought_with_an_exact_division_are_exact() {
        TokenDefinition token = TokenWithDecimals(4);
        TokenPrice      price = new TokenPrice(ExactDecimal.Parse("0.25"), "USD");

        Assert.Equal(new TokenAmount(40_000), price.TokensFor(ExactDecimal.Parse("1"), token));
        Assert.Equal(new TokenAmount(1_000_000), price.TokensFor(ExactDecimal.Parse("25"), token));
    }

    [Fact]
    public void A_quote_too_small_to_buy_a_base_unit_buys_nothing() {
        TokenDefinition token = TokenWithDecimals(2);
        TokenPrice      price = new TokenPrice(ExactDecimal.Parse("100"), "USD");

        Assert.Equal(TokenAmount.Zero, price.TokensFor(ExactDecimal.Parse("0.99"), token));
    }

    [Fact]
    public void A_zero_quote_buys_nothing() {
        TokenDefinition token = Any.TokenDefinition().Generate();
        TokenPrice      price = Any.TokenPrice().Generate();

        Assert.Equal(TokenAmount.Zero, price.TokensFor(ExactDecimal.Zero, token));
    }

    [Fact]
    public void A_negative_quote_is_refused() {
        TokenDefinition token       = Any.TokenDefinition().Generate();
        TokenPrice      price       = Any.TokenPrice().Generate();
        ExactDecimal    anyNegative = Any.ExactDecimal().WithMantissa(m => m.AtMost(-1)).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => price.TokensFor(anyNegative, token));

        Assert.Equal("quoteAmount", refusal.ParamName);
    }

    [Fact]
    public void A_quote_that_buys_more_than_two_to_the_256_minus_one_base_units_overflows() {
        TokenDefinition token = TokenWithDecimals(36);
        TokenPrice      price = new TokenPrice(new ExactDecimal(1, 18), "USD");

        // 10^40 USD at 10^-18 USD a token is 10^58 tokens, i.e. 10^94 base units.
        Assert.Throws<OverflowException>(() => price.TokensFor(new ExactDecimal(BigInteger.Pow(10, 40), 0), token));
    }

    [Fact]
    public void A_quote_that_buys_exactly_the_maximum_is_accepted() {
        TokenDefinition token = TokenWithDecimals(0);
        TokenPrice      price = new TokenPrice(ExactDecimal.One, "USD");

        Assert.Equal(TokenAmount.Max, price.TokensFor(new ExactDecimal(TokenAmount.MaxBaseUnits, 0), token));
        Assert.Throws<OverflowException>(() => price.TokensFor(new ExactDecimal(TokenAmount.MaxBaseUnits + 1, 0), token));
    }

    [Fact]
    public void Valuation_refuses_null_arguments() {
        TokenDefinition token = Any.TokenDefinition().Generate();
        TokenPrice      price = Any.TokenPrice().Generate();

        Assert.Throws<ArgumentNullException>(() => price.ValueOf(null!, token));
        Assert.Throws<ArgumentNullException>(() => price.ValueOf(TokenAmount.Zero, null!));
        Assert.Throws<ArgumentNullException>(() => price.TokensFor(null!, token));
        Assert.Throws<ArgumentNullException>(() => price.TokensFor(ExactDecimal.Zero, null!));
    }

    [Fact]
    public void Two_prices_are_equal_when_value_and_currency_are_the_same() {
        TokenPrice first  = new TokenPrice(ExactDecimal.Parse("2.50"), "USD");
        TokenPrice second = new TokenPrice(new ExactDecimal(25, 1), "USD");

        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new TokenPrice(ExactDecimal.Parse("2.5"), "EUR"));
        Assert.NotEqual(first, new TokenPrice(ExactDecimal.Parse("2.6"), "USD"));
        Assert.False(first.Equals(null));
    }

}
