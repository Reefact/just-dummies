using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class PromoCodeTests {

    private static readonly DateOnly From  = new DateOnly(2026, 3, 1);
    private static readonly DateOnly Until = new DateOnly(2026, 3, 31);

    private static PromoCode PromoBetween(DateOnly from, DateOnly until, decimal minimumBasket = 0m) {
        return Any.PromoCode().WithValidFrom(from).WithValidUntil(until).WithMinimumBasket(minimumBasket).Generate();
    }

    [Fact]
    public void Every_field_is_kept_as_given() {
        // Act
        PromoCode promo = new PromoCode("SPRING", 15m, From, Until, 30m, 100);

        // Assert
        Assert.Equal("SPRING", promo.Code);
        Assert.Equal(15m, promo.PercentOff);
        Assert.Equal(From, promo.ValidFrom);
        Assert.Equal(Until, promo.ValidUntil);
        Assert.Equal(30m, promo.MinimumBasket);
        Assert.Equal(100, promo.MaxUses);
    }

    [Theory]
    [InlineData("spring25", "SPRING25")]
    [InlineData("MixedCase", "MIXEDCASE")]
    [InlineData("a-b_", "A-B_")]
    public void The_code_is_stored_upper_cased(string given, string expected) {
        // Arrange
        PromoCode promo = Any.PromoCode().WithCode(given).Generate();

        // Act
        string code = promo.Code;

        // Assert
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("ABCDEFGHIJKL")]
    public void A_code_of_4_to_12_characters_is_accepted(string code) {
        // Act
        PromoCode promo = Any.PromoCode().WithCode(code).Generate();

        // Assert
        Assert.Equal(code, promo.Code);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("a")]
    public void A_code_shorter_than_4_or_longer_than_12_is_refused(string code) {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PromoCode(code, 10m, anyDate, anyDate, 0m, 1));

        // Assert
        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PromoCode(null!, 10m, From, Until, 0m, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("      ")]
    [InlineData("\t\t\t\t\t")]
    public void A_blank_code_is_refused_even_when_long_enough(string code) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PromoCode(code, 10m, From, Until, 0m, 1));

        // Assert
        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("25")]
    [InlineData("50")]
    public void A_percentage_above_0_and_up_to_50_is_accepted(string percentOff) {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        PromoCode promo = new PromoCode(anyCode, decimal.Parse(percentOff, System.Globalization.CultureInfo.InvariantCulture), From, Until, 0m, 1);

        // Assert
        Assert.Equal(decimal.Parse(percentOff, System.Globalization.CultureInfo.InvariantCulture), promo.PercentOff);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("50.01")]
    [InlineData("100")]
    public void A_percentage_of_0_or_less_or_above_50_is_refused(string percentOff) {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PromoCode(anyCode, decimal.Parse(percentOff, System.Globalization.CultureInfo.InvariantCulture), From, Until, 0m, 1));

        // Assert
        Assert.Equal("percentOff", exception.ParamName);
    }

    [Fact]
    public void A_promo_valid_for_a_single_day_is_accepted() {
        // Act
        PromoCode promo = PromoBetween(From, From);

        // Assert
        Assert.Equal(promo.ValidFrom, promo.ValidUntil);
    }

    [Fact]
    public void A_promo_expiring_before_it_starts_is_refused() {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PromoCode(anyCode, 10m, Until, From, 0m, 1));

        // Assert
        Assert.Equal("validUntil", exception.ParamName);
    }

    [Fact]
    public void A_promo_expiring_the_day_before_it_starts_is_refused() {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PromoCode(anyCode, 10m, From, From.AddDays(-1), 0m, 1));
    }

    [Fact]
    public void A_minimum_basket_of_zero_is_accepted() {
        // Act
        PromoCode promo = Any.PromoCode().WithMinimumBasket(0m).Generate();

        // Assert
        Assert.Equal(0m, promo.MinimumBasket);
    }

    [Fact]
    public void A_negative_minimum_basket_is_refused() {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(anyCode, 10m, From, Until, -0.01m, 1));

        // Assert
        Assert.Equal("minimumBasket", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1_000_000)]
    [InlineData(int.MaxValue)]
    public void A_positive_maximum_number_of_uses_is_accepted(int maxUses) {
        // Act
        PromoCode promo = Any.PromoCode().WithMaxUses(maxUses).Generate();

        // Assert
        Assert.Equal(maxUses, promo.MaxUses);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_maximum_number_of_uses_below_1_is_refused(int maxUses) {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(anyCode, 10m, From, Until, 0m, maxUses));

        // Assert
        Assert.Equal("maxUses", exception.ParamName);
    }

    [Fact]
    public void The_maximum_number_of_uses_is_checked_last() {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(anyCode, 10m, From, Until, -1m, 0));

        // Assert
        Assert.Equal("minimumBasket", exception.ParamName);
    }

    [Fact]
    public void The_validity_window_is_checked_before_the_minimum_basket() {
        // Arrange
        string anyCode = Any.String().Alpha().WithLengthBetween(4, 12).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PromoCode(anyCode, 10m, Until, From, -1m, 1));

        // Assert
        Assert.Equal("validUntil", exception.ParamName);
    }

    [Theory]
    [InlineData(2026, 3, 1)]
    [InlineData(2026, 3, 15)]
    [InlineData(2026, 3, 31)]
    public void A_promo_applies_on_any_day_of_its_window_both_ends_included(int year, int month, int day) {
        // Arrange
        PromoCode promo  = PromoBetween(From, Until);
        Money     basket = Any.Money().Generate();

        // Act
        bool applies = promo.AppliesTo(basket, new DateOnly(year, month, day));

        // Assert
        Assert.True(applies);
    }

    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2026, 4, 1)]
    [InlineData(2020, 1, 1)]
    public void A_promo_does_not_apply_outside_its_window(int year, int month, int day) {
        // Arrange
        PromoCode promo  = PromoBetween(From, Until);
        Money     basket = Any.Money().Generate();

        // Act
        bool applies = promo.AppliesTo(basket, new DateOnly(year, month, day));

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void A_single_day_promo_applies_that_day_only() {
        // Arrange
        PromoCode promo  = PromoBetween(From, From);
        Money     basket = Any.Money().Generate();

        // Act & Assert
        Assert.True(promo.AppliesTo(basket, From));
        Assert.False(promo.AppliesTo(basket, From.AddDays(-1)));
        Assert.False(promo.AppliesTo(basket, From.AddDays(1)));
    }

    [Fact]
    public void A_promo_applies_to_a_basket_equal_to_its_minimum() {
        // Arrange
        PromoCode promo = PromoBetween(From, Until, minimumBasket: 30m);
        DateOnly  anyDayInWindow = Any.DateOnly().Between(From, Until).Generate();

        // Act
        bool applies = promo.AppliesTo(new Money(30.00m, "EUR"), anyDayInWindow);

        // Assert
        Assert.True(applies);
    }

    [Fact]
    public void A_promo_applies_to_a_basket_above_its_minimum() {
        // Arrange
        PromoCode promo = PromoBetween(From, Until, minimumBasket: 30m);
        DateOnly  anyDayInWindow = Any.DateOnly().Between(From, Until).Generate();

        // Act
        bool applies = promo.AppliesTo(new Money(30.01m, "EUR"), anyDayInWindow);

        // Assert
        Assert.True(applies);
    }

    [Fact]
    public void A_promo_does_not_apply_to_a_basket_below_its_minimum() {
        // Arrange
        PromoCode promo = PromoBetween(From, Until, minimumBasket: 30m);
        DateOnly  anyDayInWindow = Any.DateOnly().Between(From, Until).Generate();

        // Act
        bool applies = promo.AppliesTo(new Money(29.99m, "EUR"), anyDayInWindow);

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void A_promo_without_minimum_applies_to_an_empty_basket() {
        // Arrange
        PromoCode promo = PromoBetween(From, Until, minimumBasket: 0m);
        DateOnly  anyDayInWindow = Any.DateOnly().Between(From, Until).Generate();

        // Act
        bool applies = promo.AppliesTo(Money.Zero("EUR"), anyDayInWindow);

        // Assert
        Assert.True(applies);
    }

    [Fact]
    public void The_currency_of_the_basket_is_not_compared() {
        // Arrange
        PromoCode promo = PromoBetween(From, Until, minimumBasket: 30m);
        DateOnly  anyDayInWindow = Any.DateOnly().Between(From, Until).Generate();

        // Act & Assert
        Assert.True(promo.AppliesTo(new Money(30m, "EUR"), anyDayInWindow));
        Assert.True(promo.AppliesTo(new Money(30m, "USD"), anyDayInWindow));
        Assert.True(promo.AppliesTo(new Money(30m, "GBP"), anyDayInWindow));
    }

    [Fact]
    public void Both_conditions_must_hold() {
        // Arrange
        PromoCode promo = PromoBetween(From, Until, minimumBasket: 30m);

        // Act & Assert
        Assert.False(promo.AppliesTo(new Money(10m, "EUR"), Until.AddDays(1)));
        Assert.False(promo.AppliesTo(new Money(10m, "EUR"), From));
        Assert.False(promo.AppliesTo(new Money(100m, "EUR"), Until.AddDays(1)));
        Assert.True(promo.AppliesTo(new Money(100m, "EUR"), From));
    }

}
