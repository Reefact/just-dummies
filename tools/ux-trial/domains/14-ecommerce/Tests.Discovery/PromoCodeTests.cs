using System.Globalization;

using JustDummies;

using Xunit;

namespace Shop.Domain;

public sealed class PromoCodeTests {

    private static readonly DateOnly June15 = new DateOnly(2026, 6, 15);

    [Fact]
    public void A_promo_code_keeps_its_terms() {
        PromoCode promo = new PromoCode("SUMMER", 15m, new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31), 40m, 100);

        Assert.Equal("SUMMER", promo.Code);
        Assert.Equal(15m, promo.PercentOff);
        Assert.Equal(new DateOnly(2026, 6, 1), promo.ValidFrom);
        Assert.Equal(new DateOnly(2026, 8, 31), promo.ValidUntil);
        Assert.Equal(40m, promo.MinimumBasket);
        Assert.Equal(100, promo.MaxUses);
    }

    [Theory]
    [InlineData("summer", "SUMMER")]
    [InlineData("Summer1", "SUMMER1")]
    [InlineData("abcd", "ABCD")]
    public void The_code_is_stored_in_upper_case(string given, string stored) {
        PromoCode promo = Any.PromoCode().WithCode(given).Generate();

        Assert.Equal(stored, promo.Code);
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("abcdefghijkl")]
    public void A_code_of_four_to_twelve_characters_is_accepted(string code) {
        PromoCode promo = Any.PromoCode().WithCode(code).Generate();

        Assert.Equal(code.ToUpperInvariant(), promo.Code);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("abcdefghijklm")]
    public void A_code_shorter_than_four_or_longer_than_twelve_characters_is_refused(string code) {
        PromoCode anyPromo = Any.PromoCode().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PromoCode(code, anyPromo.PercentOff, anyPromo.ValidFrom, anyPromo.ValidUntil, anyPromo.MinimumBasket, anyPromo.MaxUses));

        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void The_length_is_measured_on_the_code_as_given_spaces_included() {
        PromoCode promo = Any.PromoCode().WithCode(" ab ").Generate();

        Assert.Equal(" AB ", promo.Code);
    }

    [Fact]
    public void A_null_code_is_refused_with_a_null_exception() {
        PromoCode anyPromo = Any.PromoCode().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new PromoCode(null!, anyPromo.PercentOff, anyPromo.ValidFrom, anyPromo.ValidUntil, anyPromo.MinimumBasket, anyPromo.MaxUses));
    }

    [Theory]
    [InlineData("")]
    [InlineData("      ")]
    [InlineData("\t\t\t\t\t")]
    public void A_blank_code_is_refused(string code) {
        PromoCode anyPromo = Any.PromoCode().Generate();

        Assert.Throws<ArgumentException>(
            () => new PromoCode(code, anyPromo.PercentOff, anyPromo.ValidFrom, anyPromo.ValidUntil, anyPromo.MinimumBasket, anyPromo.MaxUses));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("25")]
    [InlineData("50")]
    public void A_percentage_above_zero_and_up_to_fifty_is_accepted(string percent) {
        decimal expected = decimal.Parse(percent, CultureInfo.InvariantCulture);

        PromoCode promo = Any.PromoCode().WithPercentOff(expected).Generate();

        Assert.Equal(expected, promo.PercentOff);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("50.01")]
    [InlineData("100")]
    public void A_percentage_of_zero_or_less_or_above_fifty_is_refused(string percent) {
        PromoCode anyPromo = Any.PromoCode().Generate();

        decimal outOfRange = decimal.Parse(percent, CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PromoCode(anyPromo.Code, outOfRange, anyPromo.ValidFrom, anyPromo.ValidUntil, anyPromo.MinimumBasket, anyPromo.MaxUses));
    }

    [Fact]
    public void A_single_day_validity_is_accepted() {
        PromoCode promo = Any.PromoCode().WithValidFrom(June15).WithValidUntil(June15).Generate();

        Assert.Equal(promo.ValidFrom, promo.ValidUntil);
    }

    [Fact]
    public void A_code_cannot_expire_before_it_starts() {
        PromoCode anyPromo = Any.PromoCode().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PromoCode(anyPromo.Code, anyPromo.PercentOff, new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 14), anyPromo.MinimumBasket, anyPromo.MaxUses));

        Assert.Equal("validUntil", refusal.ParamName);
    }

    [Fact]
    public void A_zero_minimum_basket_is_accepted() {
        PromoCode promo = Any.PromoCode().WithMinimumBasket(0m).Generate();

        Assert.Equal(0m, promo.MinimumBasket);
    }

    [Fact]
    public void A_negative_minimum_basket_is_refused() {
        PromoCode anyPromo = Any.PromoCode().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PromoCode(anyPromo.Code, anyPromo.PercentOff, anyPromo.ValidFrom, anyPromo.ValidUntil, -0.01m, anyPromo.MaxUses));
    }

    [Fact]
    public void A_single_use_is_accepted() {
        PromoCode promo = Any.PromoCode().WithMaxUses(1).Generate();

        Assert.Equal(1, promo.MaxUses);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Fewer_than_one_use_is_refused(int maxUses) {
        PromoCode anyPromo = Any.PromoCode().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PromoCode(anyPromo.Code, anyPromo.PercentOff, anyPromo.ValidFrom, anyPromo.ValidUntil, anyPromo.MinimumBasket, maxUses));

        Assert.Equal("maxUses", refusal.ParamName);
    }

    [Fact]
    public void The_max_uses_are_checked_last() {
        PromoCode anyPromo = Any.PromoCode().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PromoCode(anyPromo.Code, 99m, anyPromo.ValidFrom, anyPromo.ValidUntil, anyPromo.MinimumBasket, 0));

        Assert.Equal("percentOff", refusal.ParamName);
    }

    [Fact]
    public void A_promo_applies_on_its_first_and_last_day_and_between() {
        PromoCode promo = Any.PromoCode().WithValidFrom(new DateOnly(2026, 6, 1)).WithValidUntil(new DateOnly(2026, 6, 30)).WithMinimumBasket(0m).Generate();
        Money     basket = new Money(10m, "EUR");

        Assert.True(promo.AppliesTo(basket, new DateOnly(2026, 6, 1)));
        Assert.True(promo.AppliesTo(basket, new DateOnly(2026, 6, 15)));
        Assert.True(promo.AppliesTo(basket, new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void A_promo_does_not_apply_the_day_before_it_starts_nor_the_day_after_it_ends() {
        PromoCode promo = Any.PromoCode().WithValidFrom(new DateOnly(2026, 6, 1)).WithValidUntil(new DateOnly(2026, 6, 30)).WithMinimumBasket(0m).Generate();
        Money     basket = new Money(10m, "EUR");

        Assert.False(promo.AppliesTo(basket, new DateOnly(2026, 5, 31)));
        Assert.False(promo.AppliesTo(basket, new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void A_promo_applies_from_exactly_the_minimum_basket_up() {
        PromoCode promo = Any.PromoCode().WithValidFrom(June15).WithValidUntil(June15).WithMinimumBasket(40m).Generate();

        Assert.True(promo.AppliesTo(new Money(40m, "EUR"), June15));
        Assert.True(promo.AppliesTo(new Money(40.01m, "EUR"), June15));
        Assert.False(promo.AppliesTo(new Money(39.99m, "EUR"), June15));
        Assert.False(promo.AppliesTo(Money.Zero("EUR"), June15));
    }

    [Fact]
    public void A_promo_needs_both_the_date_and_the_basket() {
        PromoCode promo = Any.PromoCode().WithValidFrom(June15).WithValidUntil(June15).WithMinimumBasket(40m).Generate();

        Assert.False(promo.AppliesTo(new Money(100m, "EUR"), June15.AddDays(1)));
        Assert.False(promo.AppliesTo(new Money(10m, "EUR"), June15));
    }

    [Fact]
    public void The_basket_currency_is_not_compared() {
        PromoCode promo = Any.PromoCode().WithValidFrom(June15).WithValidUntil(June15).WithMinimumBasket(40m).Generate();

        Assert.True(promo.AppliesTo(new Money(40m, "USD"), June15));
        Assert.True(promo.AppliesTo(new Money(40m, "GBP"), June15));
    }

}
