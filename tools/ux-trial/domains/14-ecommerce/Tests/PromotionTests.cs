using JustDummies;
using JustDummies.Xunit;

using Shop.Domain;

using Xunit;

namespace Shop.Tests;

[Reproducible]
public sealed class PromotionTests {

    // The scaffolded base for maxUses is unguarded (see Bug_Scaffolded_AnyPromoCode_...); refine it here.
    private static AnyPromoCode AnyPromo() => Any.PromoCode().WithMaxUses(uses => uses.Positive());

    [Fact]
    public void A_promo_code_applies_on_its_last_valid_day() {
        // Arrange
        DateOnly lastDay  = new DateOnly(2026, 3, 31);
        PromoCode anyPromo = AnyPromo().WithValidUntil(lastDay).WithMinimumBasket(0m).Generate();
        Money anyBasket = Any.Money().Generate();

        // Act
        bool applies = anyPromo.AppliesTo(anyBasket, lastDay);

        // Assert
        Assert.True(applies);
    }

    [Fact]
    public void A_promo_code_does_not_apply_below_its_minimum_basket() {
        // Arrange
        PromoCode anyPromo  = AnyPromo().WithMinimumBasket(40m).Generate();
        Money     basket    = new Money(39.99m, Any.OneOf("EUR", "USD", "GBP").Generate());
        DateOnly  anyDayInWindow = Any.DateOnly().Between(anyPromo.ValidFrom, anyPromo.ValidUntil).Generate();

        // Act
        bool applies = anyPromo.AppliesTo(basket, anyDayInWindow);

        // Assert
        Assert.False(applies);
    }

    [Fact]
    public void A_gift_card_pays_what_it_holds_and_leaves_the_rest_to_pay() {
        // Arrange
        GiftCard anyCardOf30 = Any.GiftCard().WithBalance(30m).Generate();

        // Act
        decimal leftToPay = anyCardOf30.Redeem(45m);

        // Assert
        Assert.Equal(15m, leftToPay);
        Assert.Equal(0m, anyCardOf30.Balance);
    }

}
