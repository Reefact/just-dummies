using System.Text.RegularExpressions;

namespace Freight.Domain;

/// <summary>A tariff per revenue ton (W/M: one metric ton or one cubic metre, whichever is greater).</summary>
public sealed class FreightRate {

    private static readonly Regex CurrencyFormat = new("^[A-Z]{3}$");

    public FreightRate(decimal perRevenueTon, string currency) {
        PerRevenueTon = perRevenueTon switch {
            <= 0m       => throw new ArgumentOutOfRangeException(nameof(perRevenueTon), perRevenueTon, "A rate must be positive."),
            > 10_000m   => throw new ArgumentOutOfRangeException(nameof(perRevenueTon), perRevenueTon, "A rate above 10,000 per revenue ton is a typo."),
            _ when decimal.Round(perRevenueTon, 2) != perRevenueTon
                        => throw new ArgumentException("A rate has at most two decimal places.", nameof(perRevenueTon)),
            _           => perRevenueTon
        };
        ArgumentNullException.ThrowIfNull(currency);
        if (!CurrencyFormat.IsMatch(currency)) {
            throw new FormatException($"'{currency}' is not an ISO 4217 code.");
        }

        Currency = currency;
    }

    public decimal PerRevenueTon { get; }

    public string Currency { get; }

}
