using System.Globalization;

using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class RateSheetTests {

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    private static ExchangeRate Rate(Currency @base, Currency quote, string value) => new ExchangeRate(new CurrencyPair(@base, quote), D(value));

    // 2026-03-17 is a Tuesday.
    private static readonly DateOnly Tuesday = new DateOnly(2026, 3, 17);

    private static RateSheet UsdSheet(params ExchangeRate[] rates) => new RateSheet(Currency.Usd, Tuesday, rates);

    [Fact]
    public void A_sheet_keeps_its_pivot_date_and_rates() {
        // Arrange
        ExchangeRate eurUsd = Rate(Currency.Eur, Currency.Usd, "1.08");
        ExchangeRate usdJpy = Rate(Currency.Usd, Currency.Jpy, "150");

        // Act
        RateSheet sheet = UsdSheet(eurUsd, usdJpy);

        // Assert
        Assert.Equal(Currency.Usd, sheet.Pivot);
        Assert.Equal(Tuesday, sheet.AsOf);
        Assert.Equivalent(new[] { eurUsd, usdJpy }, sheet.Rates, strict: false);
        Assert.Equal(2, sheet.Rates.Count);
    }

    [Fact]
    public void An_arbitrary_sheet_quotes_every_currency_against_its_pivot_once() {
        // Arrange
        RateSheet anySheet = Any.RateSheet().Generate();

        // Act / Assert
        Assert.NotEmpty(anySheet.Rates);
        Assert.All(anySheet.Rates, rate => Assert.True(rate.Pair.Involves(anySheet.Pivot)));
    }

    [Theory]
    [InlineData("2026-03-14")]
    [InlineData("2026-03-15")]
    public void A_weekend_as_of_date_is_refused(string asOf) {
        // Arrange
        Currency     anyPivot = Any.Currency().Generate();
        ExchangeRate anyRate  = Any.ExchangeRate().WithPair(new CurrencyPair(anyPivot, new AnyCurrency().Except(anyPivot).Generate())).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RateSheet(anyPivot, DateOnly.Parse(asOf), new[] { anyRate }));

        // Assert
        Assert.Equal("asOf", refusal.ParamName);
    }

    [Fact]
    public void A_sheet_without_any_rate_is_refused() {
        // Arrange
        Currency anyPivot = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(anyPivot, Tuesday, Array.Empty<ExchangeRate>()));
    }

    [Fact]
    public void A_rate_that_does_not_involve_the_pivot_is_refused() {
        // Arrange
        Currency anyPivot = Any.Currency().Generate();
        CurrencyPair anyForeignPair = Any.CurrencyPair().WithBase(b => b.Except(anyPivot)).WithQuote(q => q.Except(anyPivot)).Generate();
        ExchangeRate anyForeignRate = Any.ExchangeRate().WithPair(anyForeignPair).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(anyPivot, Tuesday, new[] { anyForeignRate }));
    }

    [Fact]
    public void A_currency_quoted_twice_is_refused_even_in_opposite_directions() {
        // Arrange
        Currency anyPivot = Any.Currency().Generate();
        Currency anyOther = new AnyCurrency().Except(anyPivot).Generate();
        ExchangeRate straight = Any.ExchangeRate().WithPair(new CurrencyPair(anyOther, anyPivot)).Generate();
        ExchangeRate reversed = Any.ExchangeRate().WithPair(new CurrencyPair(anyPivot, anyOther)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(anyPivot, Tuesday, new[] { straight, straight }));
        Assert.Throws<ArgumentException>(() => new RateSheet(anyPivot, Tuesday, new[] { straight, reversed }));
    }

    [Fact]
    public void A_null_rate_is_refused() {
        // Arrange
        Currency anyPivot = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(anyPivot, Tuesday, new ExchangeRate[] { null! }));
    }

    [Fact]
    public void A_sheet_refuses_a_missing_pivot_or_rate_list() {
        // Arrange
        ExchangeRate anyRate = Any.ExchangeRate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RateSheet(null!, Tuesday, new[] { anyRate }));
        Assert.Throws<ArgumentNullException>(() => new RateSheet(anyRate.Pair.Base, Tuesday, null!));
    }

    [Fact]
    public void A_sheet_covers_its_pivot_and_the_currencies_it_quotes_only() {
        // Arrange
        Currency anyUnquoted = new AnyCurrency().Except(Currency.Usd, Currency.Eur, Currency.Jpy).Generate();
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08"), Rate(Currency.Usd, Currency.Jpy, "150"));

        // Act / Assert
        Assert.True(sheet.Covers(Currency.Usd));
        Assert.True(sheet.Covers(Currency.Eur));
        Assert.True(sheet.Covers(Currency.Jpy));
        Assert.False(sheet.Covers(anyUnquoted));
    }

    [Fact]
    public void A_sheet_covers_a_pair_when_it_covers_both_currencies() {
        // Arrange
        Currency anyUnquoted = new AnyCurrency().Except(Currency.Usd, Currency.Eur, Currency.Jpy).Generate();
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08"), Rate(Currency.Usd, Currency.Jpy, "150"));

        // Act / Assert
        Assert.True(sheet.Covers(new CurrencyPair(Currency.Eur, Currency.Jpy)));
        Assert.True(sheet.Covers(new CurrencyPair(Currency.Jpy, Currency.Usd)));
        Assert.False(sheet.Covers(new CurrencyPair(Currency.Eur, anyUnquoted)));
        Assert.False(sheet.Covers(new CurrencyPair(anyUnquoted, Currency.Usd)));
    }

    [Fact]
    public void Covers_refuses_null() {
        // Arrange
        RateSheet anySheet = Any.RateSheet().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySheet.Covers((Currency)null!));
        Assert.Throws<ArgumentNullException>(() => anySheet.Covers((CurrencyPair)null!));
    }

    [Fact]
    public void A_quoted_pair_returns_the_quoted_rate_as_is() {
        // Arrange
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08425"));

        // Act
        ExchangeRate rate = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Usd), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(Currency.Eur, Currency.Usd, "1.08425"), rate);
    }

    [Fact]
    public void The_reverse_of_a_quoted_pair_is_its_rounded_inverse() {
        // Arrange
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08425"), Rate(Currency.Usd, Currency.Jpy, "151.237"));

        // Act
        ExchangeRate usdEur = sheet.RateFor(new CurrencyPair(Currency.Usd, Currency.Eur), MidpointRounding.ToEven);
        ExchangeRate jpyUsd = sheet.RateFor(new CurrencyPair(Currency.Jpy, Currency.Usd), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(Currency.Usd, Currency.Eur, "0.92230"), usdEur);
        Assert.Equal(Rate(Currency.Jpy, Currency.Usd, "0.00661"), jpyUsd);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.01562")]
    [InlineData(MidpointRounding.AwayFromZero, "0.01563")]
    public void An_inverse_on_a_midpoint_rounds_with_the_mode_given(MidpointRounding mode, string expected) {
        // Arrange
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "64"));

        // Act
        ExchangeRate usdEur = sheet.RateFor(new CurrencyPair(Currency.Usd, Currency.Eur), mode);

        // Assert
        Assert.Equal(D(expected), usdEur.Value);
    }

    [Fact]
    public void A_cross_rate_is_triangulated_through_the_pivot() {
        // Arrange
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08425"), Rate(Currency.Gbp, Currency.Usd, "1.26745"));

        // Act
        ExchangeRate eurGbp = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(Currency.Eur, Currency.Gbp, "0.85546"), eurGbp);
    }

    [Fact]
    public void A_cross_rate_with_a_pivot_quoted_the_other_way_round_is_still_triangulated() {
        // Arrange: EUR is worth 1.08 USD, 1 USD is worth 150 JPY, so 1 EUR is worth 162 JPY.
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08"), Rate(Currency.Usd, Currency.Jpy, "150"));

        // Act
        ExchangeRate eurJpy = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Jpy), MidpointRounding.ToEven);
        ExchangeRate jpyEur = sheet.RateFor(new CurrencyPair(Currency.Jpy, Currency.Eur), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(Currency.Eur, Currency.Jpy, "162"), eurJpy);
        Assert.Equal(Rate(Currency.Jpy, Currency.Eur, "0.00617"), jpyEur);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.50002")]
    [InlineData(MidpointRounding.AwayFromZero, "0.50003")]
    public void A_cross_rate_on_a_midpoint_is_rounded_once_with_the_mode_given(MidpointRounding mode, string expected) {
        // Arrange: 1.00005 / 2 = 0.500025.
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.00005"), Rate(Currency.Gbp, Currency.Usd, "2"));

        // Act
        ExchangeRate eurGbp = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), mode);

        // Assert
        Assert.Equal(D(expected), eurGbp.Value);
    }

    [Fact]
    public void A_pair_the_sheet_cannot_price_is_refused() {
        // Arrange
        Currency anyUnquoted = new AnyCurrency().Except(Currency.Usd, Currency.Eur).Generate();
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1.08"));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, anyUnquoted), MidpointRounding.ToEven));
    }

    [Fact]
    public void A_cross_rate_above_the_maximum_is_refused() {
        // Arrange: a euro worth a million dollars against a yen worth a hundred-thousandth of one.
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "1000000"), Rate(Currency.Jpy, Currency.Usd, "0.00001"));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Jpy), MidpointRounding.ToEven));
    }

    [Fact]
    public void A_cross_rate_that_rounds_to_zero_is_refused() {
        // Arrange
        RateSheet sheet = UsdSheet(Rate(Currency.Eur, Currency.Usd, "0.00001"), Rate(Currency.Gbp, Currency.Usd, "1000000"));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), MidpointRounding.ToEven));
    }

    [Fact]
    public void RateFor_refuses_a_missing_pair() {
        // Arrange
        RateSheet anySheet = Any.RateSheet().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySheet.RateFor(null!, MidpointRounding.ToEven));
    }

}
