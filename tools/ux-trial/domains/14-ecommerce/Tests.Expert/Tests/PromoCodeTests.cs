using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class PromoCodeTests {

    private static readonly DateOnly March1  = new DateOnly(2026, 3, 1);
    private static readonly DateOnly March31 = new DateOnly(2026, 3, 31);

    private static readonly AnyString AnyValidCode = Any.String().NotBlank().WithLengthBetween(4, 12);

    #region Construction

    [Fact]
    public void A_promo_keeps_what_it_was_given() {
        // Arrange
        string anyCode    = AnyValidCode.Generate();
        int    anyMaxUses = Any.Int32().Positive().Generate();

        // Act
        PromoCode promo = new PromoCode(anyCode, 15m, March1, March31, 30m, anyMaxUses);

        // Assert
        Assert.Equal(15m, promo.PercentOff);
        Assert.Equal(March1, promo.ValidFrom);
        Assert.Equal(March31, promo.ValidUntil);
        Assert.Equal(30m, promo.MinimumBasket);
        Assert.Equal(anyMaxUses, promo.MaxUses);
    }

    [Fact]
    public void The_code_is_stored_in_upper_case() {
        // Act
        PromoCode promo = Any.PromoCode().WithCode("spring26").Generate();

        // Assert
        Assert.Equal("SPRING26", promo.Code);
    }

    [Fact]
    public void A_missing_code_is_refused_as_null() {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new PromoCode(null!, valid.PercentOff, valid.ValidFrom, valid.ValidUntil, valid.MinimumBasket, valid.MaxUses));
        Assert.Equal("code", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("      ")]
    public void A_blank_code_is_refused(string code) {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PromoCode(code, valid.PercentOff, valid.ValidFrom, valid.ValidUntil, valid.MinimumBasket, valid.MaxUses));
        Assert.Equal("code", error.ParamName);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    public void A_code_of_four_to_twelve_characters_is_accepted(int length) {
        // Arrange
        string anyCode = Any.String().NotBlank().WithLength(length).Generate();

        // Act
        PromoCode promo = Any.PromoCode().WithCode(anyCode).Generate();

        // Assert
        Assert.Equal(length, promo.Code.Length);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(13)]
    public void A_code_shorter_than_four_or_longer_than_twelve_characters_is_refused(int length) {
        // Arrange
        string    anyCode = Any.String().NotBlank().WithLength(length).Generate();
        PromoCode valid   = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PromoCode(anyCode, valid.PercentOff, valid.ValidFrom, valid.ValidUntil, valid.MinimumBasket, valid.MaxUses));
        Assert.Equal("code", error.ParamName);
    }

    [Fact]
    public void The_code_length_is_measured_on_the_string_as_given_padding_included() {
        // A code of two letters and two spaces is four characters long and is not blank.
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act
        PromoCode promo = new PromoCode("ab  ", valid.PercentOff, valid.ValidFrom, valid.ValidUntil, valid.MinimumBasket, valid.MaxUses);

        // Assert
        Assert.Equal("AB  ", promo.Code);
    }

    public static TheoryData<decimal> AcceptedPercentages => new TheoryData<decimal> { 0.01m, 0.5m, 25m, 50m };

    [Theory]
    [MemberData(nameof(AcceptedPercentages))]
    public void A_percentage_above_zero_and_up_to_fifty_is_accepted(decimal percentOff) {
        // Act
        PromoCode promo = Any.PromoCode().WithPercentOff(percentOff).Generate();

        // Assert
        Assert.Equal(percentOff, promo.PercentOff);
    }

    public static TheoryData<decimal> RefusedPercentages => new TheoryData<decimal> { 0m, -1m, 50.01m, 51m, 100m };

    [Theory]
    [MemberData(nameof(RefusedPercentages))]
    public void A_percentage_of_zero_or_less_or_above_fifty_is_refused(decimal percentOff) {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(valid.Code, percentOff, valid.ValidFrom, valid.ValidUntil, valid.MinimumBasket, valid.MaxUses));
        Assert.Equal("percentOff", error.ParamName);
    }

    [Fact]
    public void A_promo_that_lasts_a_single_day_is_accepted() {
        // Act
        PromoCode promo = Any.PromoCode().WithValidFrom(March1).WithValidUntil(March1).Generate();

        // Assert
        Assert.Equal(promo.ValidFrom, promo.ValidUntil);
    }

    [Fact]
    public void A_promo_that_expires_the_day_before_it_starts_is_refused() {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PromoCode(valid.Code, valid.PercentOff, March1, March1.AddDays(-1), valid.MinimumBasket, valid.MaxUses));
        Assert.Equal("validUntil", error.ParamName);
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
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(valid.Code, valid.PercentOff, valid.ValidFrom, valid.ValidUntil, -0.01m, valid.MaxUses));
        Assert.Equal("minimumBasket", error.ParamName);
    }

    [Fact]
    public void A_promo_usable_once_is_accepted() {
        // Act
        PromoCode promo = Any.PromoCode().WithMaxUses(1).Generate();

        // Assert
        Assert.Equal(1, promo.MaxUses);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_promo_usable_less_than_once_is_refused(int maxUses) {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(valid.Code, valid.PercentOff, valid.ValidFrom, valid.ValidUntil, valid.MinimumBasket, maxUses));
        Assert.Equal("maxUses", error.ParamName);
    }

    [Fact]
    public void The_dates_are_checked_before_the_maximum_uses() {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PromoCode(valid.Code, valid.PercentOff, March31, March1, valid.MinimumBasket, 0));
        Assert.Equal("validUntil", error.ParamName);
    }

    [Fact]
    public void The_percentage_is_checked_before_the_dates() {
        // Arrange
        PromoCode valid = Any.PromoCode().Generate();

        // Act / Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new PromoCode(valid.Code, 60m, March31, March1, valid.MinimumBasket, valid.MaxUses));
        Assert.Equal("percentOff", error.ParamName);
    }

    #endregion

    #region AppliesTo

    private static PromoCode PromoFromMarch1ToMarch31WithMinimumBasket(decimal minimumBasket) {
        return Any.PromoCode().WithValidFrom(March1).WithValidUntil(March31).WithMinimumBasket(minimumBasket).Generate();
    }

    public static TheoryData<DateOnly> DaysWithinTheWindow => new TheoryData<DateOnly> { March1, new DateOnly(2026, 3, 15), March31 };

    [Theory]
    [MemberData(nameof(DaysWithinTheWindow))]
    public void A_promo_applies_on_every_day_of_its_window_bounds_included(DateOnly on) {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(0m);
        Money     basket = Any.Money().Generate();

        // Act
        bool applies = promo.AppliesTo(basket, on);

        // Assert
        Assert.True(applies);
    }

    public static TheoryData<DateOnly> DaysOutsideTheWindow => new TheoryData<DateOnly> { March1.AddDays(-1), March31.AddDays(1), DateOnly.MinValue, DateOnly.MaxValue };

    [Theory]
    [MemberData(nameof(DaysOutsideTheWindow))]
    public void A_promo_does_not_apply_outside_its_window(DateOnly on) {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(0m);
        Money     basket = Any.Money().Generate();

        // Act
        bool applies = promo.AppliesTo(basket, on);

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void A_promo_applies_to_a_basket_exactly_at_the_minimum() {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(40m);
        Money     basket = Any.Money().WithAmount(40m).Generate();

        // Act
        bool applies = promo.AppliesTo(basket, March1);

        // Assert
        Assert.True(applies);
    }

    [Fact]
    public void A_promo_does_not_apply_to_a_basket_a_cent_under_the_minimum() {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(40m);
        Money     basket = Any.Money().WithAmount(39.99m).Generate();

        // Act
        bool applies = promo.AppliesTo(basket, March1);

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void A_promo_with_no_minimum_applies_to_an_empty_basket() {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(0m);
        Money     basket = Any.Money().WithAmount(0m).Generate();

        // Act
        bool applies = promo.AppliesTo(basket, March1);

        // Assert
        Assert.True(applies);
    }

    [Fact]
    public void A_basket_well_under_the_minimum_does_not_qualify_even_inside_the_window() {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(40m);
        Money     basket = Any.Money().WithAmount(10m).Generate();

        // Act
        bool applies = promo.AppliesTo(basket, new DateOnly(2026, 3, 15));

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void A_big_basket_does_not_qualify_outside_the_window() {
        // Arrange
        PromoCode promo  = PromoFromMarch1ToMarch31WithMinimumBasket(40m);
        Money     basket = Any.Money().WithAmount(400m).Generate();

        // Act
        bool applies = promo.AppliesTo(basket, March31.AddDays(1));

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void The_currency_of_the_basket_is_not_compared_with_anything() {
        // Arrange: the minimum is a bare number, so 40 in any currency reaches it.
        PromoCode promo     = PromoFromMarch1ToMarch31WithMinimumBasket(40m);
        Money     usdBasket = Any.Money().WithAmount(40m).WithCurrency("USD").Generate();
        Money     gbpBasket = Any.Money().WithAmount(40m).WithCurrency("GBP").Generate();

        // Act
        bool usdApplies = promo.AppliesTo(usdBasket, March1);
        bool gbpApplies = promo.AppliesTo(gbpBasket, March1);

        // Assert
        Assert.True(usdApplies);
        Assert.True(gbpApplies);
    }

    #endregion

}
