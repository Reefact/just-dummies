using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class RateSheetTests {

    private static readonly CurrencyPair EurUsd = new CurrencyPair(Currency.Eur, Currency.Usd);
    private static readonly CurrencyPair UsdEur = new CurrencyPair(Currency.Usd, Currency.Eur);
    private static readonly CurrencyPair UsdJpy = new CurrencyPair(Currency.Usd, Currency.Jpy);
    private static readonly CurrencyPair GbpUsd = new CurrencyPair(Currency.Gbp, Currency.Usd);
    private static readonly CurrencyPair UsdGbp = new CurrencyPair(Currency.Usd, Currency.Gbp);

    private static ExchangeRate Rate(CurrencyPair pair, string value) {
        return new ExchangeRate(pair, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static RateSheet UsdSheetOf(params ExchangeRate[] rates) {
        return new RateSheet(Currency.Usd, Week.Monday, rates);
    }

    [Fact]
    public void A_sheet_quotes_currencies_against_its_pivot_either_way_round() {
        // Act
        RateSheet sheet = new RateSheet(Currency.Usd, Week.Monday, [Rate(EurUsd, "1.08"), Rate(UsdJpy, "150")]);

        // Assert
        Assert.Equal(Currency.Usd, sheet.Pivot);
        Assert.Equal(Week.Monday, sheet.AsOf);
        Assert.Equal(2, sheet.Rates.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reference_rates_are_fixed_on_business_days_only(bool saturday) {
        // Arrange
        ExchangeRate anyRate    = new AnyExchangeRate().WithPair(EurUsd).Generate();
        DateOnly     anyWeekend = saturday ? Days.On(DayOfWeek.Saturday).Generate() : Days.On(DayOfWeek.Sunday).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(Currency.Usd, anyWeekend, [anyRate]));
    }

    [Fact]
    public void A_sheet_quotes_at_least_one_currency() {
        // Arrange
        DateOnly anyBusinessDay = Any.BusinessDay().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(Currency.Usd, anyBusinessDay, []));
    }

    [Fact]
    public void Every_rate_must_involve_the_pivot() {
        // Arrange
        ExchangeRate eurGbp = new AnyExchangeRate().WithPair(new CurrencyPair(Currency.Eur, Currency.Gbp)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(Currency.Usd, Week.Monday, [eurGbp]));
    }

    [Fact]
    public void A_currency_is_quoted_once_whichever_way_round() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(Currency.Usd, Week.Monday, [Rate(EurUsd, "1.08"), Rate(UsdEur, "0.92593")]));
        Assert.Throws<ArgumentException>(() => new RateSheet(Currency.Usd, Week.Monday, [Rate(EurUsd, "1.08"), Rate(EurUsd, "1.09")]));
    }

    [Fact]
    public void A_sheet_holds_no_null_rate() {
        // Arrange
        ExchangeRate anyRate = new AnyExchangeRate().WithPair(EurUsd).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RateSheet(Currency.Usd, Week.Monday, [anyRate, null!]));
    }

    [Fact]
    public void A_sheet_needs_a_pivot_and_a_list_of_rates() {
        // Arrange
        ExchangeRate anyRate = new AnyExchangeRate().WithPair(EurUsd).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RateSheet(null!, Week.Monday, [anyRate]));
        Assert.Throws<ArgumentNullException>(() => new RateSheet(Currency.Usd, Week.Monday, null!));
    }

    [Fact]
    public void A_sheet_covers_its_pivot_and_every_currency_it_quotes() {
        // Arrange
        RateSheet sheet            = UsdSheetOf(Rate(EurUsd, "1.08"), Rate(UsdJpy, "150"));
        Currency  anyUnquotedOne   = Any.Currency().Except(Currency.Usd, Currency.Eur, Currency.Jpy).Generate();

        // Assert
        Assert.True(sheet.Covers(Currency.Usd));
        Assert.True(sheet.Covers(Currency.Eur));
        Assert.True(sheet.Covers(Currency.Jpy));
        Assert.False(sheet.Covers(anyUnquotedOne));
    }

    [Fact]
    public void A_sheet_covers_a_pair_when_it_covers_both_its_currencies() {
        // Arrange
        RateSheet sheet          = UsdSheetOf(Rate(EurUsd, "1.08"), Rate(UsdJpy, "150"));
        Currency  anyUnquotedOne = Any.Currency().Except(Currency.Usd, Currency.Eur, Currency.Jpy).Generate();

        // Assert
        Assert.True(sheet.Covers(EurUsd));
        Assert.True(sheet.Covers(UsdEur));
        Assert.True(sheet.Covers(new CurrencyPair(Currency.Eur, Currency.Jpy)));
        Assert.False(sheet.Covers(new CurrencyPair(Currency.Eur, anyUnquotedOne)));
        Assert.False(sheet.Covers(new CurrencyPair(anyUnquotedOne, Currency.Usd)));
    }

    [Fact]
    public void A_quoted_pair_is_priced_at_its_quoted_rate() {
        // Arrange
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1.08"), Rate(UsdJpy, "150"));

        // Act
        ExchangeRate rate = sheet.RateFor(UsdJpy, MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(UsdJpy, "150"), rate);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.92593")]
    [InlineData(MidpointRounding.AwayFromZero, "0.92593")]
    public void The_inverse_of_a_quoted_pair_is_its_rate_inverted(MidpointRounding mode, string expected) {
        // Arrange
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1.08"));

        // Act
        ExchangeRate rate = sheet.RateFor(UsdEur, mode);

        // Assert
        Assert.Equal(Rate(UsdEur, expected), rate);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.01562")]
    [InlineData(MidpointRounding.AwayFromZero, "0.01563")]
    public void An_inverse_that_lands_on_a_midpoint_follows_the_mode_asked(MidpointRounding mode, string expected) {
        // Arrange: 1 / 64 = 0.015625
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "64"));

        // Act
        ExchangeRate rate = sheet.RateFor(UsdEur, mode);

        // Assert
        Assert.Equal(Rate(UsdEur, expected), rate);
    }

    [Fact]
    public void A_pair_quoted_the_other_way_round_is_priced_by_the_same_inversion() {
        // Arrange
        RateSheet sheet = UsdSheetOf(Rate(UsdEur, "0.92"));

        // Act
        ExchangeRate rate = sheet.RateFor(EurUsd, MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(EurUsd, "1.08696"), rate);
    }

    [Fact]
    public void A_cross_rate_goes_through_the_pivot() {
        // Arrange
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1.08"), Rate(UsdJpy, "150"));

        // Act
        ExchangeRate rate = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Jpy), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(new CurrencyPair(Currency.Eur, Currency.Jpy), "162"), rate);
    }

    [Fact]
    public void A_cross_rate_is_the_same_whichever_currencies_are_quoted_against_the_pivot_as_base() {
        // Arrange
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1.08"), Rate(GbpUsd, "1.25"));

        // Act
        ExchangeRate rate = sheet.RateFor(new CurrencyPair(Currency.Gbp, Currency.Eur), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(new CurrencyPair(Currency.Gbp, Currency.Eur), "1.15741"), rate);
    }

    [Fact]
    public void A_cross_rate_is_rounded_once_not_built_from_rounded_inverses() {
        // Arrange: 0.81 / 0.92 = 0.880434..., where 1.08696 / 1.23457, the inverses rounded first, would give 0.88044
        RateSheet sheet = UsdSheetOf(Rate(UsdEur, "0.92"), Rate(UsdGbp, "0.81"));

        // Act
        ExchangeRate rate = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), MidpointRounding.ToEven);

        // Assert
        Assert.Equal(Rate(new CurrencyPair(Currency.Eur, Currency.Gbp), "0.88043"), rate);
    }

    [Theory]
    [InlineData(MidpointRounding.ToEven, "0.50000")]
    [InlineData(MidpointRounding.AwayFromZero, "0.50001")]
    public void A_cross_rate_that_lands_on_a_midpoint_follows_the_mode_asked(MidpointRounding mode, string expected) {
        // Arrange: 1.00001 / 2 = 0.500005
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1.00001"), Rate(GbpUsd, "2"));

        // Act
        ExchangeRate rate = sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), mode);

        // Assert
        Assert.Equal(Rate(new CurrencyPair(Currency.Eur, Currency.Gbp), expected), rate);
    }

    [Fact]
    public void A_pair_with_an_uncovered_currency_cannot_be_priced() {
        // Arrange
        RateSheet sheet          = UsdSheetOf(Rate(EurUsd, "1.08"));
        Currency  anyUnquotedOne = Any.Currency().Except(Currency.Usd, Currency.Eur).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, anyUnquotedOne), MidpointRounding.ToEven));
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(anyUnquotedOne, Currency.Usd), MidpointRounding.ToEven));
    }

    [Fact]
    public void A_cross_rate_that_rounds_to_zero_cannot_be_quoted() {
        // Arrange: 0.00001 / 100 000 = 1e-10
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "0.00001"), Rate(GbpUsd, "100000"));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), MidpointRounding.ToEven));
    }

    [Fact]
    public void A_cross_rate_above_one_million_cannot_be_quoted() {
        // Arrange: 1 000 000 / 0.00001 = 1e11
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1000000"), Rate(GbpUsd, "0.00001"));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), MidpointRounding.ToEven));
    }

    [Fact]
    public void Pricing_refuses_a_mode_that_is_not_a_midpoint_mode() {
        // Arrange
        RateSheet sheet = UsdSheetOf(Rate(EurUsd, "1.08"), Rate(GbpUsd, "1.25"));

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.RateFor(new CurrencyPair(Currency.Eur, Currency.Gbp), (MidpointRounding)99));
    }

}
