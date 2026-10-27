namespace Shop.Domain;

/// <summary>A prepaid card: a 16-digit number, a balance between 5 and 500, a four-digit PIN.</summary>
public sealed class GiftCard {

    public GiftCard(string number, decimal balance, int pin) {
        ArgumentNullException.ThrowIfNull(number);
        if (!number.All(char.IsAsciiDigit)) {
            throw new ArgumentException("A gift card number is made of digits.", nameof(number));
        }
        if (number.Length != 16) {
            throw new ArgumentException("A gift card number has 16 digits.", nameof(number));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(balance, 5m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(balance, 500m);
        if (!(pin >= 0 && pin <= 9999)) {
            throw new ArgumentOutOfRangeException(nameof(pin), "A PIN has four digits.");
        }

        Number  = number;
        Balance = balance;
        Pin     = pin;
    }

    public string Number { get; }

    public decimal Balance { get; private set; }

    public int Pin { get; }

    /// <summary>Pays what it can of an amount; returns what is left to pay.</summary>
    public decimal Redeem(decimal amount) {
        decimal paid = Math.Min(amount, Balance);
        Balance -= paid;

        return amount - paid;
    }

}
