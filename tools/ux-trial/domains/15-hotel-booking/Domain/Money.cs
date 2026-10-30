using System.Text.RegularExpressions;

namespace Booking.Domain;

public sealed class Money {

    public Money(decimal amount, string currency) {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Validate(currency);
        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException("An amount has at most two decimal places.", nameof(amount));
        }
        Amount   = amount;
        Currency = currency;
    }

    public decimal Amount   { get; }
    public string  Currency { get; }

    public Money Times(int factor) => new Money(Amount * factor, Currency);

    public Money Percent(decimal rate) => new Money(decimal.Round(Amount * rate, 2, MidpointRounding.AwayFromZero), Currency);

    public override bool Equals(object? obj) => obj is Money other && other.Amount == Amount && other.Currency == Currency;
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public override string ToString() => $"{Amount} {Currency}";

    private static void Validate(string currency) {
        ArgumentNullException.ThrowIfNull(currency);
        if (!Regex.IsMatch(currency, "^[A-Z]{3}$")) {
            throw new ArgumentException($"'{currency}' is not an ISO 4217 code.", nameof(currency));
        }
    }

}
