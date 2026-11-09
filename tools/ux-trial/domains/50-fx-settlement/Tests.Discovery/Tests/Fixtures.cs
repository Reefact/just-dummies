using System;

namespace FxSettlement.Domain.Tests;

/// <summary>Values the tests are about: chosen once, and spelled as literals rather than drawn.</summary>
internal static class Fixtures {

    /// <summary>The ISO 4217 table of the desk, as the domain describes it, with the minor units of each code.</summary>
    public static readonly (string Code, int MinorUnits)[] DeskIsoTable = [
        ("AUD", 2), ("BHD", 3), ("CAD", 2), ("CHF", 2), ("CLF", 4), ("CLP", 0), ("EUR", 2), ("GBP", 2), ("JOD", 3),
        ("JPY", 0), ("KRW", 0), ("KWD", 3), ("NOK", 2), ("OMR", 3), ("SEK", 2), ("TND", 3), ("USD", 2)
    ];

    // Two real LEIs, in ordinal order: "52..." sorts before "54...".
    public static readonly Lei LeiFirst  = new Lei("529900T8BM49AURSDO55");
    public static readonly Lei LeiSecond = new Lei("5493001KJTIIGC8Y1R12");

    // Mid-March 2025: Monday the 10th to Monday the 17th.
    public static readonly DateOnly Monday    = new DateOnly(2025, 3, 10);
    public static readonly DateOnly Tuesday   = new DateOnly(2025, 3, 11);
    public static readonly DateOnly Wednesday = new DateOnly(2025, 3, 12);
    public static readonly DateOnly Thursday  = new DateOnly(2025, 3, 13);
    public static readonly DateOnly Friday    = new DateOnly(2025, 3, 14);
    public static readonly DateOnly Saturday  = new DateOnly(2025, 3, 15);
    public static readonly DateOnly Sunday    = new DateOnly(2025, 3, 16);
    public static readonly DateOnly NextMonday = new DateOnly(2025, 3, 17);

    public static CurrencyPair Pair(Currency @base, Currency quote) {
        return new CurrencyPair(@base, quote);
    }

    public static ExchangeRate Rate(Currency @base, Currency quote, decimal value) {
        return new ExchangeRate(new CurrencyPair(@base, quote), value);
    }

    public static Money Usd(decimal amount) { return new Money(amount, Currency.Usd); }

    public static Money Eur(decimal amount) { return new Money(amount, Currency.Eur); }

    public static Money Jpy(decimal amount) { return new Money(amount, Currency.Jpy); }

    /// <summary>Currencies of the standard sheet.</summary>
    public static readonly Currency Aud = Currency.Iso("AUD");

}
