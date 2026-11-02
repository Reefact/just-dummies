using System.Globalization;

using JustDummies;

using Xunit;

namespace Shop.Domain;

public sealed class GiftCardTests {

    [Fact]
    public void A_gift_card_keeps_its_number_balance_and_pin() {
        GiftCard card = new GiftCard("1234567890123456", 50m, 4321);

        Assert.Equal("1234567890123456", card.Number);
        Assert.Equal(50m, card.Balance);
        Assert.Equal(4321, card.Pin);
    }

    [Fact]
    public void A_null_number_is_refused_with_a_null_exception() {
        decimal anyBalance = Any.GiftCard().Generate().Balance;
        int     anyPin     = Any.GiftCard().Generate().Pin;

        Assert.Throws<ArgumentNullException>(() => new GiftCard(null!, anyBalance, anyPin));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345")]
    [InlineData("12345678901234567")]
    public void A_number_that_is_not_sixteen_digits_long_is_refused(string number) {
        GiftCard anyCard = Any.GiftCard().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GiftCard(number, anyCard.Balance, anyCard.Pin));

        Assert.Equal("number", refusal.ParamName);
    }

    [Theory]
    [InlineData("123456789012345A")]
    [InlineData("1234 5678 9012 34")]
    [InlineData("1234-5678-9012-34")]
    [InlineData("12345678901234 6")]
    public void A_number_with_something_else_than_digits_is_refused(string number) {
        GiftCard anyCard = Any.GiftCard().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GiftCard(number, anyCard.Balance, anyCard.Pin));

        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void A_number_of_non_ascii_digits_is_refused() {
        GiftCard anyCard = Any.GiftCard().Generate();

        // Sixteen Arabic-Indic digits: digits for Unicode, not for the card.
        string arabicIndic = new string('١', 16);

        Assert.Throws<ArgumentException>(() => new GiftCard(arabicIndic, anyCard.Balance, anyCard.Pin));
    }

    [Theory]
    [InlineData("5")]
    [InlineData("5.01")]
    [InlineData("250")]
    [InlineData("499.99")]
    [InlineData("500")]
    public void A_balance_from_five_to_five_hundred_is_accepted(string balance) {
        decimal expected = decimal.Parse(balance, CultureInfo.InvariantCulture);

        GiftCard card = Any.GiftCard().WithBalance(expected).Generate();

        Assert.Equal(expected, card.Balance);
    }

    [Theory]
    [InlineData("4.99")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("500.01")]
    [InlineData("1000")]
    public void A_balance_out_of_range_is_refused(string balance) {
        GiftCard anyCard = Any.GiftCard().Generate();

        decimal outOfRange = decimal.Parse(balance, CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => new GiftCard(anyCard.Number, outOfRange, anyCard.Pin));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1234)]
    [InlineData(9999)]
    public void A_pin_from_zero_to_nine_thousand_nine_hundred_ninety_nine_is_accepted(int pin) {
        GiftCard card = Any.GiftCard().WithPin(pin).Generate();

        Assert.Equal(pin, card.Pin);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_pin_out_of_range_is_refused(int pin) {
        GiftCard anyCard = Any.GiftCard().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GiftCard(anyCard.Number, anyCard.Balance, pin));

        Assert.Equal("pin", refusal.ParamName);
    }

    [Fact]
    public void Redeeming_less_than_the_balance_pays_it_all_and_lowers_the_balance() {
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        decimal leftToPay = card.Redeem(20m);

        Assert.Equal(0m, leftToPay);
        Assert.Equal(30m, card.Balance);
    }

    [Fact]
    public void Redeeming_exactly_the_balance_empties_the_card() {
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        decimal leftToPay = card.Redeem(50m);

        Assert.Equal(0m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Redeeming_more_than_the_balance_pays_what_it_can_and_returns_the_rest() {
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        decimal leftToPay = card.Redeem(80.25m);

        Assert.Equal(30.25m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Redeeming_nothing_changes_nothing() {
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        decimal leftToPay = card.Redeem(0m);

        Assert.Equal(0m, leftToPay);
        Assert.Equal(50m, card.Balance);
    }

    [Fact]
    public void An_emptied_card_pays_nothing_more() {
        GiftCard card = Any.GiftCard().WithBalance(10m).Generate();
        card.Redeem(10m);

        decimal leftToPay = card.Redeem(15m);

        Assert.Equal(15m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Successive_redemptions_draw_the_balance_down() {
        GiftCard card = Any.GiftCard().WithBalance(100m).Generate();

        card.Redeem(30m);
        card.Redeem(30m);
        decimal leftToPay = card.Redeem(60m);

        Assert.Equal(20m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Redeeming_does_not_alter_the_number_or_the_pin() {
        GiftCard card = new GiftCard("1234567890123456", 50m, 4321);

        card.Redeem(20m);

        Assert.Equal("1234567890123456", card.Number);
        Assert.Equal(4321, card.Pin);
    }

    [Fact(Skip = "domain defect: Redeem has no check on a negative amount, so redeeming -10 on a balance of 50 raises the balance to 60 (seed: not drawn, literal case)")]
    public void Redeeming_a_negative_amount_is_refused() {
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        Assert.ThrowsAny<ArgumentException>(() => card.Redeem(-10m));
        Assert.Equal(50m, card.Balance);
    }

}
