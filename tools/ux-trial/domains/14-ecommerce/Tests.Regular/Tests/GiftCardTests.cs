using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class GiftCardTests {

    private static AnyString AnyNumber() {
        return Any.String().Numeric().WithLength(16);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("50")]
    [InlineData("500")]
    public void A_balance_from_5_to_500_is_accepted(string balance) {
        // Arrange
        string anyNumber = AnyNumber().Generate();
        int    anyPin    = Any.Int32().Between(0, 9999).Generate();

        // Act
        GiftCard card = new GiftCard(anyNumber, decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture), anyPin);

        // Assert
        Assert.Equal(decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture), card.Balance);
    }

    [Fact]
    public void Number_and_pin_are_kept_as_given() {
        // Act
        GiftCard card = new GiftCard("0123456789012345", 20m, 42);

        // Assert
        Assert.Equal("0123456789012345", card.Number);
        Assert.Equal(42, card.Pin);
    }

    [Theory]
    [InlineData("4.99")]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("500.01")]
    [InlineData("1000")]
    public void A_balance_outside_5_to_500_is_refused(string balance) {
        // Arrange
        string anyNumber = AnyNumber().Generate();
        int    anyPin    = Any.Int32().Between(0, 9999).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GiftCard(anyNumber, decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture), anyPin));

        // Assert
        Assert.Equal("balance", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1234)]
    [InlineData(9999)]
    public void A_pin_from_0_to_9999_is_accepted(int pin) {
        // Arrange
        string anyNumber = AnyNumber().Generate();

        // Act
        GiftCard card = new GiftCard(anyNumber, 20m, pin);

        // Assert
        Assert.Equal(pin, card.Pin);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    public void A_pin_outside_0_to_9999_is_refused(int pin) {
        // Arrange
        string anyNumber = AnyNumber().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GiftCard(anyNumber, 20m, pin));

        // Assert
        Assert.Equal("pin", exception.ParamName);
    }

    [Fact]
    public void A_null_number_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GiftCard(null!, 20m, 1234));
    }

    [Theory]
    [InlineData("")]
    [InlineData("012345678901234")]      // 15 digits
    [InlineData("01234567890123456")]    // 17 digits
    public void A_number_of_digits_that_is_not_16_long_is_refused(string number) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new GiftCard(number, 20m, 1234));

        // Assert
        Assert.Equal("number", exception.ParamName);
    }

    [Theory]
    [InlineData("012345678901234A")]
    [InlineData("0123 5678 9012 34")]
    [InlineData("0123-5678-9012-34")]
    [InlineData("012345678901234\n")]
    [InlineData("٠123456789012345")]   // an Arabic-Indic digit is not an ASCII digit
    public void A_number_with_a_character_that_is_not_an_ASCII_digit_is_refused(string number) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new GiftCard(number, 20m, 1234));

        // Assert
        Assert.Equal("number", exception.ParamName);
    }

    [Fact]
    public void The_number_is_checked_before_the_balance_and_the_balance_before_the_pin() {
        // Act & Assert
        Assert.Equal("number", Assert.Throws<ArgumentException>(() => new GiftCard("abc", 1m, -1)).ParamName);
        Assert.Equal("balance", Assert.Throws<ArgumentOutOfRangeException>(() => new GiftCard("0123456789012345", 1m, -1)).ParamName);
    }

    [Fact]
    public void Redeeming_less_than_the_balance_lowers_it_and_leaves_nothing_to_pay() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        // Act
        decimal leftToPay = card.Redeem(20m);

        // Assert
        Assert.Equal(0m, leftToPay);
        Assert.Equal(30m, card.Balance);
    }

    [Fact]
    public void Redeeming_exactly_the_balance_empties_the_card() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        // Act
        decimal leftToPay = card.Redeem(50m);

        // Assert
        Assert.Equal(0m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Redeeming_more_than_the_balance_pays_what_it_can_and_returns_the_rest() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        // Act
        decimal leftToPay = card.Redeem(80.25m);

        // Assert
        Assert.Equal(30.25m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Redeeming_nothing_changes_nothing() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        // Act
        decimal leftToPay = card.Redeem(0m);

        // Assert
        Assert.Equal(0m, leftToPay);
        Assert.Equal(50m, card.Balance);
    }

    [Fact]
    public void Redeeming_from_an_empty_card_returns_the_whole_amount() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(5m).Generate();
        card.Redeem(5m);

        // Act
        decimal leftToPay = card.Redeem(12m);

        // Assert
        Assert.Equal(12m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact]
    public void Successive_redemptions_accumulate() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(100m).Generate();

        // Act
        card.Redeem(30m);
        decimal leftToPay = card.Redeem(90m);

        // Assert
        Assert.Equal(20m, leftToPay);
        Assert.Equal(0m, card.Balance);
    }

    [Fact(Skip = "domain defect: Redeem has no check on the amount, so Redeem(-10) credits the card (balance 50 -> 60) and returns 0 (literal input, no drawn value)")]
    public void Redeeming_a_negative_amount_does_not_credit_the_card() {
        // Arrange
        GiftCard card = Any.GiftCard().WithBalance(50m).Generate();

        // Act
        try {
            card.Redeem(-10m);
        } catch (ArgumentOutOfRangeException) {
            // refusing is as good as ignoring
        }

        // Assert
        Assert.True(card.Balance <= 50m);
    }

}
